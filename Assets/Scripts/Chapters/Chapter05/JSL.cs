using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 添加TextMeshPro支持

public class JSL : MonoBehaviour
{
    [Header("碰撞体引用")]
    public Collider northCollider;   // 北面碰撞体
    public Collider eastCollider;    // 东面碰撞体
    public Collider southCollider;   // 南面碰撞体
    public Collider westCollider;    // 西面碰撞体
    
    [Header("UI引用")]
    public GameObject systemPromptPanel;     // 系统提示面板
    public GameObject systemPromptTextObject; // 系统提示文本对象(脚本自动查找Text组件)
    
    public GameObject cultureCardPanel;      // 文化卡牌面板
    public GameObject uiHintPanel;           // UI交互提示面板
    public GameObject uiHintTextObject;      // UI提示文本对象(脚本自动查找Text组件)
    
    [Header("镜头控制")]
    public Camera mainCamera;                // 主相机
    public Transform cameraPivot;            // 镜头旋转中心点(见山楼位置)
    
    [Header("残页效果")]
    public GameObject pagePrefab;            // 残页预制体(可选,如果为空则自动创建Cube)
    public Transform pageSpawnPoint;         // 残页生成点(匾额位置)
    public Material pageMaterial;            // 残页材质(可选,让Cube更好看)
    
    // 状态标记
    private bool hasTriggeredNorth = false;
    private bool hasTriggeredEast = false;
    private bool hasTriggeredSouth = false;
    private bool hasTriggeredWest = false;
    private bool allDirectionsVisited = false;
    private bool isLookingUp = false;
    private bool hasCompletedLookUp = false;
    private bool isCardShowing = false;
    private bool hasCollectedPage = false;
    private bool isLevelComplete = false;
    
    // 协程引用
    private Coroutine currentPromptCoroutine = null;
    
