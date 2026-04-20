using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZhuozhengYuan
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ZhuozhengYuan/UI/Chapter 03 Knowledge Panel")]
    public class Chapter03KnowledgePanel : MonoBehaviour
    {
        public struct KnowledgeSection
        {
            public KnowledgeSection(string title, string body)
            {
                Title = title;
                Body = body;
            }

            public string Title { get; }
            public string Body { get; }
        }

        [SerializeField]
        private Canvas targetCanvas;

        [SerializeField]
        private bool startHidden = true;

        private GameObject _panelRoot;
        private Chapter03AcousticDiagramGraphic _diagramGraphic;
        private GardenGameManager _manager;
        private Action _onClosed;
        private bool _isOpen;

        public bool IsOpen
        {
            get { return _isOpen; }
        }

        public static Chapter03KnowledgePanel ShowOrCreate(GardenGameManager manager, Action onClosed = null)
        {
            Chapter03KnowledgePanel panel = FindObjectOfType<Chapter03KnowledgePanel>(true);
            if (panel == null)
            {
                GameObject canvasObject = new GameObject(
                    "Chapter03KnowledgeCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 120;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                ConfigureScaler(scaler);

                panel = canvasObject.AddComponent<Chapter03KnowledgePanel>();
                panel.targetCanvas = canvas;
            }

            panel.Show(manager, onClosed);
            return panel;
        }

        public static KnowledgeSection[] CreateDefaultSections()
        {
            return new[]
            {
                new KnowledgeSection(
                    "\u5f27\u5f62\u5377\u68da\u9876",
                    "\u8fde\u7eed\u5f27\u9762\u53cd\u5c04\u58f0\u6ce2\uff0c\u524a\u5f31\u76f4\u8fbe\u58f0\u8fc7\u5f3a\uff0c\u8ba9\u6c34\u78e8\u8154\u66f4\u5706\u6da6\u9971\u6ee1\u3002"),
                new KnowledgeSection(
                    "\u65b9\u5f62\u4e0e\u8033\u623f",
                    "\u65b9\u5f62\u5e73\u9762\u4e0e\u56db\u9685\u8033\u623f\u8c03\u548c\u53cd\u5c04\u65f6\u5ef6\uff0c\u51cf\u5c11\u9a7b\u6ce2\uff0c\u4f7f\u5531\u8bcd\u8fdc\u8fd1\u6e05\u6670\u3002"),
                new KnowledgeSection(
                    "\u8377\u6c60\u6c34\u9762",
                    "\u6c34\u9762\u53cd\u5c04\u66f2\u58f0\u4e0e\u7b1b\u97f3\uff0c\u5ef6\u957f\u6df7\u54cd\uff0c\u4e30\u5bcc\u6cdb\u97f3\uff0c\u589e\u52a0\u4f59\u97f5\u3002"),
                new KnowledgeSection(
                    "\u5730\u9f99\u7a7a\u5c42",
                    "\u65b9\u7816\u4e0b\u7684\u5730\u9f99\u7a7a\u8154\u50cf\u5171\u9e23\u7bb1\uff0c\u8865\u8db3\u4f4e\u9891\uff0c\u4f7f\u8f7b\u58f0\u541f\u5531\u4e5f\u66f4\u6d51\u539a\u3002")
            };
        }

        private void Awake()
        {
            if (targetCanvas == null)
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }

            if (startHidden)
            {
                EnsureBuilt();
                SetPanelVisible(false);
            }
        }

        private void Update()
        {
            if (!_isOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                Close();
            }
        }

        public void Show(GardenGameManager manager, Action onClosed = null)
        {
            _manager = manager;
            _onClosed = onClosed;
            EnsureBuilt();
            EnsureEventSystem();
            SetPanelVisible(true);
            _isOpen = true;

            if (_diagramGraphic != null)
            {
                _diagramGraphic.Replay();
            }

            _manager?.SetChapter03KnowledgeActive(true);
        }

        public void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
            SetPanelVisible(false);
            _manager?.SetChapter03KnowledgeActive(false);

            Action callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        }

        private void EnsureBuilt()
        {
            if (_panelRoot != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }

            if (targetCanvas == null)
            {
                targetCanvas = gameObject.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                ConfigureScaler(gameObject.AddComponent<CanvasScaler>());
                gameObject.AddComponent<GraphicRaycaster>();
            }

            RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
            _panelRoot = CreateUIObject("Chapter03KnowledgePanel", canvasRect);
            Stretch(_panelRoot.GetComponent<RectTransform>());

            Image veil = _panelRoot.AddComponent<Image>();
            veil.color = new Color(0.01f, 0.026f, 0.023f, 0.98f);

            GameObject board = CreateUIObject("KnowledgeBoard", _panelRoot.transform);
            RectTransform boardRect = board.GetComponent<RectTransform>();
            boardRect.anchorMin = new Vector2(0.5f, 0.5f);
            boardRect.anchorMax = new Vector2(0.5f, 0.5f);
            boardRect.pivot = new Vector2(0.5f, 0.5f);
            boardRect.sizeDelta = new Vector2(1200f, 650f);
            boardRect.anchoredPosition = Vector2.zero;

            Image boardImage = board.AddComponent<Image>();
            boardImage.color = new Color(0.018f, 0.078f, 0.066f, 1f);

            global::Chapter03PlaqueFrame.ApplyPanel(board);

            CreateText(board.transform, "Title", "\u5345\u516d\u9e33\u9e2f\u9986\u7684\u6606\u66f2\u4f20\u97f3", 42, FontStyle.Bold,
                new Rect(54f, -34f, 660f, 58f), TextAnchor.MiddleLeft, new Color(0.98f, 0.94f, 0.78f, 1f));

            CreateText(board.transform, "Subtitle", "\u5f27\u5f62\u9876\u5b9a\u8c03\u3001\u65b9\u5f62\u5385\u5300\u573a\u3001\u6c34\u9762\u52a0\u6df7\u54cd\u3001\u5730\u9f99\u8865\u5171\u9e23", 22, FontStyle.Normal,
                new Rect(57f, -88f, 710f, 34f), TextAnchor.MiddleLeft, new Color(0.86f, 0.73f, 0.38f, 1f));

            CreateSummaryBand(board.transform);
            CreateSectionCards(board.transform);
            CreateDiagramPanel(board.transform);
            CreateCloseButton(board.transform);
        }

        private void CreateSummaryBand(Transform parent)
        {
            GameObject summary = CreateUIObject("KunquSummary", parent);
            RectTransform rect = summary.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(54f, -136f);
            rect.sizeDelta = new Vector2(610f, 96f);

            Image image = summary.AddComponent<Image>();
            image.color = new Color(0.72f, 0.55f, 0.2f, 0.28f);

            CreateText(summary.transform, "SummaryTitle", "\u4e00\u53e5\u8bdd\u603b\u7ed3", 24, FontStyle.Bold,
                new Rect(22f, -12f, 560f, 30f), TextAnchor.MiddleLeft, new Color(0.96f, 0.8f, 0.38f, 1f));
            CreateText(summary.transform, "SummaryBody", "\u56db\u79cd\u5efa\u7b51\u6761\u4ef6\u5171\u540c\u6210\u5c31\u201c\u8fdc\u8fbe\u800c\u4e0d\u566a\uff0c\u8fd1\u8046\u800c\u4e0d\u4fc3\u201d\u7684\u542c\u611f\u3002", 22, FontStyle.Normal,
                new Rect(22f, -47f, 560f, 42f), TextAnchor.UpperLeft, new Color(0.98f, 0.99f, 0.93f, 1f));
        }

        private void CreateSectionCards(Transform parent)
        {
            KnowledgeSection[] sections = CreateDefaultSections();
            for (int index = 0; index < sections.Length; index++)
            {
                int row = index / 2;
                int column = index % 2;
                GameObject card = CreateUIObject("KnowledgeSection_" + index.ToString("00"), parent);
                RectTransform rect = card.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(54f + column * 318f, -264f - row * 154f);
                rect.sizeDelta = new Vector2(292f, 128f);

                Image image = card.AddComponent<Image>();
                image.color = new Color(0f, 0.03f, 0.027f, 0.9f);

                CreateText(card.transform, "SectionTitle", sections[index].Title, 22, FontStyle.Bold,
                    new Rect(18f, -13f, 256f, 30f), TextAnchor.MiddleLeft, new Color(0.96f, 0.78f, 0.35f, 1f));
                CreateText(card.transform, "SectionBody", sections[index].Body, 19, FontStyle.Normal,
                    new Rect(18f, -50f, 256f, 66f), TextAnchor.UpperLeft, new Color(0.96f, 0.97f, 0.9f, 1f));
            }
        }

        private void CreateDiagramPanel(Transform parent)
        {
            GameObject panel = CreateUIObject("DynamicAcousticDiagram", parent);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-54f, -118f);
            rect.sizeDelta = new Vector2(462f, 390f);

            Image image = panel.AddComponent<Image>();
            image.color = new Color(0f, 0.024f, 0.024f, 0.96f);

            CreateText(panel.transform, "DiagramTitle", "\u52a8\u6001\u58f0\u5b66\u793a\u610f", 23, FontStyle.Bold,
                new Rect(26f, -18f, 400f, 32f), TextAnchor.MiddleLeft, new Color(0.98f, 0.8f, 0.35f, 1f));

            GameObject diagramObject = CreateUIObject("AcousticLineAnimation", panel.transform);
            RectTransform diagramRect = diagramObject.GetComponent<RectTransform>();
            diagramRect.anchorMin = new Vector2(0f, 0f);
            diagramRect.anchorMax = new Vector2(1f, 1f);
            diagramRect.offsetMin = new Vector2(30f, 62f);
            diagramRect.offsetMax = new Vector2(-30f, -64f);

            _diagramGraphic = diagramObject.AddComponent<Chapter03AcousticDiagramGraphic>();
            _diagramGraphic.color = new Color(1f, 0.96f, 0.76f, 1f);
            _diagramGraphic.raycastTarget = false;

            CreateText(panel.transform, "DiagramCaption", "\u4eae\u7ebf\u6f14\u793a\u58f0\u6ce2\u7ecf\u5377\u68da\u9876\u3001\u6c34\u9762\u4e0e\u5730\u9f99\u7a7a\u5c42\u5faa\u73af\u53cd\u5c04\u3002", 18, FontStyle.Normal,
                new Rect(28f, -330f, 406f, 44f), TextAnchor.UpperLeft, new Color(0.94f, 0.96f, 0.88f, 1f));
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject buttonObject = CreateUIObject("CloseKnowledgePanelButton", parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-54f, 40f);
            rect.sizeDelta = new Vector2(150f, 46f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.73f, 0.57f, 0.24f, 0.94f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Close);

            CreateText(buttonObject.transform, "Label", "\u5173\u95ed", 22, FontStyle.Bold,
                new Rect(0f, 0f, 150f, 46f), TextAnchor.MiddleCenter, Color.white);

            CreateText(parent, "CloseHint", "Esc / Space / Enter", 18, FontStyle.Normal,
                new Rect(890f, -588f, 230f, 26f), TextAnchor.MiddleRight, new Color(0.78f, 0.82f, 0.74f, 1f));
        }

        private Text CreateText(Transform parent, string name, string content, int fontSize, FontStyle fontStyle, Rect rect, TextAnchor alignment, Color color)
        {
            GameObject textObject = CreateUIObject(name, parent);
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(rect.x, rect.y);
            rectTransform.sizeDelta = new Vector2(rect.width, rect.height);

            Text text = textObject.AddComponent<Text>();
            text.text = content;
            text.font = ResolveFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(13, fontSize - 8);
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            return text;
        }

        private static Font ResolveFont()
        {
            Font font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 24);
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void ConfigureScaler(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private void SetPanelVisible(bool visible)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(visible);
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemObject.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        }
    }
}
