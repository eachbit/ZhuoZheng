using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 北厅传音交互系统 - 卅六鸳鸯馆工尺谱传音机制
/// </summary>
public class North : MonoBehaviour
{
    [Header("触发器引用")]
    [Tooltip("红地毯中心触发器对象")]
    public GameObject redCarpetTrigger;
    
    [Header("场景引用")]
    [Tooltip("主相机")]
    public Camera mainCamera;
    
    [Tooltip("玩家对象（用于定位）")]
    public GameObject playerObject;
    
    [Tooltip("工尺谱卷轴对象（金色卷轴模型）")]
    public GameObject gongcheScrollObject;
    
    [Tooltip("声波粒子系统（沿拱顶弧线飞行）")]
    public ParticleSystem soundWaveParticles;
    
    [Tooltip("隔扇雕花对象（粒子经过时亮起）")]
    public GameObject latticeWindow;
    
    [Tooltip("飞回的残页对象")]
    public GameObject returningPage;
    
    [Header("UI引用")]
    [Tooltip("UI画布")]
    public Canvas uiCanvas;
    
    [Tooltip("提示文本UI（支持Text和TextMeshPro）")]
    public Component hintText;
    
    [Tooltip("文化提示弹窗")]
    public GameObject cultureTipPanel;
    
    [Tooltip("工尺谱图标UI（复用南厅已获取的图标）")]
    public Image gongcheIconUI;
    
    [Header("北厅专属UI")]
    [Tooltip("北厅专用的工尺谱图标UI（可选，如果南厅图标不在北厅Canvas下）")]
    public Image northGongcheIconUI;
    
    [Header("字符亮起效果")]
    [Tooltip("工尺上乙四合六个字符对象")]
    public List<GameObject> characterObjects;
    [Tooltip("字符亮起间隔时间")]
    public float characterLightUpInterval = 0.2f;
    
    [Header("残页飞行动画")]
    [Tooltip("残页飞行时长")]
    public float returningPageFlyDuration = 1.5f;
    [Tooltip("残页飞行高度（抛物线）")]
    public float returningPageFlyHeight = 2f;

    [Header("状态管理")]
    [Tooltip("是否已获得工尺谱（从南厅同步）")]
    [SerializeField]
    private bool hasGongcheScore = false;
    
    [Tooltip("玩家是否在触发器内")]
    private bool isPlayerInTrigger = false;
    
    [Tooltip("是否正在交互中")]
    private bool isInteractionActive = false;
    
    [Tooltip("交互是否已完成")]
    private bool isInteractionCompleted = false;

    void Start()
    {
        Debug.Log("🎬🎬 North.cs Start方法开始执行 🎬🎬");
        Debug.Log($"🎯 North脚本挂载在对象: {gameObject.name}");
        Debug.Log($"🎯 脚本是否启用: {enabled}");
        Debug.Log($"🎯 对象是否激活: {gameObject.activeInHierarchy}");
        
        // 自动查找主相机
        if (mainCamera == null)
        {
            Debug.Log("📷 自动查找主相机...");
            mainCamera = Camera.main;
            if (mainCamera == null)
                Debug.LogError("❌ 未找到主相机！");
            else
                Debug.Log($"✅ 找到主相机: {mainCamera.name}");
        }
        
        // 自动查找玩家对象
        if (playerObject == null)
        {
            Debug.Log("👤 自动查找玩家对象...");
            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
                playerObject = GameObject.Find("Player");
            
            if (playerObject != null)
                Debug.Log($"✅ 找到玩家: {playerObject.name}");
            else
                Debug.LogError("❌ 未找到玩家对象！请确保玩家对象Tag为'Player'或名称为'Player'");
        }
        
        // 设置红地毯触发器
        SetupRedCarpetTrigger();
        
        // 自动查找Book模型
        if (gongcheScrollObject == null)
        {
            Debug.Log("📖 自动查找Book模型...");
            gongcheScrollObject = GameObject.Find("Book");
            if (gongcheScrollObject != null)
                Debug.Log($"✅ 找到Book: {gongcheScrollObject.name}");
            else
                Debug.LogWarning("⚠️ 未找到Book模型");
        }
        
        // 验证UI引用
        Debug.Log("🎨 开始验证UI引用...");
        ValidateUIReferences();
        
        // 隐藏初始对象
        Debug.Log("🙈 开始隐藏初始对象...");
        
        if (gongcheScrollObject != null)
        {
            gongcheScrollObject.SetActive(false);
            Debug.Log($"   ✅ 隐藏残页: {gongcheScrollObject.name}");
        }
        
        if (gongcheIconUI != null)
        {
            gongcheIconUI.gameObject.SetActive(false);
            Debug.Log($"   ✅ 隐藏工尺谱图标: {gongcheIconUI.name}");
        }
        
        if (cultureTipPanel != null)
        {
            cultureTipPanel.SetActive(false);
            Debug.Log($"   ✅ 隐藏文化提示弹窗: {cultureTipPanel.name}");
        }
        
        if (returningPage != null)
        {
            returningPage.SetActive(false);
            Debug.Log($"   ✅ 隐藏残页对象: {returningPage.name}");
        }
        
        if (soundWaveParticles != null)
            soundWaveParticles.Stop();
        
        // 隐藏所有字符
        if (characterObjects != null)
        {
            foreach (GameObject charObj in characterObjects)
            {
                if (charObj != null)
                    charObj.SetActive(false);
            }
            Debug.Log($"   ✅ 隐藏{characterObjects.Count}个字符对象");
        }
        
        Debug.Log("💬 准备更新提示文本...");
        UpdateHintText("");
        
        Debug.Log("✅✅✅ North系统初始化完成 ✅✅✅");
    }