    void Start()
    {
        Debug.Log("=== JSL脚本启动 ===");
        
        // 初始化UI状态
        if (systemPromptPanel != null)
        {
            systemPromptPanel.SetActive(false);
            Debug.Log("SystemPromptPanel 已隐藏");
        }
        else
        {
            Debug.LogWarning("SystemPromptPanel 未赋值!");
        }
        
        if (cultureCardPanel != null)
        {
            cultureCardPanel.SetActive(false);
            Debug.Log("CultureCardPanel 已隐藏");
        }
        
        if (uiHintPanel != null)
        {
            uiHintPanel.SetActive(false);
            Debug.Log("UIHintPanel 已隐藏");
        }
        
        // 检查碰撞体配置
        Debug.Log("=== 碰撞体配置检查 ===");
        Debug.Log("northCollider: " + (northCollider != null ? northCollider.gameObject.name : "未赋值"));
        Debug.Log("eastCollider: " + (eastCollider != null ? eastCollider.gameObject.name : "未赋值"));
        Debug.Log("southCollider: " + (southCollider != null ? southCollider.gameObject.name : "未赋值"));
        Debug.Log("westCollider: " + (westCollider != null ? westCollider.gameObject.name : "未赋值"));
        
        // 确保碰撞体触发器启用
        SetupColliders();
        
        // 检查玩家对象
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Debug.Log("找到玩家对象: " + player.name);
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Debug.Log("玩家有Rigidbody组件 ✓");
            }
            else
            {
                Debug.LogError("玩家没有Rigidbody组件! OnTriggerEnter不会被调用!");
            }
        }
        else
        {
            Debug.LogError("没有找到Tag为'Player'的对象!");
        }
    }
    
    void Update()
    {
        // 测试用: 按T键手动触发所有方位(仅用于调试)
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("[测试] 手动触发所有方位");
            TriggerNorth();
            TriggerEast();
            TriggerSouth();
            TriggerWest();
        }
        
        // 备用方案: 使用距离检测(如果OnTriggerEnter不工作)
        CheckDirectionByDistance();
        
        // 检查是否所有方位都已访问
        CheckAllDirectionsVisited();
        
        // 按E键仰观见山楼
        if (Input.GetKeyDown(KeyCode.E) && allDirectionsVisited && !isLookingUp && !hasCompletedLookUp)
        {
            StartCoroutine(LookUpAnimation());
        }
        
        // 按空格键关闭文化卡牌
        if (Input.GetKeyDown(KeyCode.Space) && isCardShowing)
        {
            CloseCultureCard();
        }
        
        // 按E键拾取残页
        if (Input.GetKeyDown(KeyCode.E) && hasCompletedLookUp && !hasCollectedPage && !isCardShowing)
        {
            CollectPage();
        }
    }
    
    /// <summary>
    /// 设置碰撞体为触发器模式
    /// </summary>
    void SetupColliders()
    {
        Collider[] colliders = { northCollider, eastCollider, southCollider, westCollider };
        foreach (var collider in colliders)
        {
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }
    }
    
    /// <summary>
    /// 检查是否所有方位都已访问
    /// </summary>
    void CheckAllDirectionsVisited()
    {
        if (!allDirectionsVisited && hasTriggeredNorth && hasTriggeredEast && 
            hasTriggeredSouth && hasTriggeredWest)
        {
            allDirectionsVisited = true;
            ShowSystemPrompt(new string[] {
                "三面环水,西山连陆。下层藕香榭,上层见山楼。"
            }, 4f);
            
            // 显示文化卡牌
            StartCoroutine(ShowCultureCardAfterDelay(2f));
            
            // 显示UI交互提示
            ShowUIHint("按 E 仰观见山楼");
        }
    }
    
    /// <summary>
    /// 触发器进入事件
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("=== OnTriggerEnter 被调用 ===");
        Debug.Log("进入的物体名称: " + other.gameObject.name);
        Debug.Log("进入的物体Tag: " + other.tag);
        Debug.Log("进入的物体Layer: " + LayerMask.LayerToName(other.gameObject.layer));
        
        // 检查是否是玩家进入
        bool isPlayer = other.CompareTag("Player") || other.gameObject.name == "Player";
        
        Debug.Log("是否是玩家: " + isPlayer);
        
        if (!isPlayer)
        {
            Debug.Log("不是玩家,忽略");
            return;
        }
        
        // 获取触发这个事件的碰撞体所在的GameObject
        string colliderName = other.gameObject.name;
        
        Debug.Log($"[JSL] 玩家进入碰撞体: {colliderName}");
        
        // 方案1: 如果碰撞体本身有特定名称
        if (colliderName.Contains("North") || colliderName.Contains("北"))
        {
            TriggerNorth();
        }
        else if (colliderName.Contains("East") || colliderName.Contains("东"))
        {
            TriggerEast();
        }
        else if (colliderName.Contains("South") || colliderName.Contains("南"))
        {
            TriggerSouth();
        }
        else if (colliderName.Contains("West") || colliderName.Contains("西"))
        {
            TriggerWest();
        }
        else
        {
            Debug.Log("碰撞体名称不匹配,尝试通过位置判断");
            // 方案2: 通过位置关系判断(备用方案)
            DetectDirectionByPosition(other.transform.position);
        }
    }
    
    /// <summary>
    /// 触发器保持事件(持续检测)
    /// </summary>
    void OnTriggerStay(Collider other)
    {
        // 可选: 持续检测玩家是否在碰撞体内
    }
    
    /// <summary>
    /// 触发器退出事件
    /// </summary>
    void OnTriggerExit(Collider other)
    {
        Debug.Log("=== OnTriggerExit ===");
        Debug.Log("离开的物体: " + other.gameObject.name);
    }
    
    /// <summary>
    /// 触发北面事件
    /// </summary>
    void TriggerNorth()
    {
        Debug.Log("[JSL] 触发北面");
        if (!hasTriggeredNorth)
        {
            hasTriggeredNorth = true;
            ShowSystemPrompt(new string[] {
                "北面环水,楼在水中。",
                "见山楼北、东、南三面皆水,唯西面连陆。",
                "楼如浮于水上,倒影入池。"
            }, 5f);
        }
    }
    
    /// <summary>
    /// 触发东面事件
    /// </summary>
    void TriggerEast()
    {
        Debug.Log("[JSL] 触发东面");
        if (!hasTriggeredEast)
        {
            hasTriggeredEast = true;
            ShowSystemPrompt(new string[] {
                "东面临池,倒影入画。",
                "楼影与真身相接,虚实之间。",
                "水面如镜,将楼一分为二——一实一虚。"
            }, 5f);
        }
    }
    
    /// <summary>
    /// 触发南面事件
    /// </summary>
    void TriggerSouth()
    {
        Debug.Log("[JSL] 触发南面");
        if (!hasTriggeredSouth)
        {
            hasTriggeredSouth = true;
            ShowSystemPrompt(new string[] {
                "南面开阔,远山可借。",
                "'见山楼'之名,取自陶渊明'采菊东篱下,悠然见南山'。",
                "站于此位,园外山影可入眼帘——此乃借景。"
            }, 5f);
        }
    }
    
    /// <summary>
    /// 触发西面事件
    /// </summary>
    void TriggerWest()
    {
        Debug.Log("[JSL] 触发西面");
        if (!hasTriggeredWest)
        {
            hasTriggeredWest = true;
            ShowSystemPrompt(new string[] {
                "西侧假山,登楼之道。",
                "二楼入口不在楼内,而藏于西侧假山之中。",
                "拾级而上,可登楼而不入室——苏州园林孤例。"
            }, 5f);
        }
    }
    
    /// <summary>
    /// 通过玩家位置判断方位(备用方案)
    /// </summary>
    void DetectDirectionByPosition(Vector3 playerPosition)
    {
        if (cameraPivot == null)
            return;
        
        Vector3 direction = playerPosition - cameraPivot.position;
        
        // 假设cameraPivot在见山楼中心
        // 根据相对位置判断方位
        if (direction.z > 0 && Mathf.Abs(direction.x) < Mathf.Abs(direction.z))
        {
            TriggerNorth(); // 北
        }
        else if (direction.x > 0 && Mathf.Abs(direction.z) < Mathf.Abs(direction.x))
        {
            TriggerEast();  // 东
        }
        else if (direction.z < 0 && Mathf.Abs(direction.x) < Mathf.Abs(direction.z))
        {
            TriggerSouth(); // 南
        }
        else if (direction.x < 0 && Mathf.Abs(direction.z) < Mathf.Abs(direction.x))
        {
            TriggerWest();  // 西
        }
    }
    
    /// <summary>
    /// 通过距离检测方位(备用方案,不依赖碰撞体)
    /// </summary>
    void CheckDirectionByDistance()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
            return;
        
        Vector3 playerPos = player.transform.position;
        float triggerDistance = 3f; // 触发距离
        
        // 检测北面
        if (!hasTriggeredNorth && northCollider != null)
        {
            Vector3 northPos = northCollider.transform.position;
            // 检查玩家是否在碰撞体范围内
            if (Vector3.Distance(playerPos, northPos) < triggerDistance && 
                IsInColliderBounds(northCollider, playerPos))
            {
                TriggerNorth();
            }
        }
        
        // 检测东面
        if (!hasTriggeredEast && eastCollider != null)
        {
            Vector3 eastPos = eastCollider.transform.position;
            if (Vector3.Distance(playerPos, eastPos) < triggerDistance && 
                IsInColliderBounds(eastCollider, playerPos))
            {
                TriggerEast();
            }
        }
        
        // 检测南面
        if (!hasTriggeredSouth && southCollider != null)
        {
            Vector3 southPos = southCollider.transform.position;
            if (Vector3.Distance(playerPos, southPos) < triggerDistance && 
                IsInColliderBounds(southCollider, playerPos))
            {
                TriggerSouth();
            }
        }
        
        // 检测西面
        if (!hasTriggeredWest && westCollider != null)
        {
            Vector3 westPos = westCollider.transform.position;
            if (Vector3.Distance(playerPos, westPos) < triggerDistance && 
                IsInColliderBounds(westCollider, playerPos))
            {
                TriggerWest();
            }
        }
    }
    
    /// <summary>
    /// 检查点是否在碰撞体范围内
    /// </summary>
    bool IsInColliderBounds(Collider collider, Vector3 point)
    {
        Bounds bounds = collider.bounds;
        return bounds.Contains(point);
    }
    
    /// <summary>
    /// 显示系统提示(逐行显示)
    /// </summary>
    void ShowSystemPrompt(string[] lines, float displayDuration)
    {
        if (currentPromptCoroutine != null)
            StopCoroutine(currentPromptCoroutine);
        
        currentPromptCoroutine = StartCoroutine(DisplayPromptLines(lines, displayDuration));
    }
    
    /// <summary>
    /// 逐行显示提示文本
    /// </summary>
    IEnumerator DisplayPromptLines(string[] lines, float totalDuration)
    {
        if (systemPromptPanel != null)
        {
            systemPromptPanel.SetActive(true);
            
            // 自动设置Panel背景为半透明黑色,防止泛白
            SetPanelBackground(systemPromptPanel, new Color(0f, 0f, 0f, 0.7f));
        }
        
        // 获取文本组件
        Component textComp = GetTextComponent(systemPromptTextObject);
        
        // 清除现有文本
        SetTextContent(textComp, "");
        
        float timePerLine = totalDuration / lines.Length;
        
        for (int i = 0; i < lines.Length; i++)
        {
            string currentText = "";
            if (i > 0)
                currentText = GetTextContent(textComp) + "\n";
            currentText += lines[i];
            
            SetTextContent(textComp, currentText);
            
            yield return new WaitForSeconds(timePerLine);
        }
        
        // 等待一段时间后隐藏
        yield return new WaitForSeconds(1f);
        
        if (systemPromptPanel != null)
            systemPromptPanel.SetActive(false);
    }
    
    /// <summary>
    /// 显示UI交互提示
    /// </summary>
    void ShowUIHint(string hintText)
    {
        if (uiHintPanel != null)
        {
            uiHintPanel.SetActive(true);
            
            // 自动设置Panel背景为完全透明或轻微半透明
            SetPanelBackground(uiHintPanel, new Color(0f, 0f, 0f, 0.5f));
            
            Component textComp = GetTextComponent(uiHintTextObject);
            SetTextContent(textComp, hintText);
        }
    }
    
    /// <summary>
    /// 从GameObject获取文本组件
    /// </summary>
    Component GetTextComponent(GameObject obj)
    {
        if (obj == null)
            return null;
        
        // 先尝试获取传统Text
        Text uiText = obj.GetComponent<Text>();
        if (uiText != null)
            return uiText;
        
        // 再尝试获取TextMeshPro
        TextMeshProUGUI tmpText = obj.GetComponent<TextMeshProUGUI>();
        if (tmpText != null)
            return tmpText;
        
        // 如果都没有,尝试从子对象查找
        Text childUiText = obj.GetComponentInChildren<Text>();
        if (childUiText != null)
            return childUiText;
        
        TextMeshProUGUI childTmpText = obj.GetComponentInChildren<TextMeshProUGUI>();
        if (childTmpText != null)
            return childTmpText;
        
        Debug.LogWarning("未找到文本组件: " + obj.name);
        return null;
    }
    
    /// <summary>
    /// 获取文本内容(兼容Text和TextMeshPro)
    /// </summary>
    string GetTextContent(Component textComp)
    {
        if (textComp == null)
            return "";
        
        if (textComp is Text)
            return (textComp as Text).text;
        else if (textComp is TextMeshProUGUI)
            return (textComp as TextMeshProUGUI).text;
        
        return "";
    }
    
    /// <summary>
    /// 设置文本内容(兼容Text和TextMeshPro)
    /// </summary>
    void SetTextContent(Component textComp, string content)
    {
        if (textComp == null)
            return;
        
        if (textComp is Text)
            (textComp as Text).text = content;
        else if (textComp is TextMeshProUGUI)
            (textComp as TextMeshProUGUI).text = content;
    }
    
    /// <summary>
    /// 隐藏UI交互提示
    /// </summary>
    void HideUIHint()
    {
        if (uiHintPanel != null)
            uiHintPanel.SetActive(false);
    }
    
    /// <summary>
    /// 仰观见山楼动画
    /// </summary>
    IEnumerator LookUpAnimation()
    {
        isLookingUp = true;
        HideUIHint();
        
        float duration = 4f;
        float elapsedTime = 0f;
        
        // 初始角度和位置
        Vector3 initialPosition = mainCamera.transform.position;
        Quaternion initialRotation = mainCamera.transform.rotation;
        
        // 目标角度(上仰)
        Quaternion targetRotation = Quaternion.Euler(30f, initialRotation.eulerAngles.y, initialRotation.eulerAngles.z);
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // 平滑插值
            mainCamera.transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, t);
            
            // 根据阶段显示不同的提示
            if (t < 0.375f) // 0-1.5秒
            {
                ShowSystemPrompt(new string[] { "下层藕香榭,临水敞轩。" }, 1.5f);
                HighlightElement("藕香榭");
            }
            else if (t < 0.75f) // 1.5-3秒
            {
                ShowSystemPrompt(new string[] { "上层见山楼,凭窗远眺。" }, 1.5f);
                HighlightElement("见山楼");
            }
            else // 3-4秒
            {
                ShowSystemPrompt(new string[] { "西侧假山有石阶,可登楼而不入室。" }, 1f);
                HighlightElement("石阶");
            }
            
            yield return null;
        }
        
        // 动画完成
        isLookingUp = false;
        hasCompletedLookUp = true;
        
        // 显示后续提示
        yield return new WaitForSeconds(0.5f);
        ShowSystemPrompt(new string[] {
            "一座楼,两个名字。",
            "下层藕香榭写实——临水观鱼,荷香四溢。",
            "上层见山楼写意——凭窗远眺,悠然见山。"
        }, 4f);
        
        // 1.5秒后弹出文化卡牌
        yield return new WaitForSeconds(1.5f);
        ShowDetailedCultureCard();
        
        // 显示拾取提示
        yield return new WaitForSeconds(2f);
        ShowUIHint("按 E 拾取《长物志》残页");
    }
    
    /// <summary>
    /// 高亮显示元素(简化版本,实际项目中需要实现具体的高亮效果)
    /// </summary>
    void HighlightElement(string elementName)
    {
        // TODO: 实现具体的元素高亮效果
        Debug.Log("高亮显示: " + elementName);
    }
    
    /// <summary>
    /// 延迟显示文化卡牌
    /// </summary>
    IEnumerator ShowCultureCardAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowSimpleCultureCard();
    }
    
    /// <summary>
    /// 显示简化的文化卡牌
    /// </summary>
    void ShowSimpleCultureCard()
    {
        if (cultureCardPanel != null)
        {
            cultureCardPanel.SetActive(true);
            
            // 设置卡牌背景为深色半透明
            SetPanelBackground(cultureCardPanel, new Color(0.08f, 0.08f, 0.12f, 0.9f));
            
            isCardShowing = true;
            
            // TODO: 在这里设置卡牌内容
            // 可以使用Text组件或Image组件显示卡牌信息
        }
    }
    
    /// <summary>
    /// 显示详细的文化卡牌
    /// </summary>
    void ShowDetailedCultureCard()
    {
        if (cultureCardPanel != null)
        {
            cultureCardPanel.SetActive(true);
            
            // 设置卡牌背景为深色半透明
            SetPanelBackground(cultureCardPanel, new Color(0.08f, 0.08f, 0.12f, 0.9f));
            
            isCardShowing = true;
            
            // TODO: 在这里设置详细卡牌内容
            // 包含三重境界的详细信息
        }
    }
    
    /// <summary>
    /// 关闭文化卡牌
    /// </summary>
    void CloseCultureCard()
    {
        if (cultureCardPanel != null)
            cultureCardPanel.SetActive(false);
        
        isCardShowing = false;
    }
    
    /// <summary>
    /// 拾取残页
    /// </summary>
    void CollectPage()
    {
        hasCollectedPage = true;
        HideUIHint();
        
        // 生成残页并播放飞行动画
        if (pageSpawnPoint != null)
        {
            GameObject page;
            
            // 如果提供了预制体,使用预制体;否则创建Cube
            if (pagePrefab != null)
            {
                page = Instantiate(pagePrefab, pageSpawnPoint.position, Quaternion.identity);
            }
            else
            {
                // 创建Cube作为残页
                page = CreatePageCube();
                page.transform.position = pageSpawnPoint.position;
            }
            
            StartCoroutine(FlyPageToPlayer(page));
        }
        
        // 显示获得提示
        ShowSystemPrompt(new string[] {
            "获得《长物志》残页。",
            "见山识楼,心自此明。"
        }, 4f);
        
        // 关卡完成
        StartCoroutine(CompleteLevel());
    }
    
    /// <summary>
    /// 创建残页Cube
    /// </summary>
    GameObject CreatePageCube()
    {
        // 创建Cube
        GameObject page = GameObject.CreatePrimitive(PrimitiveType.Cube);
        page.name = "JSL_Page";
        
        // 设置为扁平形状(像一张纸)
        page.transform.localScale = new Vector3(0.3f, 0.02f, 0.4f);
        
        // 应用材质(如果有)
        if (pageMaterial != null)
        {
            Renderer renderer = page.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = pageMaterial;
        }
        else
        {
            // 默认使用淡黄色材质(像古旧纸张)
            Renderer renderer = page.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.96f, 0.87f, 0.70f, 1f); // 米黄色
            }
        }
        
        // 添加发光效果(可选)
        SetupPageEffects(page);
        
        return page;
    }
    
    /// <summary>
    /// 设置残页特效
    /// </summary>
    void SetupPageEffects(GameObject page)
    {
        // 可以在这里添加粒子效果、光晕等
        // 例如:添加点光源让残页发光
        
        Light pageLight = page.AddComponent<Light>();
        pageLight.type = LightType.Point;
        pageLight.color = new Color(1f, 0.9f, 0.6f, 1f); // 暖黄色光
        pageLight.intensity = 0.5f;
        pageLight.range = 2f;
    }
    
    /// <summary>
    /// 残页飞向玩家动画
    /// </summary>
    IEnumerator FlyPageToPlayer(GameObject page)
    {
        Transform playerTransform = GameObject.FindWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Destroy(page);
            yield break;
        }
        
        Vector3 startPos = page.transform.position;
        Vector3 endPos = playerTransform.position + Vector3.up * 1.5f;
        
        float duration = 2f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // 弧线运动
            Vector3 currentPosition = Vector3.Lerp(startPos, endPos, t);
            currentPosition.y += Mathf.Sin(t * Mathf.PI) * 2f; // 弧形高度
            
            page.transform.position = currentPosition;
            page.transform.Rotate(Vector3.up, 180f * Time.deltaTime); // 旋转效果
            
            yield return null;
        }
        
        // 到达玩家位置后销毁
        Destroy(page);
    }
    
    /// <summary>
    /// 完成关卡
    /// </summary>
    IEnumerator CompleteLevel()
    {
        yield return new WaitForSeconds(4f);
        
        ShowSystemPrompt(new string[] {
            "见山楼已完成。",
            "《长物志》残卷已齐。可前往雪香云蔚亭。"
        }, 4f);
        
        isLevelComplete = true;
        
        // TODO: 在这里添加关卡完成的逻辑
        // 例如:解锁下一个区域、保存进度等
    }
    
    /// <summary>
    /// 设置Panel背景颜色(防止泛白)
    /// </summary>
    void SetPanelBackground(GameObject panel, Color bgColor)
    {
        if (panel == null)
            return;
        
        UnityEngine.UI.Image image = panel.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            image.color = bgColor;
        }
        else
        {
            // 如果没有Image组件,尝试添加一个
            image = panel.AddComponent<UnityEngine.UI.Image>();
            image.color = bgColor;
        }
    }
}
