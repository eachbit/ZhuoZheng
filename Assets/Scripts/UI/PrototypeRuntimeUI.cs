using System;
using System.Collections;
using UnityEngine;

namespace ZhuozhengYuan
{
    public class PrototypeRuntimeUI : MonoBehaviour, IChapter01RuntimeUIPresenter, IChapter02QuizPresenter
    {
        public GardenGameManager gameManager;
        public int totalPages = 5;
        public bool suppressChapter01Overlay;

        private string _interactionPrompt = string.Empty;
        private string _objectiveText = string.Empty;
        private string _toastText = string.Empty;
        private float _toastUntilTime;
        private string _directionResultTitle = string.Empty;
        private string _directionResultText = string.Empty;
        private float _directionResultUntilTime;
        private Color _directionResultAccent = new Color(0.85f, 0.9f, 1f, 1f);
        private Color _directionFlashColor = new Color(0.85f, 0.9f, 1f, 0f);
        private float _directionFlashStartTime = -10f;
        private float _directionFlashUntilTime = -10f;
        private int _collectedPages;
        private float _fadeAlpha;

        private DialogueLine[] _activeDialogue;
        private int _dialogueIndex;
        private Action _dialogueCompletedCallback;

        private bool _isDirectionChoiceOpen;
        private string[] _directionOptions;
        private Action<string> _directionSelectedCallback;
        private bool _isChapter02QuizOpen;
        private string _chapter02QuizTitle = string.Empty;
        private string _chapter02QuizProgressText = string.Empty;
        private string _chapter02QuizQuestionText = string.Empty;
        private string[] _chapter02QuizOptions;
        private Action<int> _chapter02QuizSelectedCallback;

        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _smallStyle;
        private GUIStyle _buttonStyle;

        public bool IsDialogueOpen
        {
            get { return _activeDialogue != null && _activeDialogue.Length > 0; }
        }

        public IEnumerator Fade(float from, float to, float duration)
        {
            _fadeAlpha = from;

            if (duration <= 0f)
            {
                _fadeAlpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _fadeAlpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            _fadeAlpha = to;
        }

        public void SetFadeAlpha(float alpha)
        {
            _fadeAlpha = Mathf.Clamp01(alpha);
        }

        public void SetPageCount(int currentPages, int maxPages)
        {
            _collectedPages = currentPages;
            totalPages = maxPages;
        }

        public void SetInteractionPrompt(string prompt)
        {
            _interactionPrompt = prompt ?? string.Empty;
        }

        public void SetObjective(string objective)
        {
            _objectiveText = objective ?? string.Empty;
        }

        public void ShowToast(string message, float duration = 2.2f)
        {
            _toastText = message ?? string.Empty;
            _toastUntilTime = Time.unscaledTime + duration;
        }

        public void ShowDirectionResult(string title, string message, Color accentColor, float duration = 2.6f)
        {
            _directionResultTitle = title ?? string.Empty;
            _directionResultText = message ?? string.Empty;
            _directionResultAccent = accentColor;
            _directionResultUntilTime = Time.unscaledTime + duration;
            ShowDirectionFlash(accentColor, 0.55f);
        }

        public void ShowDirectionFlash(Color accentColor, float duration = 0.55f)
        {
            _directionFlashColor = accentColor;
            _directionFlashStartTime = Time.unscaledTime;
            _directionFlashUntilTime = _directionFlashStartTime + Mathf.Max(0.05f, duration);
        }

        public void ShowDialogue(DialogueLine[] dialogueLines, Action onCompleted)
        {
            if (dialogueLines == null || dialogueLines.Length == 0)
            {
                onCompleted?.Invoke();
                return;
            }

            _activeDialogue = dialogueLines;
            _dialogueIndex = 0;
            _dialogueCompletedCallback = onCompleted;

            if (gameManager != null)
            {
                gameManager.SetDialogueActive(true);
            }
        }

        public void ShowDirectionChoice(string[] options, Action<string> onSelected)
        {
            _directionOptions = options ?? Array.Empty<string>();
            _directionSelectedCallback = onSelected;
            _isDirectionChoiceOpen = true;

            if (gameManager != null)
            {
                gameManager.SetDirectionChoiceActive(true);
            }
        }

        public void ShowGateCalibration(Chapter01GateCalibrationViewData data)
        {
        }

        public void HideGateCalibration()
        {
        }

        public void ShowChapter02Quiz(string title, string progressText, string questionText, string[] options, Action<int> onSelected)
        {
            _chapter02QuizTitle = title ?? string.Empty;
            _chapter02QuizProgressText = progressText ?? string.Empty;
            _chapter02QuizQuestionText = questionText ?? string.Empty;
            _chapter02QuizOptions = options ?? Array.Empty<string>();
            _chapter02QuizSelectedCallback = onSelected;
            _isChapter02QuizOpen = true;

            if (gameManager != null)
            {
                gameManager.SetChapter02QuizActive(true);
            }
        }

        public void HideChapter02Quiz()
        {
            if (!_isChapter02QuizOpen)
            {
                return;
            }

            _isChapter02QuizOpen = false;
            _chapter02QuizSelectedCallback = null;
            _chapter02QuizOptions = null;

            if (gameManager != null)
            {
                gameManager.SetChapter02QuizActive(false);
            }
        }

        private void Update()
        {
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

            if (_isChapter02QuizOpen)
            {
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
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (!suppressChapter01Overlay)
            {
                DrawPageCounter();
                DrawObjective();
                DrawToast();
                DrawDirectionFlash();
                DrawDirectionResult();
                DrawInteractionPrompt();
                DrawDialogueBox();

                if (_isDirectionChoiceOpen)
                {
                    DrawDirectionChoice();
                }
            }

            if (_isChapter02QuizOpen)
            {
                DrawChapter02Quiz();
            }

            if (!suppressChapter01Overlay)
            {
                DrawFadeOverlay();
            }
        }

        private void DrawPageCounter()
        {
            GUI.Box(new Rect(24f, 20f, 180f, 54f), string.Empty);
            GUI.Label(new Rect(38f, 30f, 156f, 28f), "\u6b8b\u9875\uff1a" + _collectedPages + "/" + totalPages, _titleStyle);
        }

        private void DrawObjective()
        {
            if (string.IsNullOrEmpty(_objectiveText))
            {
                return;
            }

            GUI.Box(new Rect(24f, 82f, 420f, 70f), string.Empty);
            GUI.Label(new Rect(38f, 92f, 390f, 48f), _objectiveText, _bodyStyle);
        }

        private void DrawToast()
        {
            if (string.IsNullOrEmpty(_toastText) || Time.unscaledTime > _toastUntilTime)
            {
                return;
            }

            Rect rect = new Rect((Screen.width - 420f) * 0.5f, 28f, 420f, 52f);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 10f, rect.width - 32f, rect.height - 20f), _toastText, _bodyStyle);
        }

