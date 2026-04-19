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
    public class Chapter01CanvasUI : MonoBehaviour, IChapter01RuntimeUIPresenter, IChapter02QuizPresenter
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
        public GameObject pageRewardPanel;
        public Image pageRewardAccent;
        public TextMeshProUGUI pageRewardTitleText;
        public TextMeshProUGUI pageRewardBodyText;

        [Header("Finale History")]
        public GameObject finaleHistoryPanel;
        public TextMeshProUGUI finaleHistoryTitleText;
        public TextMeshProUGUI finaleHistoryBodyText;
        public TextMeshProUGUI finaleHistoryHintText;
        public float finaleHistoryScrollSpeed = 42f;
        public float finaleHistoryStartOffsetY = -260f;

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

        [Header("Chapter 02 Quiz")]
        public GameObject chapter02QuizPanel;
        public TextMeshProUGUI chapter02QuizTitleText;
        public TextMeshProUGUI chapter02QuizProgressText;
        public TextMeshProUGUI chapter02QuizQuestionText;
        public Button[] chapter02QuizButtons;
        public TextMeshProUGUI[] chapter02QuizOptionTexts;
        public TextMeshProUGUI chapter02QuizHintText;

        [Header("Fade")]
        public CanvasGroup fadeCanvasGroup;
        public Image fadeImage;

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
        private bool _isChapter02QuizOpen;
        private string[] _chapter02QuizOptions = Array.Empty<string>();
        private Action<int> _chapter02QuizSelectedCallback;
        private bool _isFinaleHistoryOpen;
        private float _finaleHistoryShownAtRealtime;
        private Coroutine _toastCoroutine;
        private Coroutine _resultCoroutine;
        private Coroutine _pageRewardCoroutine;
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
            ui.HidePageRewardImmediate();
            ui.HideFinaleHistoryImmediate();
            ui.HideDialogueImmediate();
            ui.HideDirectionChoiceImmediate();
            ui.HideGateCalibration();
            ui.HideChapter02QuizImmediate();
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
            HidePageRewardImmediate();
            HideFinaleHistoryImmediate();
            HideDialogueImmediate();
            HideDirectionChoiceImmediate();
            HideGateCalibration();
            HideChapter02QuizImmediate();
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

            Button[] choiceButtons = directionChoiceButtons ?? Array.Empty<Button>();
            for (int index = 0; index < choiceButtons.Length; index++)
            {
                if (choiceButtons[index] != null)
                {
                    choiceButtons[index].onClick.RemoveAllListeners();
                }
            }

            Button[] quizButtons = chapter02QuizButtons ?? Array.Empty<Button>();
            for (int index = 0; index < quizButtons.Length; index++)
            {
                if (quizButtons[index] != null)
                {
                    quizButtons[index].onClick.RemoveAllListeners();
                }
            }
        }

        private void Update()
        {
            UpdateFinaleHistoryScroll();

            if (IsDialogueOpen && Input.GetKeyDown(KeyCode.Space))
            {
                AdvanceDialogue();
            }

            if (_isDirectionChoiceOpen)
            {
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

            if (!_isChapter02QuizOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ChooseChapter02QuizOption(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ChooseChapter02QuizOption(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                ChooseChapter02QuizOption(2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                ChooseChapter02QuizOption(3);
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
            SetActiveSafe(objectivePanel, hasObjective && !_isChapter02QuizOpen);
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

        public void ShowPageReward(string title, string message, float duration = 3.4f)
        {
            EnsureFallbackHierarchy();
            SetActiveSafe(pageRewardPanel, true);

            if (pageRewardAccent != null)
            {
                pageRewardAccent.color = GoldTextColor;
            }

            if (pageRewardTitleText != null)
            {
                pageRewardTitleText.text = title ?? string.Empty;
            }

            if (pageRewardBodyText != null)
            {
                pageRewardBodyText.text = message ?? string.Empty;
            }

            if (_pageRewardCoroutine != null)
            {
                StopCoroutine(_pageRewardCoroutine);
            }

            _pageRewardCoroutine = StartCoroutine(HideAfterDelay(duration, HidePageRewardImmediate));
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

        public void ShowFinaleHistory(string title, string body)
        {
            EnsureFallbackHierarchy();
            SetActiveSafe(finaleHistoryPanel, true);

            if (finaleHistoryTitleText != null)
            {
                finaleHistoryTitleText.text = title ?? string.Empty;
            }

            if (finaleHistoryBodyText != null)
            {
                finaleHistoryBodyText.text = body ?? string.Empty;
            }

            if (finaleHistoryHintText != null)
            {
                finaleHistoryHintText.text = "游历至此落幕";
            }

            _isFinaleHistoryOpen = true;
            _finaleHistoryShownAtRealtime = Time.unscaledTime;
            ApplyFinaleHistoryScrollPosition(finaleHistoryStartOffsetY);

            if (finaleHistoryPanel != null)
            {
                finaleHistoryPanel.transform.SetAsLastSibling();
            }
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

        public void ShowChapter02Quiz(string title, string progressText, string questionText, string[] options, Action<int> onSelected)
        {
            EnsureFallbackHierarchy();
            _chapter02QuizOptions = options ?? Array.Empty<string>();
            _chapter02QuizSelectedCallback = onSelected;
            _isChapter02QuizOpen = true;
            SetActiveSafe(chapter02QuizPanel, true);
            SetActiveSafe(objectivePanel, false);
            gameManager?.SetChapter02QuizActive(true);

            if (chapter02QuizTitleText != null)
            {
                chapter02QuizTitleText.text = title ?? string.Empty;
            }

            if (chapter02QuizProgressText != null)
            {
                chapter02QuizProgressText.text = progressText ?? string.Empty;
            }

            if (chapter02QuizQuestionText != null)
            {
                chapter02QuizQuestionText.text = questionText ?? string.Empty;
            }

            Button[] buttons = chapter02QuizButtons ?? Array.Empty<Button>();
            TextMeshProUGUI[] optionTexts = chapter02QuizOptionTexts ?? Array.Empty<TextMeshProUGUI>();
            for (int index = 0; index < buttons.Length; index++)
            {
                bool isVisible = index < _chapter02QuizOptions.Length;
                if (buttons[index] != null)
                {
                    buttons[index].gameObject.SetActive(isVisible);
                }

                if (isVisible && index < optionTexts.Length && optionTexts[index] != null)
                {
                    optionTexts[index].text = (index + 1) + ". " + _chapter02QuizOptions[index];
                }
            }

            if (chapter02QuizHintText != null)
            {
                chapter02QuizHintText.text = "\u6309 1/2/3/4 \u9009\u62e9\u7b54\u6848";
            }
        }

        public void HideChapter02Quiz()
        {
            HideChapter02QuizImmediate();
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

        public void SetFadeColor(Color color)
        {
            EnsureFallbackHierarchy();

            Image image = fadeImage;
            if (image == null && fadeCanvasGroup != null)
            {
                image = fadeCanvasGroup.GetComponent<Image>();
                fadeImage = image;
            }

            if (image != null)
            {
                color.a = 1f;
                image.color = color;
            }
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

        private void ChooseChapter02QuizOption(int index)
        {
            if (!_isChapter02QuizOpen || index < 0 || index >= _chapter02QuizOptions.Length)
            {
                return;
            }

            Action<int> callback = _chapter02QuizSelectedCallback;
            _chapter02QuizSelectedCallback = null;
            HideChapter02QuizImmediate();
            callback?.Invoke(index);
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

        private void HidePageRewardImmediate()
        {
            SetActiveSafe(pageRewardPanel, false);
        }

        private void HideFinaleHistoryImmediate()
        {
            _isFinaleHistoryOpen = false;
            SetActiveSafe(finaleHistoryPanel, false);
        }

        private void UpdateFinaleHistoryScroll()
        {
            if (!_isFinaleHistoryOpen)
            {
                return;
            }

            float elapsed = Mathf.Max(0f, Time.unscaledTime - _finaleHistoryShownAtRealtime);
            ApplyFinaleHistoryScrollPosition(finaleHistoryStartOffsetY + (elapsed * Mathf.Max(1f, finaleHistoryScrollSpeed)));
        }

        private void ApplyFinaleHistoryScrollPosition(float offsetY)
        {
            SetAnchoredPositionY(finaleHistoryTitleText, offsetY);
            SetAnchoredPositionY(finaleHistoryBodyText, offsetY);
            SetAnchoredPositionY(finaleHistoryHintText, offsetY);
        }

        private static void SetAnchoredPositionY(TextMeshProUGUI text, float offsetY)
        {
            if (text == null)
            {
                return;
            }

            RectTransform rectTransform = text.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, offsetY);
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

        private void HideChapter02QuizImmediate()
        {
            _isChapter02QuizOpen = false;
            _chapter02QuizSelectedCallback = null;
            _chapter02QuizOptions = Array.Empty<string>();
            SetActiveSafe(chapter02QuizPanel, false);
            SetActiveSafe(objectivePanel, objectiveText != null && !string.IsNullOrWhiteSpace(objectiveText.text));
            gameManager?.SetChapter02QuizActive(false);
        }

        private void WireButtons()
        {
            if (dialogueContinueButton != null)
            {
                dialogueContinueButton.onClick.RemoveListener(AdvanceDialogue);
                dialogueContinueButton.onClick.AddListener(AdvanceDialogue);
            }

            Button[] choiceButtons = directionChoiceButtons ?? Array.Empty<Button>();
            for (int index = 0; index < choiceButtons.Length; index++)
            {
                if (choiceButtons[index] == null)
                {
                    continue;
                }

                int capturedIndex = index;
                choiceButtons[index].onClick.RemoveAllListeners();
                choiceButtons[index].onClick.AddListener(() => ChooseDirectionByIndex(capturedIndex));
            }

            Button[] quizButtons = chapter02QuizButtons ?? Array.Empty<Button>();
            for (int index = 0; index < quizButtons.Length; index++)
            {
                if (quizButtons[index] == null)
                {
                    continue;
                }

                int capturedIndex = index;
                quizButtons[index].onClick.RemoveAllListeners();
                quizButtons[index].onClick.AddListener(() => ChooseChapter02QuizOption(capturedIndex));
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
                && pageRewardPanel != null
                && pageRewardTitleText != null
                && pageRewardBodyText != null
                && finaleHistoryPanel != null
                && finaleHistoryTitleText != null
                && finaleHistoryBodyText != null
                && dialoguePanel != null
                && dialogueBodyText != null
                && directionChoicePanel != null
                && directionChoiceButtons != null
                && directionChoiceButtons.Length >= 3
                && gateCalibrationPanel != null
                && gateCalibrationTitleText != null
                && chapter02QuizPanel != null
                && chapter02QuizButtons != null
                && chapter02QuizButtons.Length >= 4
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

            pageRewardPanel = EnsurePanel("PageRewardPanel", canvas.transform, new Vector2(760f, 184f), new Vector2(0f, -388f), new Color(0.07f, 0.1f, 0.08f, 0.96f), new Vector2(0.5f, 1f));
            GameObject rewardAccent = EnsureImage("PageRewardAccent", pageRewardPanel.transform, GoldTextColor, Vector2.zero, Vector2.zero, new Vector2(0f, -8f), new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero);
            pageRewardAccent = rewardAccent.GetComponent<Image>();
            pageRewardTitleText = EnsureText("PageRewardTitleText", pageRewardPanel.transform, "\u83b7\u5f97\u6b8b\u9875", 38, FontStyles.Bold, new Vector2(34f, 92f), new Vector2(-34f, -20f), TextAlignmentOptions.TopLeft);
            pageRewardBodyText = EnsureText("PageRewardBodyText", pageRewardPanel.transform, "\u5df2\u83b7\u5f97\u300a\u957f\u7269\u5fd7\u300b\u7b2c\u4e8c\u5f20\u6b8b\u9875", 28, FontStyles.Normal, new Vector2(34f, 24f), new Vector2(-34f, -88f), TextAlignmentOptions.TopLeft);

            finaleHistoryPanel = EnsurePanel("FinaleHistoryPanel", canvas.transform, ReferenceResolution, Vector2.zero, new Color(0.97f, 0.965f, 0.94f, 1f), new Vector2(0.5f, 0.5f));
            finaleHistoryTitleText = EnsureText("FinaleHistoryTitleText", finaleHistoryPanel.transform, "拙政园", 56, FontStyles.Bold, new Vector2(260f, 700f), new Vector2(-260f, -210f), TextAlignmentOptions.TopLeft);
            finaleHistoryBodyText = EnsureText("FinaleHistoryBodyText", finaleHistoryPanel.transform, "这里显示拙政园历史背景。", 32, FontStyles.Normal, new Vector2(260f, 292f), new Vector2(-260f, -360f), TextAlignmentOptions.TopLeft);
            finaleHistoryHintText = EnsureText("FinaleHistoryHintText", finaleHistoryPanel.transform, "游历至此落幕", 26, FontStyles.Normal, new Vector2(260f, 148f), new Vector2(-260f, -878f), TextAlignmentOptions.BottomRight);
            SetActiveSafe(finaleHistoryPanel, false);

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

            chapter02QuizPanel = EnsurePanel("Chapter02QuizPanel", canvas.transform, new Vector2(1180f, 620f), Vector2.zero, new Color(0.08f, 0.09f, 0.08f, 0.96f), new Vector2(0.5f, 0.5f));
            chapter02QuizTitleText = EnsureText("Chapter02QuizTitleText", chapter02QuizPanel.transform, "\u7b2c\u4e8c\u7ae0\uff1a\u5c0f\u98de\u8679\u7b54\u9898", 50, FontStyles.Bold, new Vector2(54f, 470f), new Vector2(-54f, -38f), TextAlignmentOptions.TopLeft);
            chapter02QuizProgressText = EnsureText("Chapter02QuizProgressText", chapter02QuizPanel.transform, "\u7b54\u9898\u8fdb\u5ea6 1/4", 30, FontStyles.Normal, new Vector2(54f, 422f), new Vector2(-54f, -118f), TextAlignmentOptions.TopLeft);
            chapter02QuizQuestionText = EnsureText("Chapter02QuizQuestionText", chapter02QuizPanel.transform, "\u8bf7\u9009\u62e9\u6b63\u786e\u7b54\u6848\u3002", 36, FontStyles.Normal, new Vector2(54f, 312f), new Vector2(-54f, -174f), TextAlignmentOptions.TopLeft);
            chapter02QuizButtons = new Button[4];
            chapter02QuizOptionTexts = new TextMeshProUGUI[4];
            for (int index = 0; index < chapter02QuizButtons.Length; index++)
            {
                Button button = EnsureButton("Chapter02QuizButton" + index, chapter02QuizPanel.transform, "\u9009\u9879 " + (index + 1), new Vector2(0f, 72f), Vector2.zero, new Vector2(0f, 1f), new Vector2(1f, 1f));
                chapter02QuizButtons[index] = button;
                chapter02QuizOptionTexts[index] = button.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            chapter02QuizHintText = EnsureText("Chapter02QuizHintText", chapter02QuizPanel.transform, "\u6309 1/2/3/4 \u9009\u62e9\u7b54\u6848", 26, FontStyles.Normal, new Vector2(54f, 28f), new Vector2(-54f, -546f), TextAlignmentOptions.BottomLeft);

            GameObject fadeObject = EnsureImage("FadeOverlay", canvas.transform, new Color(0f, 0f, 0f, 1f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero);
            fadeImage = fadeObject.GetComponent<Image>();
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
            ApplyFont(pageRewardTitleText, true);
            ApplyFont(pageRewardBodyText, false);
            ApplyFont(finaleHistoryTitleText, true);
            ApplyFont(finaleHistoryBodyText, false);
            ApplyFont(finaleHistoryHintText, false);
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
            ApplyFont(chapter02QuizTitleText, true);
            ApplyFont(chapter02QuizProgressText, false);
            ApplyFont(chapter02QuizQuestionText, false);
            ApplyFont(chapter02QuizHintText, false);

            if (directionChoiceButtonTexts == null)
            {
                directionChoiceButtonTexts = Array.Empty<TextMeshProUGUI>();
            }

            for (int index = 0; index < directionChoiceButtonTexts.Length; index++)
            {
                ApplyFont(directionChoiceButtonTexts[index], true);
            }

            TextMeshProUGUI[] quizTexts = chapter02QuizOptionTexts ?? Array.Empty<TextMeshProUGUI>();
            for (int index = 0; index < quizTexts.Length; index++)
            {
                ApplyFont(quizTexts[index], true);
            }
        }

        private void ApplyFormalPlaqueLayout()
        {
            ApplyPanelChrome(pageCounterPanel, PanelInkColor);
            ApplyPanelChrome(objectivePanel, PanelInkSoftColor);
            ApplyPanelChrome(interactionPromptPanel, PanelInkColor);
            ApplyPanelChrome(toastPanel, PanelInkColor);
            ApplyPanelChrome(resultPanel, PanelInkColor);
            ApplyPanelChrome(pageRewardPanel, PanelInkColor);
            ApplyFinaleHistoryPanel();
            ApplyPanelChrome(dialoguePanel, PanelInkColor);
            ApplyPanelChrome(directionChoicePanel, PanelInkColor);
            ApplyPanelChrome(gateCalibrationPanel, PanelInkColor);
            ApplyPanelChrome(chapter02QuizPanel, PanelInkColor);

            SetRect(pageCounterPanel, new Vector2(0f, 1f), new Vector2(340f, 104f), new Vector2(28f, -24f));
            SetTextBox(pageCounterText, new Vector2(28f, 10f), new Vector2(-28f, -10f), 48f, TextAlignmentOptions.MidlineLeft);

            SetRect(objectivePanel, new Vector2(0f, 1f), new Vector2(820f, 148f), new Vector2(28f, -142f));
            SetTextBox(objectiveText, new Vector2(32f, 22f), new Vector2(-32f, -22f), 34f, TextAlignmentOptions.TopLeft);

            // The interaction prompt uses the selected B direction: a wide lower plaque instead of a small toast.
            SetRect(interactionPromptPanel, new Vector2(0.5f, 0f), new Vector2(940f, 118f), new Vector2(0f, 92f));
            SetTextBox(interactionPromptText, new Vector2(38f, 14f), new Vector2(-38f, -14f), 42f, TextAlignmentOptions.MidlineLeft);

            SetRect(toastPanel, new Vector2(0.5f, 1f), new Vector2(980f, 186f), new Vector2(0f, -40f));
            SetTextBox(toastText, new Vector2(54f, 24f), new Vector2(-54f, -24f), 30f, TextAlignmentOptions.TopLeft);
            ConstrainToastText(toastText);

            SetRect(resultPanel, new Vector2(0.5f, 1f), new Vector2(980f, 210f), new Vector2(0f, -184f));
            SetTextBox(resultTitleText, new Vector2(40f, 112f), new Vector2(-40f, -28f), 46f, TextAlignmentOptions.TopLeft);
            SetTextBox(resultBodyText, new Vector2(40f, 30f), new Vector2(-40f, -92f), 34f, TextAlignmentOptions.TopLeft);

            SetRect(pageRewardPanel, new Vector2(0.5f, 1f), new Vector2(860f, 194f), new Vector2(0f, -404f));
            SetTextBox(pageRewardTitleText, new Vector2(40f, 104f), new Vector2(-40f, -26f), 44f, TextAlignmentOptions.TopLeft);
            SetTextBox(pageRewardBodyText, new Vector2(40f, 30f), new Vector2(-40f, -96f), 32f, TextAlignmentOptions.TopLeft);

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

            SetRect(chapter02QuizPanel, new Vector2(0.5f, 0.5f), new Vector2(1180f, 620f), new Vector2(120f, -24f));
            SetTextBox(chapter02QuizTitleText, new Vector2(58f, 476f), new Vector2(-58f, -40f), 48f, TextAlignmentOptions.TopLeft);
            SetTextBox(chapter02QuizProgressText, new Vector2(58f, 426f), new Vector2(-58f, -126f), 30f, TextAlignmentOptions.TopLeft);
            SetTextBox(chapter02QuizQuestionText, new Vector2(58f, 318f), new Vector2(-58f, -182f), 34f, TextAlignmentOptions.TopLeft);
            SetTextBox(chapter02QuizHintText, new Vector2(58f, 18f), new Vector2(-58f, -574f), 24f, TextAlignmentOptions.BottomLeft);
            LayoutChapter02QuizButtons();
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

        private void LayoutChapter02QuizButtons()
        {
            if (chapter02QuizButtons == null)
            {
                return;
            }

            for (int index = 0; index < chapter02QuizButtons.Length; index++)
            {
                Button button = chapter02QuizButtons[index];
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
                rectTransform.offsetMin = new Vector2(70f, -352f - (index * 70f));
                rectTransform.offsetMax = new Vector2(-70f, -290f - (index * 70f));

                if (chapter02QuizOptionTexts != null && index < chapter02QuizOptionTexts.Length)
                {
                    SetTextBox(chapter02QuizOptionTexts[index], new Vector2(34f, 8f), new Vector2(-34f, -8f), 32f, TextAlignmentOptions.MidlineLeft);
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

        private void ApplyFinaleHistoryPanel()
        {
            if (finaleHistoryPanel == null)
            {
                return;
            }

            Image image = finaleHistoryPanel.GetComponent<Image>();
            if (image == null)
            {
                image = finaleHistoryPanel.AddComponent<Image>();
            }

            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = new Color(0.97f, 0.965f, 0.94f, 1f);

            SetRect(finaleHistoryPanel, new Vector2(0.5f, 0.5f), ReferenceResolution, Vector2.zero);
            SetTextBox(finaleHistoryTitleText, new Vector2(260f, 700f), new Vector2(-260f, -210f), 56f, TextAlignmentOptions.TopLeft);
            SetTextBox(finaleHistoryBodyText, new Vector2(260f, 292f), new Vector2(-260f, -360f), 32f, TextAlignmentOptions.TopLeft);
            SetTextBox(finaleHistoryHintText, new Vector2(260f, 148f), new Vector2(-260f, -878f), 26f, TextAlignmentOptions.BottomRight);

            if (finaleHistoryTitleText != null)
            {
                finaleHistoryTitleText.color = new Color(0.11f, 0.18f, 0.12f, 1f);
            }

            if (finaleHistoryBodyText != null)
            {
                finaleHistoryBodyText.color = new Color(0.12f, 0.16f, 0.12f, 1f);
                finaleHistoryBodyText.lineSpacing = 18f;
            }

            if (finaleHistoryHintText != null)
            {
                finaleHistoryHintText.color = new Color(0.36f, 0.42f, 0.35f, 1f);
            }
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

        private static void ConstrainToastText(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            text.enableWordWrapping = true;
            text.enableAutoSizing = true;
            text.fontSizeMax = Mathf.Min(text.fontSize, 31f);
            text.fontSizeMin = 22f;
            text.overflowMode = TextOverflowModes.Ellipsis;
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
