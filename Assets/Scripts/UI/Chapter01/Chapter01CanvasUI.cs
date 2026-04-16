using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ZhuozhengYuan
{
    public class Chapter01CanvasUI : MonoBehaviour, IChapter01RuntimeUIPresenter
    {
        public GardenGameManager gameManager;

        [Header("Fonts")]
        public TMP_FontAsset titleFont;
        public TMP_FontAsset bodyFont;

        [Header("HUD")]
        public GameObject pageCounterPanel;
        public TextMeshProUGUI pageCounterText;
        public GameObject objectivePanel;
        public TextMeshProUGUI objectiveText;
        public GameObject interactionPromptPanel;
        public TextMeshProUGUI interactionPromptText;
        public GameObject toastPanel;
        public TextMeshProUGUI toastText;
        public GameObject resultPanel;
        public Image resultAccent;
        public TextMeshProUGUI resultTitleText;
        public TextMeshProUGUI resultBodyText;

        [Header("Dialogue")]
        public GameObject dialoguePanel;
        public TextMeshProUGUI dialogueSpeakerText;
        public TextMeshProUGUI dialogueBodyText;
        public TextMeshProUGUI dialogueHintText;
        public Button dialogueContinueButton;

        [Header("Direction Choice")]
        public GameObject directionChoicePanel;
        public TextMeshProUGUI directionChoiceTitleText;
        public TextMeshProUGUI directionChoiceBodyText;
        public Button[] directionChoiceButtons;
        public TextMeshProUGUI[] directionChoiceButtonTexts;

        [Header("Gate Calibration")]
        public GameObject gateCalibrationPanel;
        public TextMeshProUGUI gateCalibrationTitleText;
        public TextMeshProUGUI gateCalibrationCurrentAngleText;
        public TextMeshProUGUI gateCalibrationTargetRangeText;
        public TextMeshProUGUI gateCalibrationHintText;
        public TextMeshProUGUI gateCalibrationControlsText;
        public Button gateCalibrationConfirmButton;
        public TextMeshProUGUI gateCalibrationConfirmButtonText;

        [Header("Fade")]
        public CanvasGroup fadeCanvasGroup;

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private const string DefaultDirectionChoiceTitle = "请为园中水脉选择去向";
        private const string DefaultDialogueHint = "按空格继续";
        private const string DefaultConfirmReadyText = "已对准，请按 E 确认";
        private const string DefaultConfirmPendingText = "尚未对准，请继续旋转";
        private const string DefaultToastTitle = "提示";

        private DialogueLine[] _activeDialogue;
        private int _dialogueIndex;
        private Action _dialogueCompletedCallback;
        private bool _isDirectionChoiceOpen;
        private string[] _directionOptions = Array.Empty<string>();
        private Action<string> _directionSelectedCallback;
        private Coroutine _toastCoroutine;
        private Coroutine _resultCoroutine;
        private Canvas _canvas;
        private static readonly Color PanelInkColor = new Color(0.065f, 0.105f, 0.08f, 0.78f);
        private static readonly Color PanelInkSoftColor = new Color(0.075f, 0.12f, 0.085f, 0.66f);
        private static readonly Color GoldLineColor = new Color(0.82f, 0.68f, 0.36f, 0.9f);
        private static readonly Color GoldTextColor = new Color(0.93f, 0.82f, 0.52f, 1f);
        private static readonly Color WarmTextColor = new Color(0.96f, 0.92f, 0.82f, 1f);
        private static Sprite s_plaqueBackgroundSprite;

        public bool IsDialogueOpen
        {
            get { return _activeDialogue != null && _activeDialogue.Length > 0; }
        }

        public static Chapter01CanvasUI CreateDefault()
        {
            EnsureEventSystem();

            GameObject root = new GameObject("Chapter01CanvasUI");
            DontDestroyOnLoad(root);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            root.AddComponent<GraphicRaycaster>();
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Chapter01CanvasUI ui = root.AddComponent<Chapter01CanvasUI>();
            ui.EnsureFallbackHierarchy();
            ui.ApplyDefaultFontsToKnownTexts();
            ui.ApplyFormalPlaqueLayout();
            ui.WireButtons();
            ui.HideToastImmediate();
            ui.HideResultImmediate();
            ui.HideDialogueImmediate();
            ui.HideDirectionChoiceImmediate();
            ui.HideGateCalibration();
            return ui;
        }

        private void Awake()
        {
            ResolveDefaultFonts();

            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
            }

            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 60;
            }

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            CanvasScaler currentScaler = GetComponent<CanvasScaler>();
            if (currentScaler == null)
            {
                currentScaler = gameObject.AddComponent<CanvasScaler>();
            }
            currentScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            currentScaler.referenceResolution = ReferenceResolution;
            currentScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            currentScaler.matchWidthOrHeight = 0.5f;

            EnsureFallbackHierarchy();
            ApplyDefaultFontsToKnownTexts();
            ApplyFormalPlaqueLayout();
            WireButtons();
            HideToastImmediate();
            HideResultImmediate();
            HideDialogueImmediate();
            HideDirectionChoiceImmediate();
            HideGateCalibration();
            SetFadeAlpha(0f);
        }

        private void OnEnable()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            EnsureFallbackHierarchy();
            ApplyDefaultFontsToKnownTexts();
            ApplyFormalPlaqueLayout();
        }

        private void OnDestroy()
        {
            if (dialogueContinueButton != null)
            {
                dialogueContinueButton.onClick.RemoveListener(AdvanceDialogue);
            }

            if (directionChoiceButtons == null)
            {
                return;
            }

            for (int index = 0; index < directionChoiceButtons.Length; index++)
            {
                if (directionChoiceButtons[index] != null)
                {
                    directionChoiceButtons[index].onClick.RemoveAllListeners();
                }
            }
        }

        private void Update()
        {
            if (IsDialogueOpen && Input.GetKeyDown(KeyCode.Space))
            {
                AdvanceDialogue();
            }

            if (!_isDirectionChoiceOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ChooseDirectionByIndex(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ChooseDirectionByIndex(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                ChooseDirectionByIndex(2);
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseDirectionChoice();
            }
        }

        public void SetPageCount(int currentPages, int maxPages)
        {
            EnsureFallbackHierarchy();
            SetActiveSafe(pageCounterPanel, true);
            if (pageCounterText != null)
            {
                pageCounterText.text = "残页：" + currentPages + "/" + maxPages;
            }
        }

        public void SetInteractionPrompt(string prompt)
        {
            EnsureFallbackHierarchy();
            bool hasPrompt = !string.IsNullOrWhiteSpace(prompt);
            SetActiveSafe(interactionPromptPanel, hasPrompt);
            if (hasPrompt && interactionPromptText != null)
            {
                interactionPromptText.text = prompt;
            }
        }

        public void SetObjective(string objective)
        {
            EnsureFallbackHierarchy();
            bool hasObjective = !string.IsNullOrWhiteSpace(objective);
            SetActiveSafe(objectivePanel, hasObjective);
            if (hasObjective && objectiveText != null)
            {
                objectiveText.text = objective;
            }
        }

        public void ShowToast(string message, float duration = 2.2f)
        {
            EnsureFallbackHierarchy();
            SetActiveSafe(toastPanel, true);
            if (toastText != null)
            {
                toastText.text = DefaultToastTitle + "\n" + (message ?? string.Empty);
            }

            if (_toastCoroutine != null)
            {
                StopCoroutine(_toastCoroutine);
            }

            _toastCoroutine = StartCoroutine(HideAfterDelay(duration, HideToastImmediate));
        }

        public void ShowDirectionResult(string title, string message, Color accentColor, float duration = 2.6f)
        {
            EnsureFallbackHierarchy();
            SetActiveSafe(resultPanel, true);
            if (resultAccent != null)
            {
                resultAccent.color = accentColor;
            }

            if (resultTitleText != null)
            {
                resultTitleText.text = title ?? string.Empty;
            }

            if (resultBodyText != null)
            {
                resultBodyText.text = message ?? string.Empty;
            }

            if (_resultCoroutine != null)
            {
                StopCoroutine(_resultCoroutine);
            }

            _resultCoroutine = StartCoroutine(HideAfterDelay(duration, HideResultImmediate));
        }

        public void ShowDialogue(DialogueLine[] dialogueLines, Action onCompleted)
        {
            EnsureFallbackHierarchy();
            if (dialogueLines == null || dialogueLines.Length == 0)
            {
                onCompleted?.Invoke();
                return;
            }

            _activeDialogue = dialogueLines;
            _dialogueIndex = 0;
            _dialogueCompletedCallback = onCompleted;
            SetActiveSafe(dialoguePanel, true);
            gameManager?.SetDialogueActive(true);
            RefreshDialogueLine();
        }

        public void ShowDirectionChoice(string[] options, Action<string> onSelected)
        {
            EnsureFallbackHierarchy();
            _directionOptions = options ?? Array.Empty<string>();
            _directionSelectedCallback = onSelected;
            _isDirectionChoiceOpen = true;
            SetActiveSafe(directionChoicePanel, true);
            gameManager?.SetDirectionChoiceActive(true);

            if (directionChoiceTitleText != null)
            {
                directionChoiceTitleText.text = DefaultDirectionChoiceTitle;
            }

            if (directionChoiceBodyText != null)
            {
                directionChoiceBodyText.text = "请结合眼前水势，判断哪一条去向能真正唤醒园中水脉。";
            }

            for (int index = 0; index < directionChoiceButtons.Length; index++)
            {
                bool isVisible = index < _directionOptions.Length;
                if (directionChoiceButtons[index] != null)
                {
                    directionChoiceButtons[index].gameObject.SetActive(isVisible);
                }

                if (isVisible && index < directionChoiceButtonTexts.Length && directionChoiceButtonTexts[index] != null)
                {
                    directionChoiceButtonTexts[index].text = (index + 1) + ". " + _directionOptions[index];
                }
            }
        }

        public void ShowGateCalibration(Chapter01GateCalibrationViewData data)
        {
            EnsureFallbackHierarchy();
            SetActiveSafe(gateCalibrationPanel, true);

            float minAngle = NormalizeAngle(data.targetAngle - Mathf.Abs(data.validAngleTolerance));
            float maxAngle = NormalizeAngle(data.targetAngle + Mathf.Abs(data.validAngleTolerance));

            if (gateCalibrationTitleText != null)
            {
                gateCalibrationTitleText.text = data.gateName + "校准";
            }

            if (gateCalibrationCurrentAngleText != null)
            {
                gateCalibrationCurrentAngleText.text = "当前角度：" + Mathf.RoundToInt(NormalizeAngle(data.currentAngle)) + "°";
            }

            if (gateCalibrationTargetRangeText != null)
            {
                gateCalibrationTargetRangeText.text = "正确区间：" + Mathf.RoundToInt(minAngle) + "° - " + Mathf.RoundToInt(maxAngle) + "°";
            }

            if (gateCalibrationHintText != null)
            {
                gateCalibrationHintText.text = string.IsNullOrWhiteSpace(data.rotationHint)
                    ? (data.canConfirm ? DefaultConfirmReadyText : DefaultConfirmPendingText)
                    : data.rotationHint;
            }

            if (gateCalibrationControlsText != null)
            {
                gateCalibrationControlsText.text =
                    "操作方式："
                    + data.negativeKey + " 向左旋转  /  "
                    + data.positiveKey + " 向右旋转  /  "
                    + data.confirmKey + " 确认  /  "
                    + data.cancelKey + " 取消";
            }

            if (gateCalibrationConfirmButton != null)
            {
                gateCalibrationConfirmButton.interactable = data.canConfirm;
            }

            if (gateCalibrationConfirmButtonText != null)
            {
                gateCalibrationConfirmButtonText.text = data.canConfirm ? "已进入正确位置" : "尚未进入正确位置";
            }
        }

        public void HideGateCalibration()
        {
            SetActiveSafe(gateCalibrationPanel, false);
        }

        public void SetFadeAlpha(float alpha)
        {
            EnsureFallbackHierarchy();
            if (fadeCanvasGroup == null)
            {
                return;
            }

            fadeCanvasGroup.alpha = Mathf.Clamp01(alpha);
            fadeCanvasGroup.blocksRaycasts = fadeCanvasGroup.alpha > 0.001f;
            fadeCanvasGroup.interactable = fadeCanvasGroup.alpha > 0.001f;
        }

        private void AdvanceDialogue()
        {
            if (!IsDialogueOpen)
            {
                return;
            }

            _dialogueIndex++;
            if (_dialogueIndex >= _activeDialogue.Length)
            {
                Action callback = _dialogueCompletedCallback;
                _activeDialogue = null;
                _dialogueCompletedCallback = null;
                HideDialogueImmediate();
                gameManager?.SetDialogueActive(false);
                callback?.Invoke();
                return;
            }

            RefreshDialogueLine();
        }

        private void RefreshDialogueLine()
        {
            if (!IsDialogueOpen || _dialogueIndex >= _activeDialogue.Length)
            {
                HideDialogueImmediate();
                return;
            }

            DialogueLine line = _activeDialogue[_dialogueIndex];
            if (dialogueSpeakerText != null)
            {
                dialogueSpeakerText.text = string.IsNullOrWhiteSpace(line.speaker) ? "园中回响" : line.speaker;
            }

            if (dialogueBodyText != null)
            {
                dialogueBodyText.text = line.text ?? string.Empty;
            }

            if (dialogueHintText != null)
            {
                dialogueHintText.text = DefaultDialogueHint;
            }
        }

        private void ChooseDirectionByIndex(int index)
        {
            if (!_isDirectionChoiceOpen || index < 0 || index >= _directionOptions.Length)
            {
                return;
            }

            Action<string> callback = _directionSelectedCallback;
            string selected = _directionOptions[index];
            _directionSelectedCallback = null;
            CloseDirectionChoice();
            callback?.Invoke(selected);
        }

        private void CloseDirectionChoice()
        {
            _isDirectionChoiceOpen = false;
            SetActiveSafe(directionChoicePanel, false);
            gameManager?.SetDirectionChoiceActive(false);
        }

        private IEnumerator HideAfterDelay(float duration, Action hideAction)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, duration));
            hideAction?.Invoke();
        }

        private void HideToastImmediate()
        {
            SetActiveSafe(toastPanel, false);
        }

        private void HideResultImmediate()
        {
            SetActiveSafe(resultPanel, false);
        }

        private void HideDialogueImmediate()
        {
            SetActiveSafe(dialoguePanel, false);
            gameManager?.SetDialogueActive(false);
        }

        private void HideDirectionChoiceImmediate()
        {
            _isDirectionChoiceOpen = false;
            SetActiveSafe(directionChoicePanel, false);
        }

        private void WireButtons()
        {
            if (dialogueContinueButton != null)
            {
                dialogueContinueButton.onClick.RemoveListener(AdvanceDialogue);
                dialogueContinueButton.onClick.AddListener(AdvanceDialogue);
            }

            if (directionChoiceButtons == null)
            {
                return;
            }

            for (int index = 0; index < directionChoiceButtons.Length; index++)
            {
                if (directionChoiceButtons[index] == null)
                {
                    continue;
                }

                int capturedIndex = index;
                directionChoiceButtons[index].onClick.RemoveAllListeners();
                directionChoiceButtons[index].onClick.AddListener(() => ChooseDirectionByIndex(capturedIndex));
            }
        }

        private void EnsureFallbackHierarchy()
        {
            if (pageCounterPanel != null
                && pageCounterText != null
                && objectivePanel != null
                && objectiveText != null
                && interactionPromptPanel != null
                && interactionPromptText != null
                && dialoguePanel != null
                && dialogueBodyText != null
                && directionChoicePanel != null
                && directionChoiceButtons != null
                && directionChoiceButtons.Length >= 3
                && gateCalibrationPanel != null
                && gateCalibrationTitleText != null
                && fadeCanvasGroup != null)
            {
                return;
            }

            BuildDefaultHierarchy(_canvas != null ? _canvas : GetComponent<Canvas>());
        }

        private void BuildDefaultHierarchy(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            ResolveDefaultFonts();
            _canvas = canvas;

            pageCounterPanel = EnsurePanel("PageCounterPanel", canvas.transform, new Vector2(220f, 72f), new Vector2(28f, -24f), new Color(0.08f, 0.1f, 0.09f, 0.84f), new Vector2(0f, 1f));
            pageCounterText = EnsureText("PageCounterText", pageCounterPanel.transform, "残页：0/5", 34, FontStyles.Bold, new Vector2(24f, 10f), new Vector2(-24f, -10f), TextAlignmentOptions.MidlineLeft);

            objectivePanel = EnsurePanel("ObjectivePanel", canvas.transform, new Vector2(560f, 130f), new Vector2(28f, -108f), new Color(0.08f, 0.11f, 0.1f, 0.82f), new Vector2(0f, 1f));
            objectiveText = EnsureText("ObjectiveText", objectivePanel.transform, "请前往目标地点。", 30, FontStyles.Normal, new Vector2(24f, 18f), new Vector2(-24f, -18f), TextAlignmentOptions.TopLeft);

            interactionPromptPanel = EnsurePanel("InteractionPromptPanel", canvas.transform, new Vector2(540f, 84f), new Vector2(0f, 130f), new Color(0.05f, 0.08f, 0.08f, 0.9f), new Vector2(0.5f, 0f));
            interactionPromptText = EnsureText("InteractionPromptText", interactionPromptPanel.transform, "按 E 操作", 30, FontStyles.Bold, new Vector2(22f, 10f), new Vector2(-22f, -10f), TextAlignmentOptions.MidlineLeft);

            toastPanel = EnsurePanel("ToastPanel", canvas.transform, new Vector2(520f, 110f), new Vector2(0f, -38f), new Color(0.11f, 0.14f, 0.12f, 0.92f), new Vector2(0.5f, 1f));
            toastText = EnsureText("ToastText", toastPanel.transform, "提示", 28, FontStyles.Normal, new Vector2(20f, 16f), new Vector2(-20f, -16f), TextAlignmentOptions.TopLeft);

            resultPanel = EnsurePanel("ResultPanel", canvas.transform, new Vector2(680f, 160f), new Vector2(0f, -180f), new Color(0.08f, 0.1f, 0.12f, 0.94f), new Vector2(0.5f, 1f));
            GameObject accent = EnsureImage("ResultAccent", resultPanel.transform, new Color(0.88f, 0.78f, 0.45f, 1f), Vector2.zero, Vector2.zero, new Vector2(0f, -8f), new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero);
            resultAccent = accent.GetComponent<Image>();
            resultTitleText = EnsureText("ResultTitleText", resultPanel.transform, "结果反馈", 34, FontStyles.Bold, new Vector2(24f, 80f), new Vector2(-24f, -22f), TextAlignmentOptions.TopLeft);
            resultBodyText = EnsureText("ResultBodyText", resultPanel.transform, "这里显示结果说明。", 26, FontStyles.Normal, new Vector2(24f, 20f), new Vector2(-24f, -68f), TextAlignmentOptions.TopLeft);

            dialoguePanel = EnsurePanel("DialoguePanel", canvas.transform, new Vector2(980f, 280f), new Vector2(0f, 46f), new Color(0.06f, 0.09f, 0.08f, 0.95f), new Vector2(0.5f, 0f));
            dialogueSpeakerText = EnsureText("DialogueSpeakerText", dialoguePanel.transform, "园中回响", 30, FontStyles.Bold, new Vector2(28f, 180f), new Vector2(-28f, -20f), TextAlignmentOptions.TopLeft);
            dialogueBodyText = EnsureText("DialogueBodyText", dialoguePanel.transform, "这里显示对话内容。", 28, FontStyles.Normal, new Vector2(28f, 76f), new Vector2(-28f, -68f), TextAlignmentOptions.TopLeft);
            dialogueHintText = EnsureText("DialogueHintText", dialoguePanel.transform, DefaultDialogueHint, 24, FontStyles.Normal, new Vector2(28f, 22f), new Vector2(-180f, -220f), TextAlignmentOptions.BottomLeft);
            dialogueContinueButton = EnsureButton("DialogueContinueButton", dialoguePanel.transform, "继续", new Vector2(170f, 58f), new Vector2(-28f, 24f), new Vector2(1f, 0f));

            directionChoicePanel = EnsurePanel("DirectionChoicePanel", canvas.transform, new Vector2(840f, 420f), Vector2.zero, new Color(0.08f, 0.09f, 0.08f, 0.96f), new Vector2(0.5f, 0.5f));
            directionChoiceTitleText = EnsureText("DirectionChoiceTitleText", directionChoicePanel.transform, DefaultDirectionChoiceTitle, 34, FontStyles.Bold, new Vector2(30f, 320f), new Vector2(-30f, -26f), TextAlignmentOptions.TopLeft);
            directionChoiceBodyText = EnsureText("DirectionChoiceBodyText", directionChoicePanel.transform, "请结合眼前水势，判断真正的去向。", 26, FontStyles.Normal, new Vector2(30f, 246f), new Vector2(-30f, -76f), TextAlignmentOptions.TopLeft);
            directionChoiceButtons = new Button[3];
            directionChoiceButtonTexts = new TextMeshProUGUI[3];
            for (int index = 0; index < 3; index++)
            {
                Button button = EnsureButton("DirectionChoiceButton" + index, directionChoicePanel.transform, "选项 " + (index + 1), new Vector2(0f, 72f), Vector2.zero, new Vector2(0f, 1f), new Vector2(1f, 1f));
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                buttonRect.offsetMin = new Vector2(30f, -228f - (index * 88f));
                buttonRect.offsetMax = new Vector2(-30f, -156f - (index * 88f));
                directionChoiceButtons[index] = button;
                directionChoiceButtonTexts[index] = button.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            gateCalibrationPanel = EnsurePanel("GateCalibrationPanel", canvas.transform, new Vector2(720f, 320f), new Vector2(0f, 70f), new Color(0.07f, 0.08f, 0.07f, 0.95f), new Vector2(0.5f, 0f));
            gateCalibrationTitleText = EnsureText("GateCalibrationTitleText", gateCalibrationPanel.transform, "左暗闸校准", 34, FontStyles.Bold, new Vector2(28f, 220f), new Vector2(-28f, -20f), TextAlignmentOptions.TopLeft);
            gateCalibrationCurrentAngleText = EnsureText("GateCalibrationCurrentAngleText", gateCalibrationPanel.transform, "当前角度：0°", 28, FontStyles.Normal, new Vector2(28f, 164f), new Vector2(-28f, -72f), TextAlignmentOptions.TopLeft);
            gateCalibrationTargetRangeText = EnsureText("GateCalibrationTargetRangeText", gateCalibrationPanel.transform, "正确区间：46° - 64°", 28, FontStyles.Normal, new Vector2(28f, 122f), new Vector2(-28f, -114f), TextAlignmentOptions.TopLeft);
            gateCalibrationHintText = EnsureText("GateCalibrationHintText", gateCalibrationPanel.transform, "请继续向右旋转", 32, FontStyles.Bold, new Vector2(28f, 74f), new Vector2(-28f, -164f), TextAlignmentOptions.TopLeft);
            gateCalibrationControlsText = EnsureText("GateCalibrationControlsText", gateCalibrationPanel.transform, "操作方式：A 向左 / D 向右 / E 确认 / Esc 取消", 24, FontStyles.Normal, new Vector2(28f, 24f), new Vector2(-28f, -214f), TextAlignmentOptions.TopLeft);
            gateCalibrationConfirmButton = EnsureButton("GateCalibrationConfirmButton", gateCalibrationPanel.transform, "尚未进入正确位置", new Vector2(260f, 60f), new Vector2(-28f, 24f), new Vector2(1f, 0f));
            gateCalibrationConfirmButton.interactable = false;
            gateCalibrationConfirmButtonText = gateCalibrationConfirmButton.GetComponentInChildren<TextMeshProUGUI>(true);

            GameObject fadeObject = EnsureImage("FadeOverlay", canvas.transform, new Color(0f, 0f, 0f, 1f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero);
            fadeCanvasGroup = fadeObject.GetComponent<CanvasGroup>();
            if (fadeCanvasGroup == null)
            {
                fadeCanvasGroup = fadeObject.AddComponent<CanvasGroup>();
            }
            fadeObject.transform.SetAsLastSibling();

            ApplyDefaultFontsToKnownTexts();
            ApplyFormalPlaqueLayout();
        }

        private GameObject EnsurePanel(string panelName, Transform parent, Vector2 size, Vector2 anchoredPosition, Color color, Vector2 anchor)
        {
            GameObject panel = FindOrCreateChild(panelName, parent);
            RectTransform rectTransform = EnsureRectTransform(panel);
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = anchor;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;

            Image image = panel.GetComponent<Image>();
            if (image == null)
            {
                image = panel.AddComponent<Image>();
            }
            image.sprite = GetPlaqueBackgroundSprite();
            image.type = Image.Type.Sliced;
            image.color = color.a < 0.9f ? PanelInkSoftColor : color;
            ApplyGardenFrame(panel.transform);
            return panel;
        }

        private void ApplyGardenFrame(Transform panel)
        {
            EnsureFrameStrip("GoldFrameTop", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -5f), Vector2.zero);
            EnsureFrameStrip("GoldFrameBottom", panel, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 5f));
            EnsureFrameStrip("GoldFrameLeft", panel, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(5f, 0f));
            EnsureFrameStrip("GoldFrameRight", panel, new Vector2(1f, 0f), Vector2.one, new Vector2(-5f, 0f), Vector2.zero);

            EnsureCornerBlock("GoldCornerUpperLeft", panel, new Vector2(0f, 1f), new Vector2(18f, -18f));
            EnsureCornerBlock("GoldCornerUpperRight", panel, Vector2.one, new Vector2(-18f, -18f));
            EnsureCornerBlock("GoldCornerLowerLeft", panel, Vector2.zero, new Vector2(18f, 18f));
            EnsureCornerBlock("GoldCornerLowerRight", panel, new Vector2(1f, 0f), new Vector2(-18f, 18f));
        }

        private void EnsureFrameStrip(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject strip = FindOrCreateChild(name, parent);
            RectTransform rectTransform = EnsureRectTransform(strip);
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;

            Image image = strip.GetComponent<Image>();
            if (image == null)
            {
                image = strip.AddComponent<Image>();
            }
            image.color = GoldLineColor;
            strip.transform.SetAsFirstSibling();
        }

        private void EnsureCornerBlock(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition)
        {
            GameObject corner = FindOrCreateChild(name, parent);
            RectTransform rectTransform = EnsureRectTransform(corner);
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = anchor;
            rectTransform.sizeDelta = new Vector2(18f, 18f);
            rectTransform.anchoredPosition = anchoredPosition;

            Image image = corner.GetComponent<Image>();
            if (image == null)
            {
                image = corner.AddComponent<Image>();
            }
            image.color = new Color(GoldLineColor.r, GoldLineColor.g, GoldLineColor.b, 0.7f);
            corner.transform.SetAsFirstSibling();
        }

        private GameObject EnsureImage(string imageName, Transform parent, Color color, Vector2 size, Vector2 anchoredPosition, Vector2 offsetMin, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMax)
        {
            GameObject imageObject = FindOrCreateChild(imageName, parent);
            RectTransform rectTransform = EnsureRectTransform(imageObject);
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;

            Image image = imageObject.GetComponent<Image>();
            if (image == null)
            {
                image = imageObject.AddComponent<Image>();
            }
            image.color = color;
            return imageObject;
        }

        private TextMeshProUGUI EnsureText(string textName, Transform parent, string value, float fontSize, FontStyles fontStyle, Vector2 offsetMin, Vector2 offsetMax, TextAlignmentOptions alignment)
        {
            GameObject textObject = FindOrCreateChild(textName, parent);
            RectTransform rectTransform = EnsureRectTransform(textObject);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = textObject.AddComponent<TextMeshProUGUI>();
            }

            bool prefersTitleFont = fontStyle == FontStyles.Bold;
            TMP_FontAsset preferredFont = prefersTitleFont && titleFont != null
                ? titleFont
                : bodyFont;
            if (preferredFont != null)
            {
                text.font = preferredFont;
            }

            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = preferredFont != null ? FontStyles.Normal : fontStyle;
            text.color = prefersTitleFont ? GoldTextColor : WarmTextColor;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            return text;
        }

        private void ResolveDefaultFonts()
        {
#if UNITY_EDITOR
            if (titleFont == null)
            {
                titleFont = CreateEditorRuntimeFontAsset("Assets/Fonts/SourceHan/SourceHanSerifSC-SemiBold.otf");
            }

            if (bodyFont == null)
            {
                bodyFont = CreateEditorRuntimeFontAsset("Assets/Fonts/SourceHan/SourceHanSansSC-Medium.otf");
            }
#endif
        }

        private void ApplyDefaultFontsToKnownTexts()
        {
            ApplyFont(pageCounterText, true);
            ApplyFont(objectiveText, false);
            ApplyFont(interactionPromptText, true);
            ApplyFont(toastText, false);
            ApplyFont(resultTitleText, true);
            ApplyFont(resultBodyText, false);
            ApplyFont(dialogueSpeakerText, true);
            ApplyFont(dialogueBodyText, false);
            ApplyFont(dialogueHintText, false);
            ApplyFont(directionChoiceTitleText, true);
            ApplyFont(directionChoiceBodyText, false);
            ApplyFont(gateCalibrationTitleText, true);
            ApplyFont(gateCalibrationCurrentAngleText, false);
            ApplyFont(gateCalibrationTargetRangeText, false);
            ApplyFont(gateCalibrationHintText, true);
            ApplyFont(gateCalibrationControlsText, false);
            ApplyFont(gateCalibrationConfirmButtonText, true);

            if (directionChoiceButtonTexts == null)
            {
                return;
            }

            for (int index = 0; index < directionChoiceButtonTexts.Length; index++)
            {
                ApplyFont(directionChoiceButtonTexts[index], true);
            }
        }

        private void ApplyFormalPlaqueLayout()
        {
            ApplyPanelChrome(pageCounterPanel, PanelInkColor);
            ApplyPanelChrome(objectivePanel, PanelInkSoftColor);
            ApplyPanelChrome(interactionPromptPanel, PanelInkColor);
            ApplyPanelChrome(toastPanel, PanelInkColor);
            ApplyPanelChrome(resultPanel, PanelInkColor);
            ApplyPanelChrome(dialoguePanel, PanelInkColor);
            ApplyPanelChrome(directionChoicePanel, PanelInkColor);
            ApplyPanelChrome(gateCalibrationPanel, PanelInkColor);

            SetRect(pageCounterPanel, new Vector2(0f, 1f), new Vector2(340f, 104f), new Vector2(28f, -24f));
            SetTextBox(pageCounterText, new Vector2(28f, 10f), new Vector2(-28f, -10f), 48f, TextAlignmentOptions.MidlineLeft);

            SetRect(objectivePanel, new Vector2(0f, 1f), new Vector2(820f, 148f), new Vector2(28f, -142f));
            SetTextBox(objectiveText, new Vector2(32f, 22f), new Vector2(-32f, -22f), 34f, TextAlignmentOptions.TopLeft);

            // The interaction prompt uses the selected B direction: a wide lower plaque instead of a small toast.
            SetRect(interactionPromptPanel, new Vector2(0.5f, 0f), new Vector2(940f, 118f), new Vector2(0f, 92f));
            SetTextBox(interactionPromptText, new Vector2(38f, 14f), new Vector2(-38f, -14f), 42f, TextAlignmentOptions.MidlineLeft);

            SetRect(toastPanel, new Vector2(0.5f, 1f), new Vector2(860f, 146f), new Vector2(0f, -40f));
            SetTextBox(toastText, new Vector2(40f, 18f), new Vector2(-40f, -18f), 34f, TextAlignmentOptions.TopLeft);

            SetRect(resultPanel, new Vector2(0.5f, 1f), new Vector2(980f, 210f), new Vector2(0f, -184f));
            SetTextBox(resultTitleText, new Vector2(40f, 112f), new Vector2(-40f, -28f), 46f, TextAlignmentOptions.TopLeft);
            SetTextBox(resultBodyText, new Vector2(40f, 30f), new Vector2(-40f, -92f), 34f, TextAlignmentOptions.TopLeft);

            SetRect(dialoguePanel, new Vector2(0.5f, 0f), new Vector2(1360f, 344f), new Vector2(0f, 48f));
            SetTextBox(dialogueSpeakerText, new Vector2(48f, 228f), new Vector2(-48f, -34f), 44f, TextAlignmentOptions.TopLeft);
            SetTextBox(dialogueBodyText, new Vector2(48f, 92f), new Vector2(-48f, -104f), 36f, TextAlignmentOptions.TopLeft);
            SetTextBox(dialogueHintText, new Vector2(48f, 28f), new Vector2(-250f, -276f), 28f, TextAlignmentOptions.BottomLeft);
            SetRect(dialogueContinueButton != null ? dialogueContinueButton.gameObject : null, new Vector2(1f, 0f), new Vector2(220f, 74f), new Vector2(-42f, 34f));

            SetRect(directionChoicePanel, new Vector2(0.5f, 0.5f), new Vector2(1180f, 580f), Vector2.zero);
            SetTextBox(directionChoiceTitleText, new Vector2(54f, 442f), new Vector2(-54f, -42f), 50f, TextAlignmentOptions.TopLeft);
            SetTextBox(directionChoiceBodyText, new Vector2(54f, 358f), new Vector2(-54f, -116f), 36f, TextAlignmentOptions.TopLeft);
            LayoutChoiceButtons();

            SetRect(gateCalibrationPanel, new Vector2(0.5f, 0f), new Vector2(1340f, 438f), new Vector2(0f, 54f));
            SetTextBox(gateCalibrationTitleText, new Vector2(58f, 302f), new Vector2(-58f, -36f), 56f, TextAlignmentOptions.TopLeft);
            SetTextBox(gateCalibrationCurrentAngleText, new Vector2(58f, 238f), new Vector2(-58f, -100f), 40f, TextAlignmentOptions.TopLeft);
            SetTextBox(gateCalibrationTargetRangeText, new Vector2(58f, 184f), new Vector2(-58f, -154f), 40f, TextAlignmentOptions.TopLeft);
            SetTextBox(gateCalibrationHintText, new Vector2(58f, 118f), new Vector2(-58f, -220f), 46f, TextAlignmentOptions.TopLeft);
            SetTextBox(gateCalibrationControlsText, new Vector2(58f, 48f), new Vector2(-58f, -308f), 30f, TextAlignmentOptions.TopLeft);
            SetRect(gateCalibrationConfirmButton != null ? gateCalibrationConfirmButton.gameObject : null, new Vector2(1f, 0f), new Vector2(390f, 82f), new Vector2(-50f, 42f));
            SetTextBox(gateCalibrationConfirmButtonText, new Vector2(18f, 10f), new Vector2(-18f, -10f), 34f, TextAlignmentOptions.Center);
        }

        private void LayoutChoiceButtons()
        {
            if (directionChoiceButtons == null)
            {
                return;
            }

            for (int index = 0; index < directionChoiceButtons.Length; index++)
            {
                Button button = directionChoiceButtons[index];
                if (button == null)
                {
                    continue;
                }

                RectTransform rectTransform = button.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    continue;
                }

                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.offsetMin = new Vector2(60f, -286f - (index * 112f));
                rectTransform.offsetMax = new Vector2(-60f, -190f - (index * 112f));

                if (directionChoiceButtonTexts != null && index < directionChoiceButtonTexts.Length)
                {
                    SetTextBox(directionChoiceButtonTexts[index], new Vector2(32f, 12f), new Vector2(-32f, -12f), 34f, TextAlignmentOptions.MidlineLeft);
                }
            }
        }

        private void ApplyPanelChrome(GameObject panel, Color color)
        {
            if (panel == null)
            {
                return;
            }

            Image image = panel.GetComponent<Image>();
            if (image == null)
            {
                image = panel.AddComponent<Image>();
            }

            image.sprite = GetPlaqueBackgroundSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            ApplyGardenFrame(panel.transform);
        }

        private static void SetRect(GameObject target, Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
        {
            if (target == null)
            {
                return;
            }

            RectTransform rectTransform = target.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = anchor;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        private static void SetTextBox(TextMeshProUGUI text, Vector2 offsetMin, Vector2 offsetMax, float fontSize, TextAlignmentOptions alignment)
        {
            if (text == null)
            {
                return;
            }

            RectTransform rectTransform = text.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                Vector2 resolvedOffsetMin = offsetMin;
                Vector2 resolvedOffsetMax = offsetMax;
                if (offsetMin.y < 0f && offsetMax.y > 0f)
                {
                    resolvedOffsetMin.y = offsetMax.y;
                    resolvedOffsetMax.y = offsetMin.y;
                }

                rectTransform.offsetMin = resolvedOffsetMin;
                rectTransform.offsetMax = resolvedOffsetMax;
            }

            text.fontSize = fontSize;
            text.alignment = alignment;
            text.enableWordWrapping = true;
        }

        private static Sprite GetPlaqueBackgroundSprite()
        {
            if (s_plaqueBackgroundSprite != null)
            {
                return s_plaqueBackgroundSprite;
            }

            const int size = 48;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Chapter01_PlaqueBackground_Runtime",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float vertical = y / (float)(size - 1);
                    float horizontal = Mathf.Abs((x / (float)(size - 1)) - 0.5f) * 2f;
                    float edgeFade = Mathf.Clamp01(Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y)) / 8f);
                    float softGrain = (((x * 17 + y * 31) % 11) - 5) / 255f;
                    float shade = Mathf.Lerp(0.72f, 1.12f, vertical) - (horizontal * 0.08f) + softGrain;
                    Color color = new Color(0.055f * shade, 0.11f * shade, 0.075f * shade, Mathf.Lerp(0.64f, 0.86f, edgeFade));
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            s_plaqueBackgroundSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(12f, 12f, 12f, 12f));
            return s_plaqueBackgroundSprite;
        }

        private void ApplyFont(TextMeshProUGUI text, bool preferTitleFont)
        {
            if (text == null)
            {
                return;
            }

            TMP_FontAsset fontAsset = preferTitleFont && titleFont != null ? titleFont : bodyFont;
            if (fontAsset != null)
            {
                text.font = fontAsset;
                text.fontStyle = FontStyles.Normal;
            }
            text.color = preferTitleFont ? GoldTextColor : WarmTextColor;
        }

