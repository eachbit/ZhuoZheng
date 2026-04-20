using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 南厅地暖交互系统 - 简化版触发器管理
/// </summary>
public class South : MonoBehaviour
{
    private const string HintFramePrefix = "Chapter03PlaqueFrame_";

    [Header("触发器引用")]
    [Tooltip("寒冷区域触发器对象")]
    public GameObject coldAreaTrigger;
    
    [Tooltip("灶口区域触发器对象")]
    public GameObject stoveAreaTrigger;
    
    [Tooltip("案桌区域触发器对象")]
    public GameObject deskAreaTrigger;
    
    [Header("场景引用")]
    [Tooltip("主相机，用于色调控制")]
    public Camera mainCamera;
    
    [Tooltip("火焰粒子效果（灶口处）")]
    public ParticleSystem fireParticleSystem;
    
    [Tooltip("热气粒子效果（地面缝隙）")]
    public ParticleSystem steamParticleSystem;
    
    [Tooltip("昆曲工尺谱物品对象（Book模型）")]
    public GameObject gongcheScoreObject;
    
    [Tooltip("UI画布")]
    public Canvas uiCanvas;
    
    [Tooltip("提示文本UI（支持Text和TextMeshPro）")]
    public Component hintText;
    
    [Tooltip("文化提示弹窗")]
    public GameObject cultureTipPanel;
    
    [Tooltip("昆曲工尺谱图标UI")]
    public Image gongcheIconUI;
    
    [Header("色调设置")]
    [Tooltip("冷色调（蓝灰色）")]
    public Color coldTone = new Color(0.6f, 0.7f, 0.85f, 1f);
    
    [Tooltip("暖色调（暖金色）")]
    public Color warmTone = new Color(1f, 0.95f, 0.7f, 1f);
    
    [Tooltip("正常色调")]
    public Color normalTone = Color.white;
    
    [Header("视频播放")]
    [Tooltip("视频播放器组件（用于播放8秒视频）")]
    public UnityEngine.Video.VideoPlayer videoPlayer;
    
    [Tooltip("视频渲染目标（RawImage或其他渲染目标）")]
    public UnityEngine.UI.RawImage videoRenderTexture;
    
    [Header("交互状态")]
    private bool isHeatingActivated = false;
    private bool hasPickedUpGongcheScore = false;
    [SerializeField]
    private bool isInColdArea = false;
    private bool isInStoveArea = false;
    private bool isInDeskArea = false;
    private bool isPresentationUiSuppressed = false;
    private bool restoreHintAfterVideo = false;
    private bool restoreHintFrameAfterVideo = false;
    private bool restoreCultureTipAfterVideo = false;
    private bool restoreGongcheIconAfterVideo = false;
    
    // 色调过渡相关
    private Coroutine toneTransitionCoroutine;

    void Awake()
    {
        SetVideoStandby();
        SetParticleEffectsStandby();
    }

