# Chapter01 Formal UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `Garden_Main` 原场景中，把第一章临时 `OnGUI` 界面替换为可继续迭代的正式 Canvas UI，并补齐暗闸校准面板。

**Architecture:** 新增一个第一章专用的 Canvas 表现层，集中承接第一章 HUD、对话、提示、水流选择和暗闸校准显示；`GardenGameManager` 继续做高层调度，`Chapter01Director` 继续做玩法判断。保留 `PrototypeRuntimeUI` 作为兼容 / 第二章答题展示层，避免一次性牵动过多已有逻辑。

**Tech Stack:** Unity 2022.3.62f3c1、UGUI、TextMeshPro、C#、Unity EditMode Tests

---

## 文件结构

### 新增文件

- `D:\Unity\New\Assets\Scripts\UI\Chapter01\IChapter01RuntimeUIPresenter.cs`
  - 第一章 UI 展示接口，隔离玩法层与具体 Canvas 实现。
- `D:\Unity\New\Assets\Scripts\UI\Chapter01\Chapter01CanvasUI.cs`
  - 第一章正式 Canvas UI 主脚本，管理所有第一章 UI 面板。
- `D:\Unity\New\Assets\Scripts\UI\Chapter01\Chapter01GateCalibrationViewData.cs`
  - 暗闸校准面板展示数据结构，减少方法参数混乱。
- `D:\Unity\New\Assets\Prefabs\UI\Chapter01\Chapter01UIRoot.prefab`
  - 第一章正式 UI 预制体。
- `D:\Unity\New\Assets\Tests\EditMode\UI\Chapter01CanvasUITests.cs`
  - 第一章 Canvas UI 的 EditMode 测试。
- `D:\Unity\New\Assets\Tests\EditMode\UI\Chapter01UI.EditMode.asmdef`
  - EditMode 测试程序集定义。

### 修改文件

- `D:\Unity\New\Assets\Scripts\Core\GardenGameManager.cs`
  - 持有和分发第一章 UI 展示接口。
- `D:\Unity\New\Assets\Scripts\UI\PrototypeRuntimeUI.cs`
  - 改成第一章 fallback / 第二章 presenter，避免继续承担正式第一章 UI。
- `D:\Unity\New\Assets\Scripts\Chapters\Chapter01\Chapter01Director.cs`
  - 在暗闸开始、更新、结束时向 UI 发送校准数据。
- `D:\Unity\New\Assets\Scripts\Chapters\Chapter01\GateInteractable.cs`
  - 暴露用于 UI 展示的校准状态信息。
- `D:\Unity\New\Assets\Scenes\Garden_Main.unity`
  - 挂载第一章正式 UI 预制体并连接 `GardenGameManager`。
- `D:\Unity\New\Packages\manifest.json`
  - 增加 Unity Test Framework 依赖。

---

### Task 1：补齐测试框架并建立第一章 UI 展示接口

**Files:**
- Create: `D:\Unity\New\Assets\Scripts\UI\Chapter01\IChapter01RuntimeUIPresenter.cs`
- Create: `D:\Unity\New\Assets\Scripts\UI\Chapter01\Chapter01GateCalibrationViewData.cs`
- Modify: `D:\Unity\New\Packages\manifest.json`
- Create: `D:\Unity\New\Assets\Tests\EditMode\UI\Chapter01UI.EditMode.asmdef`
- Create: `D:\Unity\New\Assets\Tests\EditMode\UI\Chapter01CanvasUITests.cs`

- [ ] **Step 1: 为项目加入 Unity Test Framework**

在 `Packages/manifest.json` 的 `dependencies` 中加入：

```json
"com.unity.test-framework": "1.1.33"
```

预期结果：
- Unity 重新导入后出现 Test Runner 能力
- 可以建立 EditMode 测试

- [ ] **Step 2: 定义第一章正式 UI 的统一展示接口**

创建 `D:\Unity\New\Assets\Scripts\UI\Chapter01\IChapter01RuntimeUIPresenter.cs`：