#if UNITY_EDITOR
        private static TMP_FontAsset CreateEditorRuntimeFontAsset(string assetPath)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(assetPath);
            if (font == null)
            {
                return null;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
            fontAsset.name = font.name + "_RuntimeTMP";
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            return fontAsset;
        }
#endif

        private Button EnsureButton(string buttonName, Transform parent, string label, Vector2 size, Vector2 anchoredPosition, Vector2 anchor)
        {
            return EnsureButton(buttonName, parent, label, size, anchoredPosition, anchor, anchor);
        }

        private Button EnsureButton(string buttonName, Transform parent, string label, Vector2 size, Vector2 anchoredPosition, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObject = FindOrCreateChild(buttonName, parent);
            RectTransform rectTransform = EnsureRectTransform(buttonObject);
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(anchorMax.x, anchorMin.y);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;

            Image image = buttonObject.GetComponent<Image>();
            if (image == null)
            {
                image = buttonObject.AddComponent<Image>();
            }
            image.color = new Color(0.13f, 0.16f, 0.12f, 0.98f);

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.13f, 0.16f, 0.12f, 0.98f);
            colors.highlightedColor = new Color(0.22f, 0.26f, 0.18f, 1f);
            colors.pressedColor = new Color(0.32f, 0.28f, 0.16f, 1f);
            colors.disabledColor = new Color(0.17f, 0.17f, 0.17f, 0.55f);
            button.colors = colors;

            TextMeshProUGUI labelText = EnsureText("Label", buttonObject.transform, label, 26, FontStyles.Bold, new Vector2(16f, 10f), new Vector2(-16f, -10f), TextAlignmentOptions.Center);
            labelText.color = GoldTextColor;
            return button;
        }

        private static GameObject FindOrCreateChild(string childName, Transform parent)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child.gameObject;
            }

            GameObject gameObject = new GameObject(childName);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static RectTransform EnsureRectTransform(GameObject gameObject)
        {
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }
            return rectTransform;
        }

        private static void SetActiveSafe(GameObject target, bool isActive)
        {
            if (target != null && target.activeSelf != isActive)
            {
                target.SetActive(isActive);
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(eventSystemObject);
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0f)
            {
                angle += 360f;
            }
            return angle;
        }
    }
}