    /// <summary>
    /// 设置红地毯触发器
    /// </summary>
    void SetupRedCarpetTrigger()
    {
        if (redCarpetTrigger == null)
        {
            Debug.LogError("❌ RedCarpetTrigger未设置！请在Inspector中拖拽红地毯触发器对象");
            return;
        }
        
        // 添加或获取触发器检测脚本
        TriggerDetector detector = redCarpetTrigger.GetComponent<TriggerDetector>();
        if (detector == null)
        {
            detector = redCarpetTrigger.AddComponent<TriggerDetector>();
        }
        
        // 配置检测器
        detector.areaName = "红地毯中心";
        detector.OnTriggerEnterAction = OnRedCarpetEnter;
        detector.OnTriggerExitAction = OnRedCarpetExit;
        
        Debug.Log($"✅ 红地毯触发器已配置: {redCarpetTrigger.name}");
    }

    /// <summary>
    /// 红地毯触发器进入回调
    /// </summary>
    void OnRedCarpetEnter(string areaName)
    {
        Debug.Log($"📥 进入{areaName}");
        
        isPlayerInTrigger = true;
        
        if (isInteractionCompleted)
            return;
        
        // 显示初始提示
        ShowInitialHint();
    }

    /// <summary>
    /// 红地毯触发器离开回调
    /// </summary>
    void OnRedCarpetExit(string areaName)
    {
        Debug.Log($"📤 离开{areaName}");
        
        isPlayerInTrigger = false;
        
        // 清除提示
        UpdateHintText("");
    }

    /// <summary>
    /// 显示初始提示
    /// </summary>
    void ShowInitialHint()
    {
        UpdateHintText("此乃拍曲之地，站此可传音");
    }

    void Update()
    {
        // 检测E键交互 - 只要交互没完成，就可以按E开始交互
        if (!isInteractionCompleted && Input.GetKeyDown(KeyCode.E))
        {
            // 如果交互正在进行中，忽略按键
            if (isInteractionActive)
            {
                Debug.Log($"⚠️ 交互正在进行中，忽略E键");
                return;
            }
            
            Debug.Log($"⌨️ North开始E键交互 | hasGongcheScore: {hasGongcheScore} | isPlayerInTrigger: {isPlayerInTrigger}");
            
            // 检查是否在触发器内
            if (!isPlayerInTrigger)
            {
                return;
            }
            
            isInteractionActive = true;
            StartCoroutine(StartSoundWaveInteraction());
        }
    }