```csharp
using System;
using UnityEngine;

namespace ZhuozhengYuan
{
    public interface IChapter01RuntimeUIPresenter
    {
        bool IsDialogueOpen { get; }
        void SetPageCount(int currentPages, int maxPages);
        void SetInteractionPrompt(string prompt);
        void SetObjective(string objective);
        void ShowToast(string message, float duration = 2.2f);
        void ShowDirectionResult(string title, string message, Color accentColor, float duration = 2.6f);
        void ShowDialogue(DialogueLine[] dialogueLines, Action onCompleted);
        void ShowDirectionChoice(string[] options, Action<string> onSelected);
        void ShowGateCalibration(Chapter01GateCalibrationViewData data);
        void HideGateCalibration();
        void SetFadeAlpha(float alpha);
    }
}
```

- [ ] **Step 3: 创建暗闸校准展示数据结构**

创建 `D:\Unity\New\Assets\Scripts\UI\Chapter01\Chapter01GateCalibrationViewData.cs`：

```csharp
using UnityEngine;

namespace ZhuozhengYuan
{
    [System.Serializable]
    public struct Chapter01GateCalibrationViewData
    {
        public string gateName;
        public float currentAngle;
        public float targetAngle;
        public float validAngleTolerance;
        public bool canConfirm;
        public KeyCode negativeKey;
        public KeyCode positiveKey;
        public KeyCode confirmKey;
        public KeyCode cancelKey;
    }
}
```

- [ ] **Step 4: 建立 EditMode 测试程序集**

创建 `D:\Unity\New\Assets\Tests\EditMode\UI\Chapter01UI.EditMode.asmdef`：

```json
{
  "name": "Chapter01UI.EditMode",
  "references": [
    "Unity.TextMeshPro",
    "UnityEngine.UI"
  ],
  "optionalUnityReferences": [
    "TestAssemblies"
  ],
  "includePlatforms": [
    "Editor"
  ]
}
```

- [ ] **Step 5: 先写失败测试，锁定第一章 UI 的核心展示行为**

创建 `D:\Unity\New\Assets\Tests\EditMode\UI\Chapter01CanvasUITests.cs`：

```csharp
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZhuozhengYuan.Tests.EditMode
{
    public class Chapter01CanvasUITests
    {
        [Test]
        public void SetObjective_ShouldWriteObjectiveText()
        {
            var root = new GameObject("UI");
            var ui = root.AddComponent<Chapter01CanvasUI>();
            ui.objectivePanel = new GameObject("ObjectivePanel");
            ui.objectiveText = ui.objectivePanel.AddComponent<TextMeshProUGUI>();

            ui.SetObjective("前往廊桥尽头");

            Assert.AreEqual("前往廊桥尽头", ui.objectiveText.text);
        }

        [Test]
        public void ShowGateCalibration_ShouldDisplayPanelAndAngles()
        {
            var root = new GameObject("UI");
            var ui = root.AddComponent<Chapter01CanvasUI>();
            ui.gateCalibrationPanel = new GameObject("GatePanel");
            ui.gateCalibrationTitleText = ui.gateCalibrationPanel.AddComponent<TextMeshProUGUI>();
            ui.gateCalibrationCurrentAngleText = new GameObject("CurrentAngle").AddComponent<TextMeshProUGUI>();
            ui.gateCalibrationTargetRangeText = new GameObject("TargetRange").AddComponent<TextMeshProUGUI>();

            ui.ShowGateCalibration(new Chapter01GateCalibrationViewData
            {
                gateName = "左暗闸",
                currentAngle = 40f,
                targetAngle = 55f,
                validAngleTolerance = 9f,
                canConfirm = false,
                negativeKey = KeyCode.A,
                positiveKey = KeyCode.D,
                confirmKey = KeyCode.E,
                cancelKey = KeyCode.Escape
            });

            Assert.IsTrue(ui.gateCalibrationPanel.activeSelf);
            StringAssert.Contains("左暗闸", ui.gateCalibrationTitleText.text);
            StringAssert.Contains("40", ui.gateCalibrationCurrentAngleText.text);
            StringAssert.Contains("46", ui.gateCalibrationTargetRangeText.text);
        }
    }
}
```

- [ ] **Step 6: 运行测试，确认当前会失败**

Run:

```powershell
& 'D:\Unity\2022.3.62f3c1\Editor\Unity.exe' -batchmode -projectPath 'D:\Unity\New' -runTests -testPlatform EditMode -testResults 'D:\Unity\New\TestResults\chapter01-ui-task1.xml' -logFile 'D:\Unity\New\TestResults\chapter01-ui-task1.log' -quit
```

Expected:
- FAIL
- 报错 `Chapter01CanvasUI` 尚不存在

- [ ] **Step 7: Commit**

```powershell
git add 'Packages/manifest.json' 'Assets/Scripts/UI/Chapter01' 'Assets/Tests/EditMode/UI'
git commit -m "test: add chapter01 ui presenter contract"
```

---

### Task 2：实现第一章正式 Canvas UI 主脚本

**Files:**
- Create: `D:\Unity\New\Assets\Scripts\UI\Chapter01\Chapter01CanvasUI.cs`
- Test: `D:\Unity\New\Assets\Tests\EditMode\UI\Chapter01CanvasUITests.cs`

- [ ] **Step 1: 先写最小可通过实现，承接已有第一章 UI 调用**

创建 `D:\Unity\New\Assets\Scripts\UI\Chapter01\Chapter01CanvasUI.cs`：

```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZhuozhengYuan
{
    public class Chapter01CanvasUI : MonoBehaviour, IChapter01RuntimeUIPresenter
    {
        [Header("Persistent HUD")]
        public GameObject pageCounterPanel;
        public TextMeshProUGUI pageCounterText;
        public GameObject objectivePanel;
        public TextMeshProUGUI objectiveText;
        public GameObject toastPanel;
        public TextMeshProUGUI toastTitleText;
        public TextMeshProUGUI toastBodyText;
        public Image toastAccentImage;
        public GameObject interactionPromptPanel;
        public TextMeshProUGUI interactionPromptText;

        [Header("Dialogue")]
        public GameObject dialoguePanel;
        public TextMeshProUGUI dialogueSpeakerText;
        public TextMeshProUGUI dialogueBodyText;
        public Button dialogueContinueButton;
        public TextMeshProUGUI dialogueContinueHintText;

        [Header("Flow Choice")]
        public GameObject flowChoicePanel;
        public TextMeshProUGUI flowChoiceTitleText;
        public TextMeshProUGUI flowChoiceBodyText;
        public Button[] flowChoiceButtons;
        public TextMeshProUGUI[] flowChoiceButtonTexts;

        [Header("Gate Calibration")]
        public GameObject gateCalibrationPanel;
        public TextMeshProUGUI gateCalibrationTitleText;
        public TextMeshProUGUI gateCalibrationCurrentAngleText;
        public TextMeshProUGUI gateCalibrationTargetRangeText;
        public TextMeshProUGUI gateCalibrationHintText;
        public Button gateCalibrationConfirmButton;
        public TextMeshProUGUI gateCalibrationConfirmButtonText;

        [Header("Fade")]
        public CanvasGroup fadeCanvasGroup;

        private DialogueLine[] _activeDialogue;
        private int _dialogueIndex;
        private Action _dialogueCompletedCallback;
        private Action<string> _directionSelectedCallback;
        private readonly string[] _flowOptionCache = new string[3];

        public bool IsDialogueOpen => dialoguePanel != null && dialoguePanel.activeSelf;

        public void SetPageCount(int currentPages, int maxPages)
        {
            if (pageCounterText != null)
            {
                pageCounterText.text = $"残页 {currentPages:00}/{maxPages:00}";
            }
        }

        public void SetInteractionPrompt(string prompt)
        {
            if (interactionPromptPanel != null)
            {
                interactionPromptPanel.SetActive(!string.IsNullOrEmpty(prompt));
            }

            if (interactionPromptText != null)
            {
                interactionPromptText.text = prompt ?? string.Empty;
            }
        }

        public void SetObjective(string objective)
        {
            if (objectivePanel != null)
            {
                objectivePanel.SetActive(!string.IsNullOrEmpty(objective));
            }

            if (objectiveText != null)
            {
                objectiveText.text = objective ?? string.Empty;
            }
        }

        public void ShowToast(string message, float duration = 2.2f)
        {
            if (toastPanel != null)
            {
                toastPanel.SetActive(!string.IsNullOrEmpty(message));
            }

            if (toastBodyText != null)
            {
                toastBodyText.text = message ?? string.Empty;
            }
        }

        public void ShowDirectionResult(string title, string message, Color accentColor, float duration = 2.6f)
        {
            if (toastPanel != null)
            {
                toastPanel.SetActive(true);
            }

            if (toastTitleText != null)
            {
                toastTitleText.text = title ?? string.Empty;
            }

            if (toastBodyText != null)
            {
                toastBodyText.text = message ?? string.Empty;
            }

            if (toastAccentImage != null)
            {
                toastAccentImage.color = accentColor;
            }
        }

        public void ShowDialogue(DialogueLine[] dialogueLines, Action onCompleted) { }
        public void ShowDirectionChoice(string[] options, Action<string> onSelected) { }
        public void ShowGateCalibration(Chapter01GateCalibrationViewData data) { }
        public void HideGateCalibration() { }
        public void SetFadeAlpha(float alpha)
        {
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = Mathf.Clamp01(alpha);
            }
        }
    }
}
```

