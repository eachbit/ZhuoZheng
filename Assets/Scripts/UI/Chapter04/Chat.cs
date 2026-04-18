using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZhuozhengYuan;

public class Chat : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel; // 对话框面板
    public TMP_Text speakerNameText; // 说话者名字
    public TMP_Text dialogueText; // 对话内容
    public Button nextButton; // 下一步按钮
    public GameObject choicesPanel; // 选项面板
    public Button choiceButtonA; // 选项A
    public Button choiceButtonB; // 选项B
    
    [Header("System UI")]
    public GameObject systemPromptPanel; // 系统提示面板
    public TMP_Text systemPromptText; // 系统提示文本
    public GameObject itemGetPanel; // 物品获得面板
    public TMP_Text itemGetText; // 物品获得文本
    
    [Header("Settings")]
    public float textSpeed = 0.05f; // 打字速度
    public Collider triggerCollider; // 触发碰撞体
    public TMP_FontAsset chineseFontAsset; // 中文字体Asset（可选）
    
    // 状态变量
    private int currentStage = -1;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private string currentFullText = "";
    private Coroutine typingCoroutine; // 打字协程
    
    // 对话数据结构
    private DialogueStage[] stages;
    
    void Start()
    {
        Debug.Log("🎬 Chat.cs 启动...");
        
        InitializeDialogueData();
        SetupUI();
        HideAllPanels();
        
        // 自动设置 UI 位置
        SetupUIPositions();
        
        // 应用中文字体到所有文本组件
        ApplyChineseFontToAllText();
        
        // 检查Collider配置
        CheckColliderSetup();
    }
    
    // 检查Collider配置
    void CheckColliderSetup()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("📋 检查Collider配置:");
        Debug.Log($"  - 当前物体: {gameObject.name}");
        
        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null)
        {
            Debug.Log($"  ✅ 当前物体有Collider: {myCollider.GetType().Name}");
            Debug.Log($"  ✅ Is Trigger: {myCollider.isTrigger}");
        }
        else
        {
            Debug.LogWarning("  ❌ 当前物体没有Collider！");
            Debug.LogWarning("  💡 请将脚本挂载到书生物体上，或挂载到带Collider的触发器物体上");
        }
        
        // 检查引用赋值
        Debug.Log("📋 检查UI引用:");
        Debug.Log($"  - dialoguePanel: {(dialoguePanel != null ? "✅ 已赋值" : "❌ 未赋值")}");
        Debug.Log($"  - speakerNameText: {(speakerNameText != null ? "✅ 已赋值" : "❌ 未赋值")}");
        Debug.Log($"  - dialogueText: {(dialogueText != null ? "✅ 已赋值" : "❌ 未赋值")}");
        Debug.Log($"  - nextButton: {(nextButton != null ? "✅ 已赋值" : "❌ 未赋值")}");
        Debug.Log($"  - choicesPanel: {(choicesPanel != null ? "✅ 已赋值" : "❌ 未赋值")}");
        Debug.Log($"  - choiceButtonA: {(choiceButtonA != null ? "✅ 已赋值" : "❌ 未赋值")}");
        Debug.Log($"  - choiceButtonB: {(choiceButtonB != null ? "✅ 已赋值" : "❌ 未赋值")}");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    }
    
    // 应用中文字体到所有TMP_Text组件
    void ApplyChineseFontToAllText()
    {
        if (chineseFontAsset == null)
        {
            Debug.LogWarning("⚠️ 未设置中文字体Asset！请在Inspector中拖入生成的SDF字体文件。");
            Debug.LogWarning("📝 如何创建：右键字体文件 → Create → TextMeshPro → Font Asset → 选择Chinese Simplified");
            return;
        }
        
        int appliedCount = 0;
        
        // 应用到对话文本
        if (speakerNameText != null)
        {
            speakerNameText.font = chineseFontAsset;
            appliedCount++;
            Debug.Log($"  ✅ 已设置 SpeakerNameText");
        }
        
        if (dialogueText != null)
        {
            dialogueText.font = chineseFontAsset;
            appliedCount++;
            Debug.Log($"  ✅ 已设置 DialogueText");
        }
        
        // 应用到系统提示文本
        if (systemPromptText != null)
        {
            systemPromptText.font = chineseFontAsset;
            appliedCount++;
            Debug.Log($"  ✅ 已设置 SystemPromptText");
        }
        
        if (itemGetText != null)
        {
            itemGetText.font = chineseFontAsset;
            appliedCount++;
            Debug.Log($"  ✅ 已设置 ItemGetText");
        }
        
        // 应用到选项按钮文本
        if (choiceButtonA != null)
        {
            TMP_Text textA = choiceButtonA.GetComponentInChildren<TMP_Text>();
            if (textA != null)
            {
                textA.font = chineseFontAsset;
                appliedCount++;
                Debug.Log($"  ✅ 已设置 ChoiceButtonA.Text");
            }
        }
        
        if (choiceButtonB != null)
        {
            TMP_Text textB = choiceButtonB.GetComponentInChildren<TMP_Text>();
            if (textB != null)
            {
                textB.font = chineseFontAsset;
                appliedCount++;
                Debug.Log($"  ✅ 已设置 ChoiceButtonB.Text");
            }
        }
        
        Debug.Log($"✅ 成功！已将 {appliedCount} 个文本组件设置为中文字体：{chineseFontAsset.name}");
        Debug.Log("💡 如果中文仍显示为方块，请检查：");
        Debug.Log("   1. Font Asset的Atlas Population Mode是否为Dynamic");
        Debug.Log("   2. 是否选择了正确的Character Set（Chinese Simplified）");
    }
    
    // 自动设置 UI 面板位置
    void SetupUIPositions()
    {
        // 设置对话框面板 - 底部居中
        if (dialoguePanel != null)
        {
            RectTransform rect = dialoguePanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.12f, 0.06f);
                rect.anchorMax = new Vector2(0.88f, 0.31f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }
            
            Chapter04PlaqueFrame.ApplyPanel(dialoguePanel);

            // 设置对话框内部组件位置
            SetupDialoguePanelLayout(dialoguePanel);
        }
        
        // 设置选项面板 - 屏幕中央偏上
        if (choicesPanel != null)
        {
            RectTransform rect = choicesPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.25f, 0.35f);
                rect.anchorMax = new Vector2(0.75f, 0.65f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }
            
            Chapter04PlaqueFrame.ApplyPanel(choicesPanel);

            // 设置选项面板内部按钮布局
            SetupChoicesPanelLayout(choicesPanel);
        }
        
        // 设置系统提示面板 - 顶部居中
        if (systemPromptPanel != null)
        {
            RectTransform rect = systemPromptPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.18f, 0.68f);
                rect.anchorMax = new Vector2(0.82f, 0.88f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }

            Chapter04PlaqueFrame.ApplySoftPanel(systemPromptPanel);

            if (systemPromptText != null)
            {
                RectTransform textRect = systemPromptText.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchorMin = new Vector2(0.06f, 0.18f);
                    textRect.anchorMax = new Vector2(0.94f, 0.82f);
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                }

                systemPromptText.fontSize = 20;
                systemPromptText.enableAutoSizing = true;
                systemPromptText.fontSizeMin = 12;
                systemPromptText.fontSizeMax = 20;
                systemPromptText.alignment = TextAlignmentOptions.Center;
                systemPromptText.enableWordWrapping = true;
                systemPromptText.overflowMode = TextOverflowModes.Truncate;
                systemPromptText.lineSpacing = 0.82f;
            }
        }
        
        // 设置物品获得面板 - 屏幕中央
        if (itemGetPanel != null)
        {
            RectTransform rect = itemGetPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.24f, 0.38f);
                rect.anchorMax = new Vector2(0.76f, 0.62f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }

            Chapter04PlaqueFrame.ApplyPanel(itemGetPanel);

            if (itemGetText != null)
            {
                RectTransform textRect = itemGetText.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchorMin = new Vector2(0.08f, 0.18f);
                    textRect.anchorMax = new Vector2(0.92f, 0.82f);
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                }

                itemGetText.fontSize = 24;
                itemGetText.enableAutoSizing = true;
                itemGetText.fontSizeMin = 16;
                itemGetText.fontSizeMax = 24;
                itemGetText.alignment = TextAlignmentOptions.Center;
                itemGetText.enableWordWrapping = true;
                itemGetText.overflowMode = TextOverflowModes.Truncate;
                itemGetText.lineSpacing = 0.85f;
            }
        }
    }
    
    // 设置对话框面板内部布局
    void SetupDialoguePanelLayout(GameObject panel)
    {
        if (panel == null) return;
        
        Transform panelTransform = panel.transform;
        
        // 设置说话者名字文本 - 顶部
        if (speakerNameText != null)
        {
            RectTransform rect = speakerNameText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.06f, 0.72f);
                rect.anchorMax = new Vector2(0.94f, 0.92f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                
                // 设置文字样式
                speakerNameText.fontSize = 24;
                speakerNameText.alignment = TextAlignmentOptions.Left;
                speakerNameText.enableWordWrapping = true;
                speakerNameText.overflowMode = TextOverflowModes.Truncate;
            }
        }
        
        // 设置对话内容文本 - 中间区域
        if (dialogueText != null)
        {
            RectTransform rect = dialogueText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.06f, 0.26f);
                rect.anchorMax = new Vector2(0.94f, 0.66f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                
                // 设置文字样式
                dialogueText.fontSize = 20;
                dialogueText.alignment = TextAlignmentOptions.TopLeft;
                dialogueText.enableWordWrapping = true;
                dialogueText.overflowMode = TextOverflowModes.Truncate;
                dialogueText.lineSpacing = 1.2f;
            }
        }
        
        // 设置下一步按钮 - 底部右侧
        if (nextButton != null)
        {
            RectTransform rect = nextButton.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.72f, 0.08f);
                rect.anchorMax = new Vector2(0.90f, 0.24f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                
                // 设置按钮文字 - 使用递归查找确保找到TMP_Text
                TMP_Text buttonText = nextButton.GetComponentInChildren<TMP_Text>(true);
                if (buttonText != null)
                {
                    RectTransform textRect = buttonText.GetComponent<RectTransform>();
                    if (textRect != null)
                    {
                        textRect.anchorMin = new Vector2(0.08f, 0.12f);
                        textRect.anchorMax = new Vector2(0.92f, 0.88f);
                        textRect.offsetMin = Vector2.zero;
                        textRect.offsetMax = Vector2.zero;
                    }

                    buttonText.text = "下一步";
                    buttonText.fontSize = 22;
                    buttonText.alignment = TextAlignmentOptions.Center;
                    buttonText.color = new Color(0.93f, 0.82f, 0.52f, 1f);
                    buttonText.fontStyle = FontStyles.Bold;
                    buttonText.enableWordWrapping = true;
                    Debug.Log("✅ 已设置下一步按钮文字");
                }
                else
                {
                    Debug.LogWarning("⚠️ 未找到NextButton的子TMP_Text组件！请检查按钮结构");
                }
            }

            Chapter04PlaqueFrame.ApplyButton(nextButton.gameObject);
        }
    }
    
    // 设置选项面板内部布局
    void SetupChoicesPanelLayout(GameObject panel)
    {
        if (panel == null) return;
        
        // 设置选项按钮A - 上方
        if (choiceButtonA != null)
        {
            RectTransform rect = choiceButtonA.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.1f, 0.55f);
                rect.anchorMax = new Vector2(0.9f, 0.9f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                
                // 设置按钮文字 - 使用递归查找
                TMP_Text buttonText = choiceButtonA.GetComponentInChildren<TMP_Text>(true);
                if (buttonText != null)
                {
                    buttonText.text = "兄台似乎对园林颇有研究？";
                    buttonText.fontSize = 18;
                    buttonText.alignment = TextAlignmentOptions.Center;
                    buttonText.enableWordWrapping = true;
                    Debug.Log("✅ 已设置选项A按钮文字");
                }
                else
                {
                    Debug.LogWarning("⚠️ 未找到ChoiceButtonA的子TMP_Text组件！");
                }
            }

            Chapter04PlaqueFrame.ApplyButton(choiceButtonA.gameObject);
        }
        
        // 设置选项按钮B - 下方
        if (choiceButtonB != null)
        {
            RectTransform rect = choiceButtonB.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.1f, 0.1f);
                rect.anchorMax = new Vector2(0.9f, 0.45f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                
                // 设置按钮文字 - 使用递归查找
                TMP_Text buttonText = choiceButtonB.GetComponentInChildren<TMP_Text>(true);
                if (buttonText != null)
                {
                    buttonText.text = "这亭子为何是扇形的？";
                    buttonText.fontSize = 18;
                    buttonText.alignment = TextAlignmentOptions.Center;
                    buttonText.enableWordWrapping = true;
                    Debug.Log("✅ 已设置选项B按钮文字");
                }
                else
                {
                    Debug.LogWarning("⚠️ 未找到ChoiceButtonB的子TMP_Text组件！");
                }
            }

            Chapter04PlaqueFrame.ApplyButton(choiceButtonB.gameObject);
        }
    }
    
    void Update()
    {
        // 按E键告别
        if (Input.GetKeyDown(KeyCode.E) && currentStage == 4)
        {
            ShowFarewellDialogue();
        }
    }
    
    // 初始化对话数据
    void InitializeDialogueData()
    {
        stages = new DialogueStage[6];
        
        // 第一阶段：初遇
        stages[0] = new DialogueStage
        {
            lines = new DialogueLine[]
            {
                new DialogueLine("听雨书生", "兄台也来避雨？请入。此轩虽小，足以容身。"),
                new DialogueLine("", "（玩家进入轩内）"),
                new DialogueLine("听雨书生", "兄台可知此轩之名？"),
                new DialogueLine("听雨书生", "与谁同坐轩。"),
                new DialogueLine("听雨书生", "在下初闻此名时，曾想——与谁同坐？是知己？是家人？还是……"),
                new DialogueLine("听雨书生", "后来读苏轼词，方知答案。"),
                new DialogueLine("听雨书生", "'与谁同坐？明月清风我。'"),
                new DialogueLine("听雨书生", "原来，最好的同坐者，不是人，是天地。")
            },
            nextStage = 1,
            hasChoices = false
        };
        
        // 第二阶段：雨景观照
        stages[1] = new DialogueStage
        {
            lines = new DialogueLine[]
            {
                new DialogueLine("", "（镜头转向轩外雨景：雨打水面、涟漪层层、荷叶承雨微颤）"),
                new DialogueLine("听雨书生", "你听。"),
                new DialogueLine("听雨书生", "这雨声。"),
                new DialogueLine("", "（停顿，雨声凸显）"),
                new DialogueLine("听雨书生", "《长物志》有言：'雨之为物，能令昼短，能令夜长。'"),
                new DialogueLine("听雨书生", "有人厌雨，嫌它碍事。可在园子里，雨是最好的画师。"),
                new DialogueLine("听雨书生", "无雨，涟漪不动；无雨，荷色徒绿。")
            },
            nextStage = 2,
            hasChoices = true
        };
        
        // 第三阶段：选项A
        stages[2] = new DialogueStage
        {
            lines = new DialogueLine[]
            {
                new DialogueLine("玩家", "兄台似乎对园林颇有研究？"),
                new DialogueLine("听雨书生", "不敢。只是常来走走。"),
                new DialogueLine("听雨书生", "园子这东西，不是一次看完的。春来看花，夏来听荷，秋来赏叶，冬来踏雪。"),
                new DialogueLine("听雨书生", "雨天，也要来看。"),
                new DialogueLine("听雨书生", "文震亨当年写《长物志》，说的就是这个理——园，是要'住'进去的。")
            },
            nextStage = 4,
            hasChoices = false
        };
        
        // 第四阶段：选项B
        stages[3] = new DialogueStage
        {
            lines = new DialogueLine[]
            {
                new DialogueLine("玩家", "这亭子为何是扇形的？"),
                new DialogueLine("听雨书生", "好眼力。此亭形如折扇，故又称'扇亭'。"),
                new DialogueLine("听雨书生", "清末有位张先生，祖上以制扇起家，便建了这座扇亭以念先祖。"),
                new DialogueLine("听雨书生", "你看那扇窗——"),
                new DialogueLine("", "（指向扇形空窗）"),
                new DialogueLine("听雨书生", "框住的是景，藏住的是心。")
            },
            nextStage = 4,
            hasChoices = false
        };
        
        // 第五阶段：雨停与获得物品
        stages[4] = new DialogueStage
        {
            lines = new DialogueLine[]
            {
                new DialogueLine("", "（雨声渐微，雨丝渐疏）"),
                new DialogueLine("", "（云隙透光，水面现虹）"),
                new DialogueLine("SYSTEM", "🌈 雨霁。"),
                new DialogueLine("听雨书生", "雨停了。"),
                new DialogueLine("", "（他转头看向玩家）"),
                new DialogueLine("听雨书生", "兄台若有事在身，不必陪在下久留。"),
                new DialogueLine("", "（他从袖中取出一卷竹简）"),
                new DialogueLine("听雨书生", "这卷残页，是在下前日于此亭中所拾。"),
                new DialogueLine("听雨书生", "读来应是《长物志》散佚之篇。"),
                new DialogueLine("听雨书生", "在下四处游历，不便携带。兄台既来修园，不如赠你。"),
                new DialogueLine("听雨书生", "物归其所，方不负古人。")
            },
            nextStage = 5,
            hasChoices = false,
            giveItem = true
        };
        
        // 第六阶段：告别或离开
        stages[5] = new DialogueStage
        {
            lines = new DialogueLine[]
            {
                new DialogueLine("系统", "玩家可以按E与书生告别，或直接走出轩")
            },
            nextStage = -1,
            hasChoices = false,
            isFinalStage = true
        };
    }
    
    // 设置UI
    void SetupUI()
    {
        // 确保下一步按钮有中文文本
        if (nextButton != null)
        {
            TMP_Text buttonText = nextButton.GetComponentInChildren<TMP_Text>(true);
            if (buttonText != null)
            {
                buttonText.text = "下一步";
                Debug.Log("✅ SetupUI: 已设置下一步按钮文字");
            }
            else
            {
                Debug.LogError("❌ SetupUI: NextButton没有TMP_Text子组件！请在按钮下添加TextMeshPro组件");
            }
            
            nextButton.onClick.AddListener(OnNextButtonClick);
        }
        
        if (choiceButtonA != null)
            choiceButtonA.onClick.AddListener(() => OnChoiceClick(0));
        
        if (choiceButtonB != null)
            choiceButtonB.onClick.AddListener(() => OnChoiceClick(1));
    }
    
    // 隐藏所有面板
    void HideAllPanels()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choicesPanel != null) choicesPanel.SetActive(false);
        if (systemPromptPanel != null) systemPromptPanel.SetActive(false);
        if (itemGetPanel != null) itemGetPanel.SetActive(false);
    }
    
    // 碰撞触发
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"🎯 OnTriggerEnter 触发！碰撞物体: {other.name}, Tag: {other.tag}");
        
        if (other.CompareTag("Player"))
        {
            Debug.Log("✅ 检测到Player，开始对话！");
            StartDialogue();
        }
        else
        {
            Debug.LogWarning($"⚠️ 碰撞物体不是Player (Tag: {other.tag})");
        }
    }
    
    // 开始对话
    void StartDialogue()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("💬 开始对话...");
        Debug.Log($"  - currentStage: {currentStage} → 0");
        Debug.Log($"  - currentLineIndex: {currentLineIndex} → 0");
        
        currentStage = 0;
        currentLineIndex = 0;
        ShowDialoguePanel();
        
        Debug.Log($"  - dialoguePanel: {(dialoguePanel != null && dialoguePanel.activeSelf ? "✅ 已激活" : "❌ 未激活")}");
        
        ShowCurrentLine();
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    }
    
    // 显示对话框
    void ShowDialoguePanel()
    {
        Debug.Log("💬 ShowDialoguePanel: 显示对话框");
        
        if (dialoguePanel != null) 
        {
            dialoguePanel.SetActive(true);
            Debug.Log("  ✅ 对话框已激活");
        }
        
        // 确保显示下一步按钮
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            
            // 确保按钮文本正确
            TMP_Text buttonText = nextButton.GetComponentInChildren<TMP_Text>(true);
            if (buttonText != null && buttonText.text != "下一步")
            {
                buttonText.text = "下一步";
                Debug.Log("  ✅ 已更新按钮文本为'下一步'");
            }
        }
        
        // 解锁光标并显示鼠标，让玩家能点击UI
        Debug.Log("  🖱️ 解锁光标，显示鼠标");
        LockCursor(false);
    }
    
    // 锁定/解锁光标
    void LockCursor(bool shouldLock)
    {
        if (shouldLock)
        {
            // 游戏模式：锁定并隐藏光标
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            // 对话模式：解锁并显示光标
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    // 显示当前行
    void ShowCurrentLine()
    {
        // 严格边界检查：防止索引越界
        if (currentStage < 0 || currentStage >= stages.Length)
        {
            Debug.LogWarning($"️ currentStage 越界: {currentStage}，有效范围 0-{stages.Length - 1}");
            EndDialogue();
            return;
        }
        
        DialogueStage stage = stages[currentStage];
        
        // 检查当前行索引是否有效
        if (currentLineIndex < 0 || currentLineIndex >= stage.lines.Length)
        {
            Debug.LogWarning($"⚠️ currentLineIndex 越界: {currentLineIndex}，有效范围 0-{stage.lines.Length - 1}");
            
            // 当前阶段结束，处理阶段转换
            if (stage.hasChoices)
            {
                ShowChoices();
            }
            else if (stage.giveItem)
            {
                GiveItemAndContinue();
            }
            else if (stage.isFinalStage)
            {
                ShowFinalStage();
            }
            else
            {
                // 进入下一阶段前，检查 nextStage 是否有效
                if (stage.nextStage < 0 || stage.nextStage >= stages.Length)
                {
                    Debug.LogWarning($"️ nextStage 无效: {stage.nextStage}，结束对话");
                    EndDialogue();
                    return;
                }
                
                currentStage = stage.nextStage;
                currentLineIndex = 0;
                ShowCurrentLine();
            }
            return;
        }
        
        DialogueLine line = stage.lines[currentLineIndex];
        
        // 如果是系统提示
        if (line.speaker == "SYSTEM")
        {
            ShowSystemPrompt(line.text);
            currentLineIndex++;
            
            // 递增后再次检查边界
            if (currentLineIndex >= stage.lines.Length)
            {
                Debug.Log(" 系统提示后阶段结束");
                // 递归调用处理阶段结束逻辑
                ShowCurrentLine();
            }
            return;
        }
        
        // 显示说话者名字
        // 确保名字显示功能正常工作
        if (speakerNameText != null)
        {
            if (string.IsNullOrEmpty(line.speaker))
            {
                // 说话者为空，隐藏名字文本
                if (speakerNameText.gameObject.activeSelf)
                {
                    speakerNameText.gameObject.SetActive(false);
                    Debug.Log($"  📝 隐藏说话者名字（空）");
                }
            }
            else
            {
                // 说话者不为空，显示并设置名字
                if (!speakerNameText.gameObject.activeSelf)
                {
                    speakerNameText.gameObject.SetActive(true);
                    Debug.Log($"  ✅ 激活说话者名字组件");
                }
                speakerNameText.text = line.speaker;
                Debug.Log($"  📝 显示说话者: {line.speaker}");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ speakerNameText 为 null！请在Inspector中赋值");
        }
        
        currentFullText = line.text;
        
        // 打字机效果
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        
        typingCoroutine = StartCoroutine(TypeText(currentFullText));
    }
    
    // 打字机效果
    IEnumerator TypeText(string fullText)
    {
        isTyping = true;
        dialogueText.text = "";
        
        foreach (char c in fullText)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
        
        isTyping = false;
    }
    
    // 下一步按钮点击
    void OnNextButtonClick()
    {
        if (isTyping)
        {
            // 如果正在打字，立即显示全部文本
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            
            dialogueText.text = currentFullText;
            isTyping = false;
        }
        else
        {
            // 显示下一行
            currentLineIndex++;
            ShowCurrentLine();
        }
        
        // 保持鼠标可见（对话模式下不应该隐藏鼠标）
        // 不移除LockCursor调用，因为我们已经在ShowDialoguePanel中解锁了
    }
    
    // 显示选项
    void ShowChoices()
    {
        if (choicesPanel != null)
        {
            choicesPanel.SetActive(true);
            
            // 设置选项文本
            TMP_Text textA = choiceButtonA.GetComponentInChildren<TMP_Text>();
            TMP_Text textB = choiceButtonB.GetComponentInChildren<TMP_Text>();
            
            if (textA != null) textA.text = "兄台似乎对园林颇有研究？";
            if (textB != null) textB.text = "这亭子为何是扇形的？";
        }
    }
    
    // 选项点击
    void OnChoiceClick(int choiceIndex)
    {
        if (choicesPanel != null)
            choicesPanel.SetActive(false);
        
        // 根据选择进入不同分支
        if (choiceIndex == 0)
        {
            // 选项A
            currentStage = 2;
        }
        else
        {
            // 选项B
            currentStage = 3;
        }
        
        currentLineIndex = 0;
        ShowCurrentLine();
    }
    
    // 给予物品并继续
    void GiveItemAndContinue()
    {
        // 显示物品获得面板
        if (itemGetPanel != null && itemGetText != null)
        {
            itemGetText.text = "获得《长物志》残页 ×1\n（收集进度：4/5）";
            AwardChapter04PageToManager();
            itemGetPanel.SetActive(true);
            
            StartCoroutine(HideItemGetPanelAfterDelay());
        }
    }

    void AwardChapter04PageToManager()
    {
        GardenGameManager manager = GardenGameManager.Instance != null
            ? GardenGameManager.Instance
            : FindObjectOfType<GardenGameManager>();

        if (manager == null || manager.CurrentSaveData == null)
        {
            return;
        }

        if (TryAwardChapter04Page(manager.CurrentSaveData, manager.totalPages))
        {
            manager.RefreshCollectedPagesDisplay();
            manager.SaveProgress();
        }
    }

    static bool TryAwardChapter04Page(SaveData saveData, int totalPages)
    {
        if (saveData == null || saveData.chapter04PageCollected)
        {
            return false;
        }

        saveData.chapter04PageCollected = true;
        saveData.collectedPages = Mathf.Clamp(Mathf.Max(saveData.collectedPages, 4), 0, Mathf.Max(4, totalPages));
        return true;
    }
    
    // 延迟隐藏物品获得面板
    IEnumerator HideItemGetPanelAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        
        if (itemGetPanel != null)
            itemGetPanel.SetActive(false);
        
        // 继续下一阶段
        currentStage = stages[currentStage].nextStage;
        currentLineIndex = 0;
        ShowCurrentLine();
        
        // 显示系统提示
        StartCoroutine(ShowExplorationPrompt());
    }
    
    // 显示探索提示
    IEnumerator ShowExplorationPrompt()
    {
        yield return new WaitForSeconds(2f);
        
        if (systemPromptPanel != null)
        {
            systemPromptText.text = "雨后的园林，空气清润。前方可见山楼。\n（玩家可以继续前往下一关：见山楼）";
            systemPromptPanel.SetActive(true);
            
            yield return new WaitForSeconds(4f);
            
            systemPromptPanel.SetActive(false);
        }
    }
    
    // 显示最终阶段
    void ShowFinalStage()
    {
        // 最终阶段，显示提示信息并保持下一步按钮
        currentLineIndex = 0;
        ShowCurrentLine();
        
        // 显示下一步按钮（让玩家点击后结束对话）
        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
        
        Debug.Log("📢 对话完成！点击下一步结束，或按E键告别，或直接走出轩离开。");
    }
    
    // 结束对话并隐藏所有UI
    void EndDialogue()
    {
        HideAllPanels();
        currentStage = -1;
        
        // 恢复光标锁定（回到游戏模式）
        LockCursor(true);
        
        Debug.Log("✅ 对话结束，已恢复游戏控制。");
    }
    
    // 显示系统提示
    void ShowSystemPrompt(string text)
    {
        if (systemPromptPanel != null)
        {
            systemPromptText.text = text;
            systemPromptPanel.SetActive(true);
            
            StartCoroutine(HideSystemPromptAfterDelay());
        }
    }
    
    // 延迟隐藏系统提示
    IEnumerator HideSystemPromptAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        
        if (systemPromptPanel != null)
            systemPromptPanel.SetActive(false);
        
        // 继续下一行
        currentLineIndex++;
        ShowCurrentLine();
    }
    
    // 显示告别对话
    void ShowFarewellDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        
        // 恢复下一步按钮显示
        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
        
        speakerNameText.text = "玩家";
        currentFullText = "多谢兄台。后会有期。";
        
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        
        typingCoroutine = StartCoroutine(TypeTextAndContinue(currentFullText, () =>
        {
            // 书生回应
            StartCoroutine(ShowScholarResponse());
        }));
    }
    
    // 显示书生回应
    IEnumerator ShowScholarResponse()
    {
        yield return new WaitForSeconds(1f);
        
        speakerNameText.text = "听雨书生";
        currentFullText = "不必谢。这园子，本就该有人来修，有人来守。";
        
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        
        yield return StartCoroutine(TypeText(currentFullText));
        
        yield return new WaitForSeconds(1f);
        
        currentFullText = "兄台去忙吧。在下……";
        yield return StartCoroutine(TypeText(currentFullText));
        
        yield return new WaitForSeconds(1f);
        
        currentFullText = "再坐一会儿。";
        yield return StartCoroutine(TypeText(currentFullText));
        
        yield return new WaitForSeconds(2f);
        
        EndDialogue();
    }
    
    // 打字并继续
    IEnumerator TypeTextAndContinue(string text, System.Action onComplete)
    {
        isTyping = true;
        dialogueText.text = "";
        
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
        
        isTyping = false;
        onComplete?.Invoke();
    }
    
    // 玩家离开时的处理
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && currentStage == 5)
        {
            // 如果玩家直接离开
            ShowDepartureMessage();
        }
    }
    
    // 显示离开消息
    void ShowDepartureMessage()
    {
        if (systemPromptPanel != null)
        {
            systemPromptText.text = "书生没有离开。他留在了轩中，与清风、明月为伴。";
            systemPromptPanel.SetActive(true);
            
            StartCoroutine(HideSystemPromptAfterDelay());
        }
    }
}

// 对话行数据结构
[System.Serializable]
public class DialogueLine
{
    public string speaker; // 说话者
    public string text; // 对话内容
    
    public DialogueLine(string speaker, string text)
    {
        this.speaker = speaker;
        this.text = text;
    }
}

// 对话阶段数据结构
[System.Serializable]
public class DialogueStage
{
    public DialogueLine[] lines; // 对话行数组
    public int nextStage; // 下一阶段索引
    public bool hasChoices; // 是否有选项
    public bool giveItem; // 是否给予物品
    public bool isFinalStage; // 是否是最终阶段
}