    /// <summary>
    /// 开始声波传音交互序列
    /// </summary>
    IEnumerator StartSoundWaveInteraction()
    {
        // 步骤①：播放声波粒子（即时反馈）
        Debug.Log("🎵 播放声波粒子...");
        if (soundWaveParticles != null)
        {
            soundWaveParticles.Play();
        }
        
        // 等待声波粒子播放一段时间
        yield return new WaitForSeconds(0.5f);
        
        // 步骤②：根据是否有工尺谱显示不同提示
        if (!hasGongcheScore)
        {
            UpdateHintText("声波已至，但无曲谱引路。工尺谱可引残页归来。");
            Debug.Log("⚠️ 无工尺谱，无法获取残页");
            
            // 等待提示显示
            yield return new WaitForSeconds(3f);
            UpdateHintText("");
            
            // 停止粒子
            if (soundWaveParticles != null)
                soundWaveParticles.Stop();
            
            isInteractionActive = false;
            yield break;
        }
        
        // 有工尺谱的情况
        UpdateHintText("工尺谱为引，声波为媒");
        Debug.Log("✅ 有工尺谱，残页即将飞回");
        
        // 步骤③：工尺谱从玩家身上飞起悬浮（核心信息展示）
        Debug.Log("📜 工尺谱飞起悬浮...");
        if (gongcheScrollObject != null)
        {
            gongcheScrollObject.SetActive(true);
            
            // 工尺谱飞到玩家面前悬浮
            if (playerObject != null)
            {
                yield return StartCoroutine(FlyGongcheToPlayer());
            }
        }
        
        // 等待工尺谱悬浮
        yield return new WaitForSeconds(0.5f);
        
        // 步骤④：字符逐个点亮（工→尺→上→乙→四→合）
        Debug.Log("✨ 开始点亮字符...");
        foreach (GameObject character in characterObjects)
        {
            if (character != null)
            {
                character.SetActive(true);
                Debug.Log($"  点亮字符: {character.name}");
                yield return new WaitForSeconds(characterLightUpInterval);
            }
        }
        
        // 等待字符全部亮起
        yield return new WaitForSeconds(1.0f);
        
        // 步骤⑤：声波粒子沿拱顶弧线飞向南厅（2秒）
        Debug.Log("🌊 声波粒子飞向南厅...");
        UpdateHintText("声波已至，残页应声而来");
        yield return new WaitForSeconds(2f);
        
        // 步骤⑥：残页飞回北厅
        Debug.Log("📜 残页开始飞回...");
        if (returningPage != null && playerObject != null)
        {
            returningPage.SetActive(true);
            yield return StartCoroutine(FlyReturningPageToPlayer());
        }
        
        // 步骤⑦：显示获得提示
        UpdateHintText("获得《长物志》残页。");
        Debug.Log("✅ 获得残页");
        
        // 等待提示显示
        yield return new WaitForSeconds(1.5f);
        
        // 步骤⑧：完成提示
        UpdateHintText("获得《长物志》残页。双厅协作，声传千里。");
        isInteractionCompleted = true;
        
        // 等待提示显示
        yield return new WaitForSeconds(3f);
        UpdateHintText("");
        
        // 停止粒子
        if (soundWaveParticles != null)
            soundWaveParticles.Stop();
        
        Debug.Log("✅ 北厅传音交互完成");
    }

    /// <summary>
    /// 残页飞向玩家的动画（抛物线轨迹）
    /// </summary>
    IEnumerator FlyReturningPageToPlayer()
    {
        if (returningPage == null || playerObject == null)
            yield break;
        
        Vector3 startPos = returningPage.transform.position;
        Vector3 endPos = playerObject.transform.position + Vector3.up * 1.5f;
        float duration = returningPageFlyDuration;
        float elapsed = 0f;
        
        Debug.Log($"  残页从 {startPos} 飞向 {endPos}");
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 使用抛物线轨迹
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            currentPos += Vector3.up * Mathf.Sin(t * Mathf.PI) * returningPageFlyHeight;
            
            returningPage.transform.position = currentPos;
            
            // 残页旋转效果
            returningPage.transform.Rotate(Vector3.up, 360f * Time.deltaTime * 2f);
            
            yield return null;
        }
        