- [ ] **Step 2: 完成对话框、水流选择、暗闸面板的最小交互实现**

把下面代码补进 `Chapter01CanvasUI.cs`：

```csharp
public void ShowDialogue(DialogueLine[] dialogueLines, Action onCompleted)
{
    _activeDialogue = dialogueLines;
    _dialogueIndex = 0;
    _dialogueCompletedCallback = onCompleted;
    if (dialoguePanel != null) dialoguePanel.SetActive(dialogueLines != null && dialogueLines.Length > 0);
    RefreshDialogue();
}

public void ShowDirectionChoice(string[] options, Action<string> onSelected)
{
    _directionSelectedCallback = onSelected;
    if (flowChoicePanel != null) flowChoicePanel.SetActive(true);

    for (int i = 0; i < flowChoiceButtons.Length; i++)
    {
        bool enabled = options != null && i < options.Length;
        flowChoiceButtons[i].gameObject.SetActive(enabled);
        if (!enabled) continue;

        string optionText = options[i];
        _flowOptionCache[i] = optionText;
        flowChoiceButtonTexts[i].text = $"{i + 1}. {optionText}";
        int capturedIndex = i;
        flowChoiceButtons[i].onClick.RemoveAllListeners();
        flowChoiceButtons[i].onClick.AddListener(() => SelectDirection(capturedIndex));
    }
}

public void ShowGateCalibration(Chapter01GateCalibrationViewData data)
{
    if (gateCalibrationPanel != null) gateCalibrationPanel.SetActive(true);
    if (gateCalibrationTitleText != null) gateCalibrationTitleText.text = $"{data.gateName}校准";
    if (gateCalibrationCurrentAngleText != null) gateCalibrationCurrentAngleText.text = $"{data.currentAngle:0}°";

    float min = data.targetAngle - data.validAngleTolerance;
    float max = data.targetAngle + data.validAngleTolerance;
    if (gateCalibrationTargetRangeText != null) gateCalibrationTargetRangeText.text = $"{min:0}° - {max:0}°";
    if (gateCalibrationHintText != null) gateCalibrationHintText.text = $"A/D 旋转  E 确认  Esc 取消";
    if (gateCalibrationConfirmButton != null) gateCalibrationConfirmButton.interactable = data.canConfirm;
}

public void HideGateCalibration()
{
    if (gateCalibrationPanel != null) gateCalibrationPanel.SetActive(false);
}

private void RefreshDialogue()
{
    if (_activeDialogue == null || _activeDialogue.Length == 0)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        return;
    }

    DialogueLine current = _activeDialogue[_dialogueIndex];
    if (dialogueSpeakerText != null) dialogueSpeakerText.text = current.speaker;
    if (dialogueBodyText != null) dialogueBodyText.text = current.text;
}

private void SelectDirection(int index)
{
    if (flowChoicePanel != null) flowChoicePanel.SetActive(false);
    _directionSelectedCallback?.Invoke(_flowOptionCache[index]);
}
```

- [ ] **Step 3: 运行 EditMode 测试，确认主脚本已满足最小接口**

Run:

```powershell
& 'D:\Unity\2022.3.62f3c1\Editor\Unity.exe' -batchmode -projectPath 'D:\Unity\New' -runTests -testPlatform EditMode -testResults 'D:\Unity\New\TestResults\chapter01-ui-task2.xml' -logFile 'D:\Unity\New\TestResults\chapter01-ui-task2.log' -quit
```

Expected:
- PASS 至少包含 `SetObjective_ShouldWriteObjectiveText`
- PASS 至少包含 `ShowGateCalibration_ShouldDisplayPanelAndAngles`

- [ ] **Step 4: Commit**

```powershell
git add 'Assets/Scripts/UI/Chapter01' 'Assets/Tests/EditMode/UI'
git commit -m "feat: add chapter01 canvas ui presenter"
```

---

### Task 3：把第一章 UI 调用从临时 OnGUI 分发到新 Presenter

**Files:**
- Modify: `D:\Unity\New\Assets\Scripts\Core\GardenGameManager.cs`
- Modify: `D:\Unity\New\Assets\Scripts\UI\PrototypeRuntimeUI.cs`

- [ ] **Step 1: 在 GardenGameManager 中新增第一章 UI presenter 字段**

在 `GardenGameManager.cs` 里新增字段：

```csharp
public Chapter01CanvasUI chapter01CanvasUI;
private IChapter01RuntimeUIPresenter _chapter01Presenter;
```

并在 `Awake()` 中初始化：

```csharp
_chapter01Presenter = ResolveChapter01Presenter();

if (_chapter01Presenter != null)
{
    _chapter01Presenter.SetPageCount(CurrentSaveData.collectedPages, totalPages);
    _chapter01Presenter.SetFadeAlpha(ShouldPlayIntroOnStart() ? 1f : 0f);
}
```

- [ ] **Step 2: 新增 ResolveChapter01Presenter，并改写第一章 UI 分发**

在 `GardenGameManager.cs` 中加入：

```csharp
private IChapter01RuntimeUIPresenter ResolveChapter01Presenter()
{
    if (chapter01CanvasUI != null)
    {
        return chapter01CanvasUI;
    }

    if (runtimeUI is IChapter01RuntimeUIPresenter fallbackPresenter)
    {
        return fallbackPresenter;
    }

    return null;
}
```

并把这些调用改成优先走 `_chapter01Presenter`：

```csharp
public void SetInteractionPrompt(string prompt)
{
    _chapter01Presenter?.SetInteractionPrompt(prompt);
}

public void SetObjective(string objective)
{
    _chapter01Presenter?.SetObjective(objective);
}

public void ShowToast(string message, float duration = 2.2f)
{
    _chapter01Presenter?.ShowToast(message, duration);
}

public void ShowDialogue(DialogueLine[] lines, Action onCompleted)
{
    if (_chapter01Presenter == null)
    {
        onCompleted?.Invoke();
        return;
    }

    _chapter01Presenter.ShowDialogue(lines, onCompleted);
}

public void ShowDirectionChoice(string[] options, Action<string> onSelected)
{
    if (_chapter01Presenter == null)
    {
        onSelected?.Invoke(string.Empty);
        return;
    }

    _chapter01Presenter.ShowDirectionChoice(options, onSelected);
}
```

- [ ] **Step 3: 让 PrototypeRuntimeUI 不再承担正式第一章 UI**

在 `PrototypeRuntimeUI.cs` 类签名上实现 fallback 接口：

```csharp
public class PrototypeRuntimeUI : MonoBehaviour, IChapter02QuizPresenter, IChapter01RuntimeUIPresenter
```

然后保留现有实现，但在 `Garden_Main` 场景真正挂上 `Chapter01CanvasUI` 之后，让它只作为 fallback 和第二章 presenter 存在。

这一步不删除 `OnGUI` 代码，只确保第一章默认走新 presenter。

- [ ] **Step 4: 运行一次 EditMode 测试和脚本编译**

Run:

```powershell
& 'D:\Unity\2022.3.62f3c1\Editor\Unity.exe' -batchmode -projectPath 'D:\Unity\New' -runTests -testPlatform EditMode -testResults 'D:\Unity\New\TestResults\chapter01-ui-task3.xml' -logFile 'D:\Unity\New\TestResults\chapter01-ui-task3.log' -quit
```