    void SetVideoStandby()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }
        }
        if (videoRenderTexture != null)
        {
            videoRenderTexture.gameObject.SetActive(false);
        }
    }

    void SetParticleEffectsStandby()
    {
        SetParticleSystemStandby(fireParticleSystem);
        SetParticleSystemStandby(steamParticleSystem);
    }

    void SetParticleSystemStandby(ParticleSystem particleSystem)
    {
        if (particleSystem == null)
        {
            return;
        }

        ParticleSystem.MainModule main = particleSystem.main;
        main.playOnAwake = false;
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void SetChapterPresentationVisible(bool isVisible)
    {
        Component actualTextComponent = ResolveHintTextComponent();
        GameObject hintObject = actualTextComponent != null ? actualTextComponent.gameObject : null;
        GameObject hintFrameObject = FindHintFrameObject(actualTextComponent);
        GameObject gongcheIconObject = gongcheIconUI != null ? gongcheIconUI.gameObject : null;

        if (!isVisible)
        {
            isPresentationUiSuppressed = true;
            restoreHintAfterVideo = hintObject != null && hintObject.activeSelf;
            restoreHintFrameAfterVideo = hintFrameObject != null && hintFrameObject.activeSelf;
            restoreCultureTipAfterVideo = cultureTipPanel != null && cultureTipPanel.activeSelf;
            restoreGongcheIconAfterVideo = gongcheIconObject != null && gongcheIconObject.activeSelf;

            if (hintFrameObject != null)
            {
                hintFrameObject.SetActive(false);
            }

            if (hintObject != null)
            {
                hintObject.SetActive(false);
            }

            if (cultureTipPanel != null)
            {
                cultureTipPanel.SetActive(false);
            }

            if (gongcheIconObject != null)
            {
                gongcheIconObject.SetActive(false);
            }

            return;
        }

        isPresentationUiSuppressed = false;

        if (hintObject != null)
        {
            hintObject.SetActive(restoreHintAfterVideo);
        }

        if (hintFrameObject != null)
        {
            hintFrameObject.SetActive(restoreHintFrameAfterVideo && hintObject != null && hintObject.activeSelf);
        }

        if (cultureTipPanel != null)
        {
            cultureTipPanel.SetActive(restoreCultureTipAfterVideo);
        }

        if (gongcheIconObject != null)
        {
            gongcheIconObject.SetActive(restoreGongcheIconAfterVideo);
        }
    }

    Component ResolveHintTextComponent()
    {
        if (hintText == null)
        {
            return null;
        }

        if (hintText is RectTransform rectTransform)
        {
            TextMeshProUGUI tmpText = rectTransform.GetComponent<TextMeshProUGUI>();
            if (tmpText == null)
            {
                tmpText = rectTransform.GetComponentInChildren<TextMeshProUGUI>();
            }

            if (tmpText != null)
            {
                hintText = tmpText;
                return tmpText;
            }

            Text uiText = rectTransform.GetComponent<Text>();
            if (uiText == null)
            {
                uiText = rectTransform.GetComponentInChildren<Text>();
            }

            if (uiText != null)
            {
                hintText = uiText;
                return uiText;
            }

            return null;
        }

        return hintText;
    }

    GameObject FindHintFrameObject(Component textComponent)
    {
        RectTransform textRect = textComponent != null ? textComponent.GetComponent<RectTransform>() : null;
        if (textRect == null || textRect.parent == null)
        {
            return null;
        }

        Transform frameTransform = textRect.parent.Find(HintFramePrefix + textComponent.gameObject.name);
        return frameTransform != null ? frameTransform.gameObject : null;
    }
    
    void Start()
    {
        Debug.Log("🎬🎬🎬 South.cs Start方法开始执行 🎬🎬");
        Debug.Log($"🎯 South脚本挂载在对象: {gameObject.name}");
        Debug.Log($"🎯 脚本是否启用: {enabled}");
        Debug.Log($"🎯 对象是否激活: {gameObject.activeInHierarchy}");
        
        // 初始化
        if (mainCamera == null)
        {
            Debug.Log("📷 自动查找主相机...");
            mainCamera = Camera.main;
            if (mainCamera == null)
                Debug.LogError("❌ 未找到主相机！");
            else
                Debug.Log($"✅ 找到主相机: {mainCamera.name}");
        }
        
        // 自动查找触发器对象
        Debug.Log("🔍 开始设置触发器...");
        SetupTriggers();
        
        // 自动查找Book模型
        if (gongcheScoreObject == null)
        {
            Debug.Log("📖 自动查找Book模型...");
            gongcheScoreObject = GameObject.Find("Book");
            if (gongcheScoreObject != null)
                Debug.Log($"✅ 找到拾取物: {gongcheScoreObject.name}");
            else
                Debug.LogWarning("⚠️ 未找到工具书拾取物");
        }
        
        // 验证UI引用
        Debug.Log("🎨 开始验证UI引用...");
        ValidateUIReferences();
        
        // 诊断UI问题
        Debug.Log("🔬 开始UI诊断...");
        DiagnoseUIIssues();
        
        // 初始设置为冷色调
        Debug.Log("🎨 设置相机色调...");
        SetCameraTone(coldTone);
        
        // 配置火焰粒子颜色渐变
        ConfigureFireParticleColor();
        
        // 创建临时图标
        if (gongcheIconUI != null && gongcheIconUI.sprite == null)
        {
            CreateTemporaryGongcheIcon();
        }
        
        // 隐藏物品和UI
        Debug.Log("🙈 开始隐藏初始对象...");
        
        if (gongcheScoreObject != null)
        {
            gongcheScoreObject.SetActive(false);
            Debug.Log($"   ✅ 隐藏工具书拾取物: {gongcheScoreObject.name}");
        }
        
        if (gongcheIconUI != null)
        {
            gongcheIconUI.gameObject.SetActive(false);
            Debug.Log($"   ✅ 隐藏工尺谱图标: {gongcheIconUI.name}");
        }
        
        if (cultureTipPanel != null)
        {
            cultureTipPanel.SetActive(false);
            Chapter03PlaqueFrame.ApplySoftPanel(cultureTipPanel);
            Debug.Log($"   ✅ 隐藏文化提示弹窗: {cultureTipPanel.name}");
        }
        else
        {
            Debug.LogWarning("   ⚠️ CultureTipPanel为null，无法隐藏");
        }
        
        SetParticleEffectsStandby();
        
        // 隐藏视频渲染目标
        if (videoRenderTexture != null)
        {
            videoRenderTexture.gameObject.SetActive(false);
            Debug.Log("🎥 隐藏视频渲染目标");
        }
        
        Debug.Log("💬 准备更新提示文本...");
        UpdateHintText("");
        
        // 最终确认：强制隐藏所有可能遮挡的UI
        Debug.Log("🔒 最终确认：强制隐藏所有UI Panel...");
        ForceHideAllUIPanels();
        
        Debug.Log("✅✅✅ South系统初始化完成 ✅✅✅");
    }
    
    /// <summary>
    /// 调试：在运行时查找并显示所有激活的UI对象
    /// 在Console中输入：FindObjectOfType&lt;South&gt;().DebugShowActiveUI()
    /// </summary>
    public void DebugShowActiveUI()
    {
        Debug.Log("========== 激活的UI对象列表 ==========");
        
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            Debug.Log($"📐 Canvas: {canvas.name} (Active: {canvas.gameObject.activeInHierarchy})");
            
            // 查找Canvas下的所有Image和Panel
            Image[] images = canvas.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (img.gameObject.activeInHierarchy)
                {
                    RectTransform rect = img.GetComponent<RectTransform>();
                    Color color = img.color;
                    
                    Debug.Log($"  🖼️  {img.name}");
                    Debug.Log($"      尺寸: {rect.sizeDelta}");
                    Debug.Log($"      颜色: RGBA({color.r:F2}, {color.g:F2}, {color.b:F2}, {color.a:F2})");
                    Debug.Log($"      位置: {rect.anchoredPosition}");
                    
                    // 标记可能是遮罩的对象
                    if (color.a > 0.1f && rect.sizeDelta.x > 500 && rect.sizeDelta.y > 500)
                    {
                        Debug.LogWarning($"      ⚠️ 这可能是一个遮罩Panel！");
                    }
                }
            }
        }
        
        Debug.Log("======================================");
    }
    
    /// <summary>
    /// 强制隐藏所有可能遮挡屏幕的UI Panel
    /// </summary>
    void ForceHideAllUIPanels()
    {
        // 查找所有Image组件的UI对象
        UnityEngine.UI.Image[] allImages = FindObjectsOfType<UnityEngine.UI.Image>();
        foreach (UnityEngine.UI.Image img in allImages)
        {
            // 检查是否是大尺寸的Panel
            RectTransform rectTransform = img.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 size = rectTransform.sizeDelta;
                // 如果尺寸很大（可能是背景遮罩）
                if (size.x > 500 || size.y > 500)
                {
                    Color color = img.color;
                    if (color.a > 0.1f)
                    {
                        Debug.Log($"    发现大尺寸半透明Panel: {img.name}, Alpha={color.a}");
                    }
                }
            }
        }
        
        // 额外检查：确保CultureTipPanel绝对隐藏
        if (cultureTipPanel != null && cultureTipPanel.activeInHierarchy)
        {
            Debug.LogError("❌ CultureTipPanel仍然激活！强制隐藏！");
            cultureTipPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 验证UI引用是否正确设置
    /// </summary>
    void ValidateUIReferences()
    {
        Debug.Log("========== UI引用验证 ==========");
        
        // 自动查找并修复HintText
        if (hintText == null)
        {
            Debug.LogWarning("⚠️ hintText未设置，尝试自动查找...");
            AutoFindHintText();
        }
        else
        {
            // 检查类型是否正确
            if (hintText is RectTransform)
            {
                Debug.LogWarning("⚠️ hintText被错误地设置为RectTransform，尝试自动修复...");
                AutoFindHintText();
            }
            else if (hintText is TextMeshProUGUI || hintText is Text)
            {
                Debug.Log($"✅ Hint Text已设置: {hintText.name}");
                
                if (hintText is TextMeshProUGUI)
                {
                    Debug.Log("   类型: TextMeshProUGUI (新版UI)");
                }
                else if (hintText is Text)
                {
                    Debug.Log("   类型: Text (旧版UGUI)");
                }
            }
            else
            {
                Debug.LogError($"❌ hintText类型不正确: {hintText.GetType().Name}，尝试自动修复...");
                AutoFindHintText();
            }
        }
        
        // 验证Canvas和Text的Active状态
        if (hintText != null)
        {
            GameObject hintObj = (hintText as Component).gameObject;
            Debug.Log($"   HintText对象Active: {hintObj.activeSelf}");
            Debug.Log($"   HintText父对象Active: {hintObj.transform.parent?.gameObject.activeSelf}");
            
            if (!hintObj.activeInHierarchy)
            {
                Debug.LogWarning("⚠️ HintText或其父对象未激活！尝试激活...");
                hintObj.SetActive(true);
            }
        }
        
        // 自动查找CultureTipPanel
        if (cultureTipPanel == null)
        {
            GameObject panelObj = GameObject.Find("CultureTipPanel");
            if (panelObj == null)
                panelObj = GameObject.Find("Culture Tip Panel");
            
            if (panelObj != null)
            {
                cultureTipPanel = panelObj;
                Debug.Log($"✅ 自动关联Culture Tip Panel: {cultureTipPanel.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ Culture Tip Panel未设置（可选）");
            }
        }
        else
        {
            Debug.Log($"✅ Culture Tip Panel已设置: {cultureTipPanel.name}");
        }
        
        // 自动查找GongcheIconUI
        if (gongcheIconUI == null)
        {
            GameObject iconObj = GameObject.Find("JadeIcon");
            if (iconObj == null)
                iconObj = GameObject.Find("GongcheIcon");
            if (iconObj == null)
                iconObj = GameObject.Find("Icon");
            
            if (iconObj != null)
            {
                gongcheIconUI = iconObj.GetComponent<Image>();
                if (gongcheIconUI != null)
                    Debug.Log($"✅ 自动关联Gongche Icon UI: {gongcheIconUI.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ Gongche Icon UI未设置（可选）");
            }
        }
        else
        {
            Debug.Log($"✅ Gongche Icon UI已设置: {gongcheIconUI.name}");
        }
        
        if (uiCanvas == null)
        {
            Debug.LogWarning("⚠️ UI Canvas未设置（可选）");
        }
        else
        {
            Debug.Log($"✅ UI Canvas已设置: {uiCanvas.name}");
        }
        
        Debug.Log("================================");
    }
    
    /// <summary>
    /// 自动查找HintText的Text组件
    /// </summary>
    void AutoFindHintText()
    {
        GameObject hintObj = null;
        
        // 尝试多种可能的名称
        string[] possibleNames = { "HintText", "Hint Text", "Hint", "Text_Hint", "TipText" };
        foreach (string name in possibleNames)
        {
            hintObj = GameObject.Find(name);
            if (hintObj != null)
            {
                Debug.Log($"🔍 自动找到HintText对象: {hintObj.name}");
                break;
            }
        }
        
        if (hintObj != null)
        {
            // 优先获取TextMeshProUGUI
            Component textComp = hintObj.GetComponent<TextMeshProUGUI>();
            if (textComp == null)
                textComp = hintObj.GetComponent<Text>();
            
            if (textComp != null)
            {
                hintText = textComp;
                Debug.Log($"✅ 自动关联Hint Text: {hintText.name} ({hintText.GetType().Name})");
            }
            else
            {
                Debug.LogError($"❌ {hintObj.name} 没有Text或TextMeshProUGUI组件！请添加UI文本组件。");
            }
        }
        else
        {
            Debug.LogError("❌ 未找到HintText对象！请在场景中创建或在Inspector中手动拖拽。");
        }
    }
    
    /// <summary>
    /// 设置触发器并添加触发器检测组件
    /// </summary>
    void SetupTriggers()
    {
        // 寒冷区域
        if (coldAreaTrigger == null)
            coldAreaTrigger = GameObject.Find("ColdAreaTrigger");
        
        if (coldAreaTrigger != null)
        {
            // 确保触发器设置正确
            SetupTrigger(coldAreaTrigger, "寒冷区域");
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到ColdAreaTrigger对象");
        }
        
        // 灶口区域
        if (stoveAreaTrigger == null)
            stoveAreaTrigger = GameObject.Find("StoveAreaTrigger");
        
        if (stoveAreaTrigger != null)
        {
            SetupTrigger(stoveAreaTrigger, "灶口区域");
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到StoveAreaTrigger对象");
        }
        
        // 案桌区域
        if (deskAreaTrigger == null)
            deskAreaTrigger = GameObject.Find("DeskAreaTrigger");
        
        if (deskAreaTrigger != null)
        {
            SetupTrigger(deskAreaTrigger, "案桌区域");
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到DeskAreaTrigger对象");
        }
    }
    
    /// <summary>
    /// 配置单个触发器
    /// </summary>
    void SetupTrigger(GameObject triggerObj, string areaName)
    {
        // 检查并修复Scale问题
        Transform trans = triggerObj.transform;
        if (trans.localScale.x < 0 || trans.localScale.y < 0 || trans.localScale.z < 0)
        {
            Debug.LogError($"❌ {areaName}的Scale有负数！请重置Transform");
            return;
        }
        
        // 添加或获取触发器检测脚本
        TriggerDetector detector = triggerObj.GetComponent<TriggerDetector>();
        if (detector == null)
        {
            detector = triggerObj.AddComponent<TriggerDetector>();
        }
        
        // 配置检测器
        detector.areaName = areaName;
        detector.OnTriggerEnterAction = OnAreaEnter;
        detector.OnTriggerExitAction = OnAreaExit;
        
        Debug.Log($"✅ {areaName}触发器已配置: {triggerObj.name}");
    }
    
    /// <summary>
    /// 区域进入回调
    /// </summary>
    void OnAreaEnter(string areaName)
    {
        Debug.Log($"📥 进入{areaName}");
        
        switch (areaName)
        {
            case "寒冷区域":
                EnterColdArea();
                break;
            case "灶口区域":
                EnterStoveArea();
                break;
            case "案桌区域":
                EnterDeskArea();
                break;
        }
    }
    
    /// <summary>
    /// 区域离开回调
    /// </summary>
    void OnAreaExit(string areaName)
    {
        Debug.Log($"📤 离开{areaName}");
        
        switch (areaName)
        {
            case "寒冷区域":
                ExitColdArea();
                break;
            case "灶口区域":
                ExitStoveArea();
                break;
            case "案桌区域":
                ExitDeskArea();
                break;
        }
    }
    
    void Update()
    {
        // 测试：每帧检查hintText状态
        if (Time.frameCount == 60) // 运行1秒后检查
        {
            Debug.Log($"🔍 [Update] hintText状态: {(hintText == null ? "NULL" : hintText.name)}");
            if (hintText != null)
            {
                Component hintComp = hintText as Component;
                Debug.Log($"🔍 [Update] HintText对象Active: {hintComp.gameObject.activeInHierarchy}");
            }
        }
        
        // 检测E键交互
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"⌨️ [Update] 按下E键 | isInStoveArea: {isInStoveArea} | isHeatingActivated: {isHeatingActivated} | isInDeskArea: {isInDeskArea} | hasPickedUpGongcheScore: {hasPickedUpGongcheScore}");
            
            if (isInStoveArea && !isHeatingActivated)
            {
                ActivateHeating();
            }
            else if (isInDeskArea && isHeatingActivated && !hasPickedUpGongcheScore)
            {
                PickUpGongcheScore();
            }
        }
    }
    
    /// <summary>
    /// 进入寒冷区域
    /// </summary>
    void EnterColdArea()
    {
        isInColdArea = true;
        
        // 切换到冷色调
        if (toneTransitionCoroutine != null)
            StopCoroutine(toneTransitionCoroutine);
        toneTransitionCoroutine = StartCoroutine(TransitionTone(coldTone, 1f));
        
        // 显示提示
        UpdateHintText("南厅寒冷，地暖未启");
        
        // 显示文化提示（可选）
        ShowCultureTip();
    }
    
    /// <summary>
    /// 离开寒冷区域
    /// </summary>
    void ExitColdArea()
    {
        isInColdArea = false;
        
        // 如果地暖已启动，保持暖色调；否则恢复常温
        if (!isHeatingActivated)
        {
            if (toneTransitionCoroutine != null)
                StopCoroutine(toneTransitionCoroutine);
            toneTransitionCoroutine = StartCoroutine(TransitionTone(normalTone, 1f));
        }
        
        UpdateHintText("");
        HideCultureTip();
    }
    
    /// <summary>
    /// 进入灶口区域
    /// </summary>
    void EnterStoveArea()
    {
        isInStoveArea = true;
        
        if (!isHeatingActivated)
        {
            UpdateHintText("地龙入口。室外烧火，热气经地下烟道循环，方砖乃温。\n按E添柴，开启地暖");
        }
    }
    
    /// <summary>
    /// 离开灶口区域
    /// </summary>
    void ExitStoveArea()
    {
        isInStoveArea = false;
        
        if (!isHeatingActivated)
        {
            UpdateHintText("");
        }
    }
    
    /// <summary>
    /// 进入案桌区域
    /// </summary>
    void EnterDeskArea()
    {
        isInDeskArea = true;
        
        if (isHeatingActivated && !hasPickedUpGongcheScore)
        {
            UpdateHintText("按 E 取走工具书");
        }
    }
    
    /// <summary>
    /// 离开案桌区域
    /// </summary>
    void ExitDeskArea()
    {
        isInDeskArea = false;
        UpdateHintText("");
    }
    
    /// <summary>
    /// 激活地暖系统
    /// </summary>
    void ActivateHeating()
    {
        isHeatingActivated = true;
        
        // 播放火焰粒子
        if (fireParticleSystem != null)
        {
            fireParticleSystem.Play(true);
        }
        
        // 播放热气粒子
        if (steamParticleSystem != null)
        {
            steamParticleSystem.Play(true);
        }
        
        // 显示交互成功提示
        UpdateHintText("柴已添，火已燃。热气沿地龙入室。");
        
        // 等待1.5秒后播放视频
        StartCoroutine(PlayVideoAfterDelayAndWarmUp());
    }
    
    /// <summary>
    /// 等待1.5秒后播放视频，然后回到室内并逐渐变暖
    /// </summary>
    IEnumerator PlayVideoAfterDelayAndWarmUp()
    {
        // 等待1.5秒
        Debug.Log("⏰ 等待1.5秒后播放视频...");
        yield return new WaitForSeconds(1.5f);
        
        // 播放8秒视频
        Debug.Log("🎥 开始播放视频...");
        yield return StartCoroutine(PlayCinematicVideo());
        Debug.Log("✅ 视频播放完成");
        
        // 色调从蓝灰色逐渐过渡到暖金色（渐变2秒）
        if (toneTransitionCoroutine != null)
            StopCoroutine(toneTransitionCoroutine);
        toneTransitionCoroutine = StartCoroutine(TransitionTone(warmTone, 2f));
        
        // 地面方砖缝隙冒出热气（已在ActivateHeating中启动）
        
        // 显示激活提示
        UpdateHintText("地暖既启，冬室如春。");
        
        // 显示昆曲工尺谱
        if (gongcheScoreObject != null)
        {
            gongcheScoreObject.SetActive(true);
            
            // 让工具书微微旋转
            StartCoroutine(RotateGongcheScore());
        }
        
        // 3秒后清除提示
        yield return new WaitForSeconds(3f);
        if (isInDeskArea)
        {
            UpdateHintText("按 E 取走工具书");
        }
        else
        {
            UpdateHintText("");
        }
    }
    
    /// <summary>
    /// 播放过场视频（8秒）
    /// </summary>
    IEnumerator PlayCinematicVideo()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("❌ VideoPlayer未设置！请在Inspector中拖拽VideoPlayer组件");
            yield break;
        }
        
        Debug.Log("🎥 开始播放视频...");
        Debug.Log($"   视频长度: {videoPlayer.length}秒");
        Debug.Log($"   视频URL: {videoPlayer.url}");
        
        // 如果有渲染目标，显示它
        SetChapterPresentationVisible(false);
        if (videoRenderTexture != null)
        {
            videoRenderTexture.gameObject.SetActive(true);
            Debug.Log("✅ 视频渲染目标已显示");
        }
        
        // 播放视频
        videoPlayer.playOnAwake = false;
        videoPlayer.Play();
        
        // 等待视频播放完成（8秒或视频实际长度）
        float videoDuration = videoPlayer.length > 0 ? (float)videoPlayer.length : 8f;
        Debug.Log($"⏰ 等待视频播放完成: {videoDuration}秒");
        yield return new WaitForSeconds(videoDuration);
        
        // 停止视频
        videoPlayer.Stop();
        Debug.Log("✅ 视频已停止");
        
        // 隐藏渲染目标
        if (videoRenderTexture != null)
        {
            videoRenderTexture.gameObject.SetActive(false);
            Debug.Log("✅ 视频渲染目标已隐藏");
        }

        SetChapterPresentationVisible(true);
    }
    
    /// <summary>
    /// 回到室内并逐渐变暖（已废弃，使用PlayVideoAfterDelayAndWarmUp替代）
    /// </summary>
    [System.Obsolete("使用 PlayVideoAfterDelayAndWarmUp 替代")]
    IEnumerator ReturnToRoomAndWarmUp()
    {
        yield return new WaitForSeconds(2f);
        
        // 色调从蓝灰色逐渐过渡到暖金色（渐变2秒）
        if (toneTransitionCoroutine != null)
            StopCoroutine(toneTransitionCoroutine);
        toneTransitionCoroutine = StartCoroutine(TransitionTone(warmTone, 2f));
        
        // 地面方砖缝隙冒出热气（已在ActivateHeating中启动）
        
        // 显示激活提示
        UpdateHintText("地暖既启，冬室如春。");
        
        // 显示昆曲工尺谱
        if (gongcheScoreObject != null)
        {
            gongcheScoreObject.SetActive(true);
            
            // 让工具书微微旋转
            StartCoroutine(RotateGongcheScore());
        }
        
        // 3秒后清除提示
        yield return new WaitForSeconds(3f);
        if (isInDeskArea)
        {
            UpdateHintText("按 E 取走工具书");
        }
        else
        {
            UpdateHintText("");
        }
    }
    
    /// <summary>
    /// 拾取昆曲工尺谱
    /// </summary>
    void PickUpGongcheScore()
    {
        hasPickedUpGongcheScore = true;
        
        // 保存状态到PlayerPrefs（供北厅读取）
        PlayerPrefs.SetInt("HasGongcheScore", 1);
        PlayerPrefs.Save();
        Debug.Log("✅ 已保存工尺谱状态到PlayerPrefs");
        
        // 信物飞向玩家（动画效果）
        if (gongcheScoreObject != null)
        {
            StartCoroutine(FlyGongcheScoreToPlayer());
        }
        
        // UI右上角出现昆曲工尺谱图标
        if (gongcheIconUI != null)
        {
            gongcheIconUI.gameObject.SetActive(true);
        }
        
        // 通知北厅已获得工尺谱
        North northScript = FindObjectOfType<North>();
        if (northScript != null)
        {
            northScript.SetGongcheScoreStatus(true);
            Debug.Log("✅ 已通知北厅获得工尺谱");
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到North脚本，无法通知北厅");
        }
        
        // 显示获得提示
        UpdateHintText("获得工具书，可前往北厅试回音。");
        
        // 3秒后清除提示
        StartCoroutine(ClearHintAfterDelay(3f));
    }
    
    /// <summary>
    /// 昆曲工尺谱飞向玩家的动画
    /// </summary>
    IEnumerator FlyGongcheScoreToPlayer(GameObject player = null)
    {
        if (player == null)
        {
            // 尝试查找玩家
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
                playerObj = GameObject.Find("Player");
            player = playerObj;
        }
        
        if (player == null || gongcheScoreObject == null)
            yield break;
        
        Vector3 startPos = gongcheScoreObject.transform.position;
        Vector3 endPos = player.transform.position + Vector3.up * 1.5f;
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 使用抛物线轨迹
            gongcheScoreObject.transform.position = Vector3.Lerp(startPos, endPos, t);
            gongcheScoreObject.transform.position += Vector3.up * Mathf.Sin(t * Mathf.PI) * 2f;
            
            yield return null;
        }
        
        // 隐藏昆曲工尺谱对象
        gongcheScoreObject.SetActive(false);
    }
    
    /// <summary>
    /// 昆曲工尺谱旋转动画（上半球左右晃动）
    /// </summary>
    IEnumerator RotateGongcheScore()
    {
        // 记录初始位置和旋转
        Vector3 initialPosition = gongcheScoreObject.transform.position;
        Quaternion initialRotation = gongcheScoreObject.transform.rotation;
        
        float hoverAmplitude = 0.003f;  // 上下悬浮幅度
        float hoverFrequency = 1f;      // 上下悬浮频率
        
        float swayAngle = 15f;          // 左右晃动角度（±15度）
        float swayFrequency = 0.8f;     // 左右晃动频率（更慢更优雅）
        
        while (gongcheScoreObject != null && gongcheScoreObject.activeSelf)
        {
            // 轻微的上下浮动
            float hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            
            // 左右晃动（钟摆效果，只在上半球）
            float swayOffset = Mathf.Sin(Time.time * swayFrequency) * swayAngle;
            
            // 应用位置偏移
            gongcheScoreObject.transform.position = initialPosition + Vector3.up * hoverOffset;
            
            // 应用旋转：绕Y轴左右晃动（限制在上半球）
            gongcheScoreObject.transform.rotation = initialRotation * Quaternion.Euler(0, swayOffset, 0);
            
            yield return null;
        }
    }

    /// <summary>
    /// 色调过渡
    /// </summary>
    IEnumerator TransitionTone(Color targetColor, float duration)
    {
        if (mainCamera == null)
            yield break;
        
        Color startColor = mainCamera.backgroundColor;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 使用平滑曲线
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            mainCamera.backgroundColor = Color.Lerp(startColor, targetColor, smoothT);
            
            yield return null;
        }
        
        mainCamera.backgroundColor = targetColor;
    }
    
    /// <summary>
    /// 设置相机色调
    /// </summary>
    void SetCameraTone(Color tone)
    {
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = tone;
        }
    }
    
    /// <summary>
    /// 更新提示文本
    /// </summary>
    /// <param name="text">要显示的文本</param>
    /// <param name="forceShow">是否强制显示（用于South）</param>
    void UpdateHintText(string text, bool forceShow = false)
    {
        if (hintText == null)
        {
            Debug.LogWarning("⚠️ hintText为null，无法更新提示文本: " + text);
            Debug.LogWarning("💡 请检查：1.HintText是否拖拽正确 2.是否是Text组件而不是GameObject");
            return;
        }
        
        Debug.Log($"📝 更新提示文本: '{text}'");
        
        // 如果拖拽的是RectTransform或GameObject，自动查找TextMeshProUGUI组件
        Component actualTextComponent = null;
        
        if (hintText is RectTransform rectTransform)
        {
            Debug.Log($"   ⚠️ 检测到RectTransform，尝试自动获取TextMeshProUGUI组件...");
            actualTextComponent = rectTransform.GetComponent<TextMeshProUGUI>();
            if (actualTextComponent == null)
            {
                actualTextComponent = rectTransform.GetComponentInChildren<TextMeshProUGUI>();
            }
            
            if (actualTextComponent == null)
            {
                actualTextComponent = rectTransform.GetComponent<Text>();
                if (actualTextComponent == null)
                {
                    actualTextComponent = rectTransform.GetComponentInChildren<Text>();
                }
            }
            
            if (actualTextComponent == null)
            {
                Debug.LogError($"❌ 在 {rectTransform.name} 中找不到TextMeshProUGUI或Text组件！");
                Debug.LogError($"💡 请确保SouthHintText对象有TextMeshProUGUI组件");
                return;
            }
            
            Debug.Log($"   ✅ 自动获取到组件: {actualTextComponent.GetType().Name}");
            hintText = actualTextComponent;
        }
        else
        {
            actualTextComponent = hintText;
        }
        
        // 获取hintText的GameObject
        GameObject hintObj = actualTextComponent.gameObject;
        
        Debug.Log($"   → 组件类型: {actualTextComponent.GetType().Name}");
        Debug.Log($"   → 对象名称: {hintObj.name}");
        Debug.Log($"   → 设置文本: '{text}'");
        Debug.Log($"   → 当前Active: {hintObj.activeInHierarchy}");
        
        if (actualTextComponent is TextMeshProUGUI tmpText)
        {
            // 强制激活对象
            if (!hintObj.activeSelf)
            {
                Debug.Log($"   ⚠️ 对象未激活，强制激活...");
                hintObj.SetActive(true);
            }
            
            // 检查并修复位置（如果Y坐标太高，调整到屏幕可见区域）
            // South的UI显示在底部（Y=150），North的UI显示在顶部（Y=-150）以避免重叠
            RectTransform textRectTransform = tmpText.GetComponent<RectTransform>();
            if (textRectTransform != null)
            {
                Vector2 currentPos = textRectTransform.anchoredPosition;
                Debug.Log($"   → 当前位置: {currentPos}");
                
                // South的HintText应该在底部（Y=150），North的HintText应该在顶部（Y=-150）
                // 自动检测并调整位置
                bool isSouthUI = hintObj.name.Contains("South") || (GetComponent<South>() != null);
                
                if (isSouthUI)
                {
                    // South UI显示在底部
                    if (currentPos.y < 100 || currentPos.y > 200)
                    {
                        Debug.Log($"   ⚠️ South UI位置不正确({currentPos.y})，调整到150...");
                        textRectTransform.anchoredPosition = new Vector2(currentPos.x, 150);
                    }
                }
                else
                {
                    // North UI显示在顶部
                    if (currentPos.y > -100)
                    {
                        Debug.Log($"   ⚠️ North UI位置不正确({currentPos.y})，调整到-150...");
                        textRectTransform.anchoredPosition = new Vector2(currentPos.x, -150);
                    }
                }
                
                Debug.Log($"   → 最终位置: {textRectTransform.anchoredPosition}");
            }
            
            tmpText.text = text;
            
            // 确保文本颜色可见
            Color textColor = tmpText.color;
            if (textColor.a <= 0.01f)
            {
                Debug.Log($"   ⚠️ 文本透明度为0，设置为白色不透明...");
                tmpText.color = new Color(1f, 1f, 1f, 1f);
            }
            
            // 确保字体大小合适
            if (tmpText.fontSize < 20)
            {
                Debug.Log($"   ⚠️ 字体大小过小({tmpText.fontSize})，设置为36...");
                tmpText.fontSize = 36;
            }
            
            bool shouldShow = !isPresentationUiSuppressed && (forceShow || !string.IsNullOrEmpty(text));
            hintObj.SetActive(shouldShow);
            Chapter03PlaqueFrame.ApplyHintFrame(actualTextComponent, shouldShow);
            
            Debug.Log($"   → Active状态: {shouldShow}");
            Debug.Log($"   → 对象实际Active: {hintObj.activeSelf}");
            Debug.Log($"   → 在Hierarchy中Active: {hintObj.activeInHierarchy}");
            Debug.Log($"   → 文本内容: '{tmpText.text}'");
            Debug.Log($"   → 字体大小: {tmpText.fontSize}");
            Debug.Log($"   → 颜色: RGBA({tmpText.color.r}, {tmpText.color.g}, {tmpText.color.b}, {tmpText.color.a})");
        }
        else if (actualTextComponent is Text uiText)
        {
            // 强制激活对象
            if (!hintObj.activeSelf)
            {
                Debug.Log($"   ⚠️ 对象未激活，强制激活...");
                hintObj.SetActive(true);
            }
            
            // 检查并修复位置
            RectTransform textRectTransform2 = uiText.GetComponent<RectTransform>();
            if (textRectTransform2 != null)
            {
                Vector2 currentPos = textRectTransform2.anchoredPosition;
                Debug.Log($"   → 当前位置: {currentPos}");
                
                // 自动检测并调整位置
                bool isSouthUI = hintObj.name.Contains("South") || (GetComponent<South>() != null);
                
                if (isSouthUI)
                {
                    if (currentPos.y < 100 || currentPos.y > 200)
                    {
                        Debug.Log($"   ⚠️ South UI位置不正确({currentPos.y})，调整到150...");
                        textRectTransform2.anchoredPosition = new Vector2(currentPos.x, 150);
                    }
                }
                else
                {
                    if (currentPos.y > -100)
                    {
                        Debug.Log($"   ⚠️ North UI位置不正确({currentPos.y})，调整到-150...");
                        textRectTransform2.anchoredPosition = new Vector2(currentPos.x, -150);
                    }
                }
                
                Debug.Log($"   → 最终位置: {textRectTransform2.anchoredPosition}");
            }
            
            uiText.text = text;
            
            // 确保文本颜色可见
            Color textColor = uiText.color;
            if (textColor.a <= 0.01f)
            {
                Debug.Log($"   ⚠️ 文本透明度为0，设置为白色不透明...");
                uiText.color = new Color(1f, 1f, 1f, 1f);
            }
            
            // 确保字体大小合适
            if (uiText.fontSize < 20)
            {
                Debug.Log($"   ⚠️ 字体大小过小({uiText.fontSize})，设置为36...");
                uiText.fontSize = 36;
            }
            
            bool shouldShow = !isPresentationUiSuppressed && (forceShow || !string.IsNullOrEmpty(text));
            hintObj.SetActive(shouldShow);
            Chapter03PlaqueFrame.ApplyHintFrame(actualTextComponent, shouldShow);
            
            Debug.Log($"   → Active状态: {shouldShow}");
            Debug.Log($"   → 对象实际Active: {hintObj.activeSelf}");
            Debug.Log($"   → 在Hierarchy中Active: {hintObj.activeInHierarchy}");
            Debug.Log($"   → 文本内容: '{uiText.text}'");
            Debug.Log($"   → 字体大小: {uiText.fontSize}");
            Debug.Log($"   → 颜色: RGBA({uiText.color.r}, {uiText.color.g}, {uiText.color.b}, {uiText.color.a})");
        }
        else
        {
            Debug.LogError($"❌ 组件类型不匹配: {actualTextComponent.GetType().Name}");
            Debug.LogError($"💡 期望类型: TextMeshProUGUI 或 Text");
            Debug.LogError($"💡 请确保拖拽的对象有TextMeshProUGUI或Text组件");
        }
    }
    
    /// <summary>
    /// 显示文化提示
    /// </summary>
    void ShowCultureTip()
    {
        if (isPresentationUiSuppressed)
        {
            return;
        }

        if (cultureTipPanel != null)
        {
            cultureTipPanel.SetActive(true);
            
            // 可以在这里设置文化提示的具体内容（支持Text和TextMeshPro）
            Component cultureText = cultureTipPanel.GetComponentInChildren<Component>();
            if (cultureText is TextMeshProUGUI tmpText)
            {
                tmpText.text = "十八曼陀罗花馆，冬春所用。方砖之下有地龙，燃火取暖，满室生春。";
            }
            else if (cultureText is Text uiText)
            {
                uiText.text = "十八曼陀罗花馆，冬春所用。方砖之下有地龙，燃火取暖，满室生春。";
            }
            
            // 5秒后自动隐藏
            StartCoroutine(HideCultureTipAfterDelay(5f));
        }
    }
    
    /// <summary>
    /// 隐藏文化提示
    /// </summary>
    void HideCultureTip()
    {
        if (cultureTipPanel != null)
        {
            cultureTipPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 延迟隐藏文化提示
    /// </summary>
    IEnumerator HideCultureTipAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideCultureTip();
    }
    
    /// <summary>
    /// 延迟清除提示
    /// </summary>
    IEnumerator ClearHintAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UpdateHintText("");
    }
    
    /// <summary>
    /// 诊断UI问题（在Start时调用）
    /// </summary>
    void DiagnoseUIIssues()
    {
        Debug.Log("========== UI诊断报告 ==========");
        
        // 1. 检查Canvas
        if (uiCanvas == null)
        {
            Debug.LogError("❌ Canvas未设置");
            uiCanvas = FindObjectOfType<Canvas>();
            if (uiCanvas != null)
                Debug.Log($"✅ 自动找到Canvas: {uiCanvas.name}");
        }
        else
        {
            Debug.Log($"✅ Canvas: {uiCanvas.name}");
            Debug.Log($"   RenderMode: {uiCanvas.renderMode}");
            Debug.Log($"   Active: {uiCanvas.gameObject.activeInHierarchy}");
        }
        
        // 2. 检查HintText
        if (hintText == null)
        {
            Debug.LogError("❌ HintText未设置");
        }
        else
        {
            Debug.Log($"✅ HintText组件类型: {hintText.GetType().Name}");
            Component hintComp = hintText as Component;
            GameObject hintObj = hintComp.gameObject;
            
            Debug.Log($"   对象名称: {hintObj.name}");
            Debug.Log($"   ActiveSelf: {hintObj.activeSelf}");
            Debug.Log($"   ActiveInHierarchy: {hintObj.activeInHierarchy}");
            
            // 检查颜色
            if (hintText is TextMeshProUGUI tmpText)
            {
                Debug.Log($"   文本内容: '{tmpText.text}'");
                Debug.Log($"   字体大小: {tmpText.fontSize}");
                Debug.Log($"   颜色: {tmpText.color}");
                Debug.Log($"   透明度: {tmpText.color.a}");
            }
            else if (hintText is Text uiText)
            {
                Debug.Log($"   文本内容: '{uiText.text}'");
                Debug.Log($"   字体大小: {uiText.fontSize}");
                Debug.Log($"   颜色: {uiText.color}");
                Debug.Log($"   透明度: {uiText.color.a}");
            }
            
            // 检查位置
            RectTransform rectTransform = hintComp.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Debug.Log($"   锚点: {rectTransform.anchorMin} - {rectTransform.anchorMax}");
                Debug.Log($"   位置: {rectTransform.anchoredPosition}");
                Debug.Log($"   尺寸: {rectTransform.sizeDelta}");
            }
            
            // 检查是否在Canvas下
            if (uiCanvas != null)
            {
                bool isInCanvas = hintComp.transform.IsChildOf(uiCanvas.transform);
                Debug.Log($"   在Canvas下: {isInCanvas}");
            }
        }
        
        // 3. 检查CultureTipPanel
        if (cultureTipPanel == null)
        {
            Debug.LogWarning("⚠️ CultureTipPanel未设置");
        }
        else
        {
            Debug.Log($"✅ CultureTipPanel: {cultureTipPanel.name}");
            Debug.Log($"   Active: {cultureTipPanel.activeInHierarchy}");
        }
        
        // 4. 检查GongcheIconUI
        if (gongcheIconUI == null)
        {
            Debug.LogWarning("⚠️ GongcheIconUI未设置");
        }
        else
        {
            Debug.Log($"✅ GongcheIconUI: {gongcheIconUI.name}");
            Debug.Log($"   Active: {gongcheIconUI.gameObject.activeInHierarchy}");
            Debug.Log($"   Sprite: {(gongcheIconUI.sprite != null ? gongcheIconUI.sprite.name : "null")}");
        }
        
        Debug.Log("================================");
    }
    
    /// <summary>
    /// 创建临时的昆曲工尺谱图标（当未分配Sprite时）
    /// </summary>
    void CreateTemporaryGongcheIcon()
    {
        if (gongcheIconUI == null)
            return;
        
        // 设置一个默认颜色作为临时图标显示
        gongcheIconUI.sprite = null;
        gongcheIconUI.color = new Color(1f, 0.84f, 0f, 1f); // 金色
        
        // 确保大小合适
        RectTransform rectTransform = gongcheIconUI.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(60, 60);
        }
    }
    
    /// <summary>
    /// 配置火焰粒子的颜色渐变，使其看起来更真实
    /// </summary>
    void ConfigureFireParticleColor()
    {
        if (fireParticleSystem == null)
            return;
        
        var colorOverLifetime = fireParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        // 创建渐变色：从亮黄到橙红再到暗红
        Gradient gradient = new Gradient();
        
        GradientColorKey[] colorKeys = new GradientColorKey[4];
        colorKeys[0] = new GradientColorKey(new Color(1f, 1f, 0.8f), 0f);   // 亮黄
        colorKeys[1] = new GradientColorKey(new Color(1f, 0.65f, 0f), 0.3f); // 橙色
        colorKeys[2] = new GradientColorKey(new Color(1f, 0.4f, 0f), 0.5f);  // 深橙
        colorKeys[3] = new GradientColorKey(new Color(0.7f, 0.1f, 0f), 1f);  // 暗红
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[4];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 0.3f);
        alphaKeys[2] = new GradientAlphaKey(0.8f, 0.6f);
        alphaKeys[3] = new GradientAlphaKey(0f, 1f);
        
        gradient.SetKeys(colorKeys, alphaKeys);
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
    }
    
    /// <summary>
    /// 调试：测试声波粒子
    /// 在Console中输入：FindObjectOfType&lt;North&gt;().TestSoundWaveParticles()
    /// </summary>
    public void TestSoundWaveParticles()
    {
        Debug.Log("🧪 测试声波粒子...");
        
        // 查找North脚本
        North northScript = FindObjectOfType<North>();
        if (northScript == null)
        {
            Debug.LogError("❌ 未找到North脚本");
            return;
        }
        
        // 临时设置hasGongcheScore为true
        northScript.SetGongcheScoreStatus(true);
        
        Debug.Log("✅ 已设置拥有工尺谱，现在走到红地毯中心按E键测试粒子效果");
    }
}
