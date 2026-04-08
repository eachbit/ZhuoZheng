using System;
using System.Collections;
using UnityEngine;

namespace ZhuozhengYuan
{
    public class PrototypeRuntimeUI : MonoBehaviour
    {
        public GardenGameManager gameManager;
        public int totalPages = 5;

        private string _interactionPrompt = string.Empty;
        private string _objectiveText = string.Empty;
        private string _toastText = string.Empty;
        private float _toastUntilTime;
        private int _collectedPages;
        private float _fadeAlpha;

        private DialogueLine[] _activeDialogue;
        private int _dialogueIndex;
        private Action _dialogueCompletedCallback;

        private bool _isDirectionChoiceOpen;
        private string[] _directionOptions;
        private Action<string> _directionSelectedCallback;

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
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawPageCounter();
            DrawObjective();
            DrawToast();
            DrawInteractionPrompt();
            DrawDialogueBox();
            DrawDirectionChoice();
            DrawFadeOverlay();
        }

        private void DrawPageCounter()
        {
            GUI.Box(new Rect(24f, 20f, 180f, 54f), string.Empty);
            GUI.Label(new Rect(38f, 30f, 156f, 28f), "残页：" + _collectedPages + "/" + totalPages, _titleStyle);
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
            if (string.IsNullOrEmpty(_interactionPrompt) || IsDialogueOpen || _isDirectionChoiceOpen)
            {
                return;
            }

            float width = 340f;
            float height = 44f;
            Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 126f, width, height);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, rect.height - 12f), _interactionPrompt, _bodyStyle);
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

            if (GUI.Button(new Rect(rect.x + rect.width - 126f, rect.y + rect.height - 46f, 100f, 28f), "继续", _buttonStyle))
            {
                AdvanceDialogue();
            }

            GUI.Label(new Rect(rect.x + 18f, rect.y + rect.height - 40f, 220f, 24f), "空格继续", _smallStyle);
        }

        private void DrawDirectionChoice()
        {
            if (!_isDirectionChoiceOpen)
            {
                return;
            }

            Rect rect = new Rect((Screen.width - 360f) * 0.5f, (Screen.height - 240f) * 0.5f, 360f, 240f);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 16f, rect.width - 40f, 28f), "调定水流方向", _titleStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 48f, rect.width - 40f, 36f), "请选择水流所向（鼠标点击或按 1/2/3）：", _bodyStyle);

            for (int index = 0; index < _directionOptions.Length; index++)
            {
                string option = _directionOptions[index];
                if (GUI.Button(new Rect(rect.x + 32f, rect.y + 92f + index * 42f, rect.width - 64f, 32f), (index + 1) + ". " + option, _buttonStyle))
                {
                    ChooseDirection(option);
                }
            }
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