Expected:
- PASS
- 无新的编译错误

- [ ] **Step 5: Commit**

```powershell
git add 'Assets/Scripts/Core/GardenGameManager.cs' 'Assets/Scripts/UI/PrototypeRuntimeUI.cs'
git commit -m "refactor: route chapter01 ui through presenter"
```

---

### Task 4：补齐暗闸校准 UI 的数据通路

**Files:**
- Modify: `D:\Unity\New\Assets\Scripts\Chapters\Chapter01\GateInteractable.cs`
- Modify: `D:\Unity\New\Assets\Scripts\Chapters\Chapter01\Chapter01Director.cs`
- Test: `D:\Unity\New\Assets\Tests\EditMode\UI\Chapter01CanvasUITests.cs`

- [ ] **Step 1: 让 GateInteractable 暴露 UI 所需信息**

在 `GateInteractable.cs` 中补充：

```csharp
public float MinValidAngle
{
    get { return NormalizeAngle(ResolvedTargetAngle - Mathf.Max(0.1f, validAngleTolerance)); }
}

public float MaxValidAngle
{
    get { return NormalizeAngle(ResolvedTargetAngle + Mathf.Max(0.1f, validAngleTolerance)); }
}
```

并修正 `gateDisplayName` 默认值为中文：

```csharp
public string gateDisplayName = "暗闸";
```

- [ ] **Step 2: 在 Chapter01Director 中实时推送校准面板数据**

在 `Chapter01Director.cs` 新增方法：

```csharp
private void UpdateGateCalibrationUI()
{
    if (manager == null)
    {
        return;
    }

    if (_activeGatePuzzle == null)
    {
        manager.HideGateCalibration();
        return;
    }

    manager.ShowGateCalibration(new Chapter01GateCalibrationViewData
    {
        gateName = _activeGatePuzzle.gateDisplayName,
        currentAngle = _activeGatePuzzle.CurrentAngle,
        targetAngle = _activeGatePuzzle.ResolvedTargetAngle,
        validAngleTolerance = _activeGatePuzzle.validAngleTolerance,
        canConfirm = _activeGatePuzzle.IsWithinCalibrationTolerance(),
        negativeKey = gateRotateNegativeKey,
        positiveKey = gateRotatePositiveKey,
        confirmKey = gateConfirmKey,
        cancelKey = gateCancelKey
    });
}
```

在以下位置调用：

- `HandleGateInteraction()` 末尾
- `HandleGatePuzzleInput()` 每次旋转后
- `SolveGatePuzzle()` 中关闭前
- `CancelGatePuzzle()` 中关闭时

- [ ] **Step 3: 给 GardenGameManager 增加暗闸 UI 分发方法**

在 `GardenGameManager.cs` 中增加：

```csharp
public void ShowGateCalibration(Chapter01GateCalibrationViewData data)
{
    _chapter01Presenter?.ShowGateCalibration(data);
}

public void HideGateCalibration()
{
    _chapter01Presenter?.HideGateCalibration();
}
```

- [ ] **Step 4: 增加一个暗闸 UI 测试，验证隐藏逻辑**

在 `Chapter01CanvasUITests.cs` 中追加：

```csharp
[Test]
public void HideGateCalibration_ShouldDisablePanel()
{
    var root = new GameObject("UI");
    var ui = root.AddComponent<Chapter01CanvasUI>();
    ui.gateCalibrationPanel = new GameObject("GatePanel");
    ui.gateCalibrationPanel.SetActive(true);

    ui.HideGateCalibration();

    Assert.IsFalse(ui.gateCalibrationPanel.activeSelf);
}
```

- [ ] **Step 5: 运行测试**

Run:

```powershell
& 'D:\Unity\2022.3.62f3c1\Editor\Unity.exe' -batchmode -projectPath 'D:\Unity\New' -runTests -testPlatform EditMode -testResults 'D:\Unity\New\TestResults\chapter01-ui-task4.xml' -logFile 'D:\Unity\New\TestResults\chapter01-ui-task4.log' -quit
```

Expected:
- PASS
- 暗闸相关显示接口可编译

- [ ] **Step 6: Commit**