        private void DrawInteractionPrompt()
        {
            if (string.IsNullOrEmpty(_interactionPrompt) || IsDialogueOpen || _isDirectionChoiceOpen || _isChapter02QuizOpen)
            {
                return;
            }

            float width = 340f;
            float height = 44f;
            Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 126f, width, height);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, rect.height - 12f), _interactionPrompt, _bodyStyle);
        }

        private void DrawDirectionResult()
        {
            if (Time.unscaledTime > _directionResultUntilTime || (string.IsNullOrEmpty(_directionResultTitle) && string.IsNullOrEmpty(_directionResultText)))
            {
                return;
            }

            float width = 520f;
            float height = 112f;
            Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.18f, width, height);

            Color previousColor = GUI.color;
            GUI.color = new Color(0.08f, 0.1f, 0.16f, 0.92f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            Rect accentRect = new Rect(rect.x, rect.y, rect.width, 8f);
            GUI.color = _directionResultAccent;
            GUI.DrawTexture(accentRect, Texture2D.whiteTexture);
            GUI.color = previousColor;

            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 18f, rect.width - 40f, 28f), _directionResultTitle, _titleStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 50f, rect.width - 40f, 44f), _directionResultText, _bodyStyle);
        }

        private void DrawDirectionFlash()
        {
            if (Time.unscaledTime > _directionFlashUntilTime)
            {
                return;
            }

            float duration = Mathf.Max(0.05f, _directionFlashUntilTime - _directionFlashStartTime);
            float normalized = Mathf.Clamp01((Time.unscaledTime - _directionFlashStartTime) / duration);
            float pulse = Mathf.Sin(normalized * Mathf.PI);
            if (pulse <= 0.001f)
            {
                return;
            }

            Color previousColor = GUI.color;

            Color washColor = _directionFlashColor;
            washColor.a = 0.14f * pulse;
            GUI.color = washColor;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            Color bandColor = _directionFlashColor;
            bandColor.a = 0.24f * pulse;
            GUI.color = bandColor;
            float bandHeight = Screen.height * 0.26f;
            GUI.DrawTexture(new Rect(0f, Screen.height * 0.28f, Screen.width, bandHeight), Texture2D.whiteTexture);

            GUI.color = previousColor;
        }

        private void DrawDialogueBox()
        {
            if (!IsDialogueOpen)
            {
                return;
            }

            DialogueLine currentLine = _activeDialogue[_dialogueIndex];
            Rect rect = new Rect(60f, Screen.height - 230f, Screen.width - 120f, 160f);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 14f, rect.width - 36f, 30f), currentLine.speaker, _titleStyle);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 52f, rect.width - 36f, 64f), currentLine.text, _bodyStyle);

            if (GUI.Button(new Rect(rect.x + rect.width - 126f, rect.y + rect.height - 46f, 100f, 28f), "\u7ee7\u7eed", _buttonStyle))
            {
                AdvanceDialogue();
            }

            GUI.Label(new Rect(rect.x + 18f, rect.y + rect.height - 40f, 220f, 24f), "\u7a7a\u683c\u7ee7\u7eed", _smallStyle);
        }

        private void DrawDirectionChoice()
        {
            if (!_isDirectionChoiceOpen)
            {
                return;
            }

            string[] options = _directionOptions ?? Array.Empty<string>();

            Rect rect = new Rect((Screen.width - 360f) * 0.5f, (Screen.height - 240f) * 0.5f, 360f, 240f);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 16f, rect.width - 40f, 28f), "\u8c03\u5b9a\u6c34\u6d41\u65b9\u5411", _titleStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 48f, rect.width - 40f, 36f), "\u8bf7\u9009\u62e9\u6c34\u6d41\u53bb\u5411\uff08\u9f20\u6807\u70b9\u51fb\u6216\u6309 1/2/3\uff09\uff1a", _bodyStyle);

            for (int index = 0; index < options.Length; index++)
            {
                string option = options[index];
                if (GUI.Button(new Rect(rect.x + 32f, rect.y + 92f + index * 42f, rect.width - 64f, 32f), (index + 1) + ". " + option, _buttonStyle))
                {
                    ChooseDirection(option);
                    GUIUtility.ExitGUI();
                    return;
                }
            }

            GUI.Label(new Rect(rect.x + 20f, rect.y + rect.height - 34f, 180f, 22f), "Esc \u5173\u95ed", _smallStyle);
            if (GUI.Button(new Rect(rect.x + rect.width - 112f, rect.y + rect.height - 42f, 80f, 28f), "\u5173\u95ed", _buttonStyle))
            {
                CloseDirectionChoice();
                GUIUtility.ExitGUI();
                return;
            }
        }

        private void DrawChapter02Quiz()
        {
            if (!_isChapter02QuizOpen)
            {
                return;
            }

            string[] options = _chapter02QuizOptions ?? Array.Empty<string>();
            Rect rect = new Rect((Screen.width - 520f) * 0.5f, (Screen.height - 340f) * 0.5f, 520f, 340f);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 16f, rect.width - 40f, 28f), _chapter02QuizTitle, _titleStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 48f, rect.width - 40f, 24f), _chapter02QuizProgressText, _smallStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 80f, rect.width - 40f, 68f), _chapter02QuizQuestionText, _bodyStyle);

            for (int index = 0; index < options.Length; index++)
            {
                string option = options[index];
                if (GUI.Button(new Rect(rect.x + 26f, rect.y + 156f + index * 42f, rect.width - 52f, 32f), (index + 1) + ". " + option, _buttonStyle))
                {
                    ChooseChapter02QuizOption(index);
                    GUIUtility.ExitGUI();
                    return;
                }
            }

            GUI.Label(new Rect(rect.x + 20f, rect.y + rect.height - 30f, rect.width - 40f, 20f), "\u6309 1/2/3/4 \u9009\u62e9\u7b54\u6848", _smallStyle);
        }

        private void DrawFadeOverlay()
        {
            if (_fadeAlpha <= 0.001f)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(_fadeAlpha));
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void AdvanceDialogue()
        {
            if (!IsDialogueOpen)
            {
                return;
            }

            _dialogueIndex++;
            if (_dialogueIndex < _activeDialogue.Length)
            {
                return;
            }

            _activeDialogue = null;
            _dialogueIndex = 0;

            Action callback = _dialogueCompletedCallback;
            _dialogueCompletedCallback = null;

            if (gameManager != null)
            {
                gameManager.SetDialogueActive(false);
            }

            callback?.Invoke();
        }

        private void ChooseDirectionByIndex(int index)
        {
            if (_directionOptions == null || index < 0 || index >= _directionOptions.Length)
            {
                return;
            }

            ChooseDirection(_directionOptions[index]);
        }

        private void ChooseDirection(string option)
        {
            if (!_isDirectionChoiceOpen)
            {
                return;
            }

            _isDirectionChoiceOpen = false;

            Action<string> callback = _directionSelectedCallback;
            _directionSelectedCallback = null;
            _directionOptions = null;

            if (gameManager != null)
            {
                gameManager.SetDirectionChoiceActive(false);
            }

            callback?.Invoke(option);
        }

        private void CloseDirectionChoice()
        {
            if (!_isDirectionChoiceOpen)
            {
                return;
            }

            _isDirectionChoiceOpen = false;
            _directionSelectedCallback = null;
            _directionOptions = null;

            if (gameManager != null)
            {
                gameManager.SetDirectionChoiceActive(false);
            }
        }

        private void ChooseChapter02QuizOption(int index)
        {
            if (!_isChapter02QuizOpen || _chapter02QuizOptions == null || index < 0 || index >= _chapter02QuizOptions.Length)
            {
                return;
            }

            _isChapter02QuizOpen = false;

            Action<int> callback = _chapter02QuizSelectedCallback;
            _chapter02QuizSelectedCallback = null;
            _chapter02QuizOptions = null;

            if (gameManager != null)
            {
                gameManager.SetChapter02QuizActive(false);
            }

            callback?.Invoke(index);
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            _smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f, 1f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