        // 确保到达目标位置
        returningPage.transform.position = endPos;
        Debug.Log("  ✅ 残页已到达玩家位置");
    }

    /// <summary>
    /// 工尺谱从玩家身上飞起并悬浮的动画
    /// </summary>
    IEnumerator FlyGongcheToPlayer()
    {
        if (gongcheScrollObject == null || playerObject == null)
            yield break;
        
        Vector3 startPos = playerObject.transform.position + Vector3.up * 0.5f; // 从玩家身上开始
        Vector3 endPos = playerObject.transform.position + Vector3.forward * 1.5f + Vector3.up * 1.5f; // 飞到玩家面前上方
        
        gongcheScrollObject.transform.position = startPos;
        gongcheScrollObject.SetActive(true);
        
        float duration = 1.0f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 使用缓动函数使运动更平滑
            float smoothT = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
            
            gongcheScrollObject.transform.position = Vector3.Lerp(startPos, endPos, smoothT);
            
            // 工尺谱旋转效果
            gongcheScrollObject.transform.Rotate(Vector3.up, 360f * Time.deltaTime);
            
            yield return null;
        }
        
        // 确保到达目标位置
        gongcheScrollObject.transform.position = endPos;
        Debug.Log("  ✅ 工尺谱已悬浮在玩家面前");
    }

    /// <summary>
    /// 更新提示文本
    /// </summary>
    void UpdateHintText(string text)
    {
        if (hintText == null)
        {
            Debug.LogWarning("⚠️ hintText为null，无法更新提示文本: " + text);
            Debug.LogWarning("💡 请在Inspector中将HintText对象下的Text/TextMeshProUGUI组件拖拽到Hint Text字段");
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
        
        // 检查是否在Canvas下
        Canvas canvas = hintObj.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ 提示文本对象不在Canvas下！这会导致UI无法显示");
            Debug.Log("   💡 请将HintText对象拖拽到 _03_Canvas 下");
        }
        else
        {
            Debug.Log($"   ✅ 在Canvas下: {canvas.name}");
        }
        
        if (actualTextComponent is TextMeshProUGUI tmpText)
        {
            // 强制激活对象
            if (!hintObj.activeSelf)
            {
                Debug.Log($"   ⚠️ 对象未激活，强制激活...");
                hintObj.SetActive(true);
            }
            
            // 检查并修复位置（North的UI应该显示在顶部，Y=-150）
            RectTransform textRectTransform = tmpText.GetComponent<RectTransform>();
            if (textRectTransform != null)
            {
                Vector2 currentPos = textRectTransform.anchoredPosition;
                Debug.Log($"   → 当前位置: {currentPos}");
                
                // North UI显示在顶部（Y=-150）
                if (currentPos.y > -100)
                {
                    Debug.Log($"   ⚠️ North UI位置不正确({currentPos.y})，调整到-150...");
                    textRectTransform.anchoredPosition = new Vector2(currentPos.x, -150);
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
            
            bool shouldShow = !string.IsNullOrEmpty(text);
            hintObj.SetActive(shouldShow);
            
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
                
                // North UI显示在顶部（Y=-150）
                if (currentPos.y > -100)
                {
                    Debug.Log($"   ⚠️ North UI位置不正确({currentPos.y})，调整到-150...");
                    textRectTransform2.anchoredPosition = new Vector2(currentPos.x, -150);
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
            
            bool shouldShow = !string.IsNullOrEmpty(text);
            hintObj.SetActive(shouldShow);
            
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
        }
    }

    /// <summary>
    /// 设置工尺谱获取状态（由南厅调用）
    /// </summary>
    /// <param name="hasScore">是否已获得工尺谱</param>
    public void SetGongcheScoreStatus(bool hasScore)
    {
        hasGongcheScore = hasScore;
        Debug.Log($"北厅工尺谱状态已更新: {(hasScore ? "已获得" : "未获得")}");
    }

    /// <summary>
    /// 获取工尺谱获取状态
    /// </summary>
    public bool HasGongcheScore()
    {
        return hasGongcheScore;
    }

    /// <summary>
    /// 诊断触发器问题
    /// 在Console中输入：FindObjectOfType&lt;North&gt;().DiagnoseTrigger()
    /// </summary>
    public void DiagnoseTrigger()
    {
        Debug.Log("========== 触发器诊断报告 ==========");
        
        // 1. 检查redCarpetTrigger引用
        if (redCarpetTrigger == null)
        {
            Debug.LogError("❌ redCarpetTrigger为null！请在Inspector中拖拽红地毯触发器对象");
        }
        else
        {
            Debug.Log($"✅ redCarpetTrigger引用: {redCarpetTrigger.name}");
            
            // 2. 检查Collider
            Collider col = redCarpetTrigger.GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogError("❌ 触发器没有Collider组件！");
            }
            else
            {
                Debug.Log($"✅ Collider类型: {col.GetType().Name}");
                Debug.Log($"✅ Is Trigger: {col.isTrigger}");
                Debug.Log($"✅ 中心点: {col.bounds.center}");
                Debug.Log($"✅ 尺寸: {col.bounds.size}");
                
                if (!col.isTrigger)
                {
                    Debug.LogError("❌ Collider未勾选Is Trigger！请在Inspector中勾选");
                }
            }
            
            // 3. 检查Scale
            Vector3 scale = redCarpetTrigger.transform.localScale;
            Debug.Log($"✅ Scale: {scale}");
            if (scale.x <= 0 || scale.y <= 0 || scale.z <= 0)
            {
                Debug.LogError("❌ Scale有负数或零！请重置Transform");
            }
            
            // 4. 检查TriggerDetector组件
            TriggerDetector detector = redCarpetTrigger.GetComponent<TriggerDetector>();
            if (detector == null)
            {
                Debug.LogError("❌ 没有TriggerDetector组件！");
            }
            else
            {
                Debug.Log($"✅ TriggerDetector组件存在");
                Debug.Log($"✅ areaName: {detector.areaName}");
                Debug.Log($"✅ OnTriggerEnterAction: {(detector.OnTriggerEnterAction != null ? "已设置" : "未设置")}");
                Debug.Log($"✅ OnTriggerExitAction: {(detector.OnTriggerExitAction != null ? "已设置" : "未设置")}");
            }
        }
        
        // 5. 检查玩家对象
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("❌ 找不到Tag为'Player'的对象！");
        }
        else
        {
            Debug.Log($"✅ 玩家对象: {player.name}");
            Debug.Log($"✅ 玩家Tag: {player.tag}");
            
            // 检查玩家的Collider和Rigidbody
            Collider playerCol = player.GetComponent<Collider>();
            if (playerCol == null)
            {
                Debug.LogWarning("⚠️ 玩家没有Collider组件！");
            }
            else
            {
                Debug.Log($"✅ 玩家Collider类型: {playerCol.GetType().Name}");
                Debug.Log($"✅ 玩家Collider Is Trigger: {playerCol.isTrigger}");
                
                if (playerCol.isTrigger)
                {
                    Debug.LogWarning("⚠️ 玩家的Collider勾选了Is Trigger！这可能导致触发器无法工作");
                }
            }
            
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb == null)
            {
                Debug.LogWarning("⚠️ 玩家没有Rigidbody组件！触发器可能需要Rigidbody才能工作");
            }
            else
            {
                Debug.Log($"✅ 玩家Rigidbody存在");
                Debug.Log($"✅ 玩家Rigidbody Is Kinematic: {playerRb.isKinematic}");
            }
        }
        
        // 6. 检查Layer碰撞矩阵
        Debug.Log("⚠️ 请手动检查：Edit -> Project Settings -> Physics -> Layer Collision Matrix");
        Debug.Log("   确保玩家所在Layer和触发器所在Layer可以碰撞");
        
        Debug.Log("======================================");
    }

    /// <summary>
    /// 验证UI引用（基础实现）
    /// </summary>
    void ValidateUIReferences()
    {
        if (hintText == null)
        {
            Debug.LogWarning("⚠️ hintText未设置，请在Inspector中配置");
        }
        else
        {
            Debug.Log($"✅ hintText已设置: {hintText.GetType().Name}");
        }
    }

    /// <summary>
    /// 诊断North UI问题
    /// 在Console中输入：FindObjectOfType&lt;North&gt;().DiagnoseUI()
    /// </summary>
    public void DiagnoseUI()
    {
        Debug.Log("========== North UI诊断 ==========");
        
        if (hintText == null)
        {
            Debug.LogError("❌ hintText为null");
            return;
        }
        
        Component hintComp = hintText as Component;
        GameObject hintObj = hintComp.gameObject;
        
        Debug.Log($"✅ 对象名称: {hintObj.name}");
        Debug.Log($"✅ Active: {hintObj.activeInHierarchy}");
        
        RectTransform rectTransform = hintComp.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Debug.Log($"✅ 锚点: {rectTransform.anchorMin} - {rectTransform.anchorMax}");
            Debug.Log($"✅ 位置: {rectTransform.anchoredPosition}");
            Debug.Log($"✅ 尺寸: {rectTransform.sizeDelta}");
            
            // 检查是否在屏幕内
            Vector2 pos = rectTransform.anchoredPosition;
            if (pos.y > 300 || pos.y < -400)
            {
                Debug.LogError($"❌ Y坐标{pos.y}可能在屏幕外！");
            }
            else
            {
                Debug.Log($"✅ Y坐标{pos.y}在屏幕内");
            }
        }
        
        if (hintText is TextMeshProUGUI tmpText)
        {
            Debug.Log($"✅ 文本: '{tmpText.text}'");
            Debug.Log($"✅ 字体大小: {tmpText.fontSize}");
            Debug.Log($"✅ 颜色: RGBA({tmpText.color.r}, {tmpText.color.g}, {tmpText.color.b}, {tmpText.color.a})");
        }
        
        // 检查Canvas
        Canvas canvas = hintObj.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"✅ 在Canvas下: {canvas.name}");
            Debug.Log($"✅ Canvas RenderMode: {canvas.renderMode}");
        }
        else
        {
            Debug.LogError("❌ 不在Canvas下！");
        }
        
        Debug.Log("================================");
    }
}