```powershell
git add 'Assets/Scripts/Chapters/Chapter01/GateInteractable.cs' 'Assets/Scripts/Chapters/Chapter01/Chapter01Director.cs' 'Assets/Scripts/Core/GardenGameManager.cs' 'Assets/Tests/EditMode/UI/Chapter01CanvasUITests.cs'
git commit -m "feat: add chapter01 gate calibration ui flow"
```

---

### Task 5：制作 UI 预制体并挂回 Garden_Main

**Files:**
- Create: `D:\Unity\New\Assets\Prefabs\UI\Chapter01\Chapter01UIRoot.prefab`
- Modify: `D:\Unity\New\Assets\Scenes\Garden_Main.unity`

- [ ] **Step 1: 在 Unity 中创建第一章正式 UI 预制体**

在 Unity 编辑器内执行：

1. 新建 `Canvas`，命名为 `Chapter01UIRoot`
2. 渲染模式设为 `Screen Space - Overlay`
3. 添加 `CanvasScaler`
4. 参考分辨率设为 `1920 x 1080`
5. 添加 `GraphicRaycaster`
6. 挂载 `Chapter01CanvasUI`

预制体路径：

```text
Assets/Prefabs/UI/Chapter01/Chapter01UIRoot.prefab
```

- [ ] **Step 2: 在预制体中建立正式层级**

在预制体内按以下层级创建对象：

```text
Chapter01UIRoot
├─ TopLeft
│  ├─ PageCounterPanel
│  └─ ObjectivePanel
├─ TopCenter
│  └─ ToastPanel
├─ Center
│  └─ FlowChoicePanel
├─ Right
│  └─ GateCalibrationPanel
├─ BottomCenter
│  └─ InteractionPromptPanel
└─ Bottom
   └─ DialoguePanel
```

要求：

- 所有文本使用导入的思源字体创建 TMP Font Asset 后绑定
- 所有主要面板使用深墨底 + 金边
- FlowChoicePanel 和 GateCalibrationPanel 使用最完整的边框层级

- [ ] **Step 3: 将预制体实例挂入 Garden_Main 并连接 GameManager**

在 `Garden_Main` 场景内：

1. 实例化 `Chapter01UIRoot.prefab`
2. 选中 `GardenGameManager`
3. 将 `Chapter01CanvasUI` 拖入 `chapter01CanvasUI` 字段
4. 保留 `runtimeUI` 供 fallback / Chapter02 使用

- [ ] **Step 4: 运行第一章手工验证流程**

在 Unity 编辑器中手工验证：

1. 进入 `Garden_Main`
2. 完成开场
3. 触发左 / 右暗闸
4. 检查右侧暗闸校准 UI
5. 解开两侧暗闸
6. 打开水流选择面板
7. 选错一次
8. 选对一次
9. 检查对话、提示条、目标栏、残页计数

Expected:

- 所有第一章 UI 块都来自 Canvas 正式 UI
- 第二章功能不受影响

- [ ] **Step 5: Commit**

```powershell
git add 'Assets/Prefabs/UI/Chapter01/Chapter01UIRoot.prefab' 'Assets/Scenes/Garden_Main.unity'
git commit -m "feat: add chapter01 formal canvas ui"
```

---

## 自查

### Spec 覆盖情况

- 第一章 7 个 UI 模块：已覆盖
- `B` 风格正式 UI：已覆盖
- Canvas 化替换：已覆盖
- 给同学换皮的结构：已覆盖
- 暗闸校准专用面板：已覆盖
- 与现有玩法解耦：已覆盖

### Placeholder 扫描

- 无 `TODO`
- 无 `TBD`
- 无“后续再补”类型描述

### 类型和命名一致性

- presenter 接口统一为 `IChapter01RuntimeUIPresenter`
- 暗闸数据统一为 `Chapter01GateCalibrationViewData`
- 第一章正式 UI 主脚本统一为 `Chapter01CanvasUI`

---

Plan complete and saved to `docs/superpowers/plans/2026-04-16-chapter01-formal-ui.md`. Two execution options:

**1. Subagent-Driven (recommended)** - 我按任务分发并逐段 review，推进更稳  
**2. Inline Execution** - 我就在当前会话直接连续实现

Which approach?
