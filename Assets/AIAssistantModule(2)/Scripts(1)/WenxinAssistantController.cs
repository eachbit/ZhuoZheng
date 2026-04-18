using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WenxinAssistantController : MonoBehaviour
{
    private const string AppConversationCreateUrl = "https://qianfan.baidubce.com/v2/app/conversation";
    private const string AppConversationRunUrl = "https://qianfan.baidubce.com/v2/app/conversation/runs";
    private const string AppConversationUploadUrl = "https://qianfan.baidubce.com/v2/app/conversation/file/upload";
    private const string VisionChatUrl = "https://qianfan.baidubce.com/v2/chat/completions";
    private const string SpeechRecognitionUrl = "http://vop.baidu.com/server_api";

    private WenxinAssistantConfigData config;
    private string conversationId = "";

    private RectTransform iconRoot;
    private RectTransform iconCoreRect;
    private RectTransform panelRoot;
    private RectTransform headerRect;
    private RectTransform panelDragHandleRect;
    private RectTransform statusRect;
    private RectTransform conversationRect;
    private RectTransform imageInfoRect;
    private RectTransform questionRect;
    private RectTransform actionRowRect;
    private RectTransform previewRect;
    private LayoutElement conversationLayout;
    private LayoutElement imagePreviewLayout;
    private HorizontalLayoutGroup actionRowHorizontalLayout;
    private GridLayoutGroup actionRowGridLayout;
    private CanvasGroup panelCanvasGroup;
    private Canvas iconLayerCanvas;
    private GraphicRaycaster iconLayerRaycaster;
    private Image panelBackgroundImage;
    private Image panelDragHandleImage;
    private Outline panelOutlineEffect;
    private Shadow panelShadowEffect;
    private Image conversationBackgroundImage;
    private Text headerTitleText;
    private Text statusText;
    private Text conversationText;
    private Text imageInfoText;
    private InputField questionInput;
    private Image imagePreview;
    private Image iconGlowImage;
    private Image iconCoreImage;
    private Image iconWaveImage;
    private Image iconFacePlateImage;
    private Image iconAntennaImage;
    private Image iconAntennaTipImage;
    private Image iconMouthImage;
    private Image iconAvatarImage;
    private Image iconAvatarHighlightImage;
    private Image iconFreeformAvatarImage;
    private Image iconAvatarMaskImage;
    private Mask iconAvatarMask;
    private Outline iconAvatarOutline;
    private Shadow iconAvatarShadow;
    private Outline iconFreeformAvatarOutline;
    private Shadow iconFreeformAvatarShadow;
    private Outline iconCoreOutline;
    private Shadow iconCoreShadow;
    private RectTransform iconEyeLeft;
    private RectTransform iconEyeRight;
    private Button sendButton;
    private Button recordButton;
    private Button clearImageButton;
    private Button captureScreenButton;
    private Button iconButton;
    private Sprite generatedRoundSprite;

    [SerializeField] private bool useSceneHierarchyLayout;
    [SerializeField] private bool preserveSceneLayout = true;

    private byte[] loadedImageBytes;
    private bool isPanelVisible;
    private bool isBusy;
    private bool isRecording;
    private AudioClip recordingClip;
    private float iconAnimationTime;
    private float blinkTimer;
    private float panelSlideProgress;
    private bool usePhotoAvatar;
    private bool useFreeformAvatar;
    private int currentAvatarFrameIndex = -1;
    private readonly List<Sprite> avatarFrames = new List<Sprite>();
    private Vector2 roamingPosition;
    private Vector2 roamingVelocity;
    private float roamingPatternTimer;
    private float roamingPatternDuration;
    private float roamingPerimeterOffset;
    private float roamingSideX;
    private float roamingSideY;
    private int roamingPattern = -1;
    private int roamingPerimeterDirection = 1;
    private float roamingCanvasWidth = -1f;
    private float roamingCanvasHeight = -1f;
    private string activeMovementEdge = "right";
    private Vector2 idleAnchorPosition;
    private float idleAnchorCanvasWidth = -1f;
    private float idleAnchorCanvasHeight = -1f;
    private string activeIdleAnchor = "bottom_right";
    private bool isDraggingIcon;
    private bool isDraggingPanel;
    private Vector2 iconDragOffset;
    private Vector2 panelDragOffset;
    private bool iconPointerPressed;
    private bool panelPointerPressed;
    private Vector2 iconPointerPressPosition;
    private Vector2 panelPointerPressPosition;
    private bool hasUserPinnedIconPosition;
    private Vector2 userPinnedIconPosition;
    private bool hasUserPinnedPanelShownPosition;
    private Vector2 userPinnedPanelShownPosition;
    private string activePanelSnapEdge = "right";
    private float suppressIconToggleUntilTime = -10f;
    private float lastIconToggleTime = -10f;
    private bool uiBoundFromSceneHierarchy;
    private bool forceAutoLayoutPass;
    private Vector2 authoredPanelShownPosition;
    private Vector2 authoredIconBasePosition;
    private Vector2 authoredPanelSize;
    private Vector2 authoredQuestionSize;
    private Vector2 authoredActionRowPosition;
    private bool hasAuthoredPanelShownPosition;
    private bool hasAuthoredIconBasePosition;
    private bool hasAuthoredPanelSize;
    private bool hasAuthoredQuestionSize;
    private bool hasAuthoredActionRowPosition;

    private static readonly Vector2 PanelShownOffset = new Vector2(-16f, 0f);
    private static readonly Vector2 IconBasePosition = new Vector2(1824f, 68f);
    private const float PanelEdgePadding = 12f;
    private const float QuestionInputMaxLines = 10f;

    private void Awake()
    {
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }

        config = WenxinAssistantConfig.Load();
        EnsureEventSystem();
        EnsureUiBuilt();
        UpdateRecordButtonVisual(false);
        SetStatus(BuildReadyText());
        AppendConfiguredWelcomeText();
    }

    private void Update()
    {
        HandleQuickSend();
        HandlePointerDragFallbacks();
        UpdateFloatingIconAnimation();
        UpdatePanelAnimation();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureEventSystem();
        UpdateContextStatus();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            if (EventSystem.current.GetComponent<BaseInputModule>() == null)
            {
                EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
            }
            return;
        }

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private void BuildUi()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
        }

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        BuildFloatingIcon(font);

        GameObject panel = CreateUiObject("AssistantPanel", transform);
        panelRoot = panel.GetComponent<RectTransform>();
        panelRoot.anchorMin = new Vector2(1f, 0f);
        panelRoot.anchorMax = new Vector2(1f, 0f);
        panelRoot.pivot = new Vector2(1f, 0f);
        panelRoot.sizeDelta = new Vector2(320f, 420f);
        panelRoot.anchoredPosition = GetPanelHiddenPosition();

        panelBackgroundImage = panel.AddComponent<Image>();
        panelBackgroundImage.color = new Color(0.06f, 0.09f, 0.14f, 0.94f);
        panelOutlineEffect = panel.AddComponent<Outline>();
        panelOutlineEffect.effectColor = new Color(0.25f, 0.65f, 1f, 0.22f);
        panelOutlineEffect.effectDistance = new Vector2(1f, -1f);
        panelShadowEffect = panel.AddComponent<Shadow>();
        panelShadowEffect.effectColor = new Color(0f, 0f, 0f, 0.35f);
        panelShadowEffect.effectDistance = new Vector2(0f, -4f);

        panelCanvasGroup = panel.AddComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.blocksRaycasts = false;
        panelCanvasGroup.interactable = false;

        CreatePanelDragHandle(panel.transform);
        BuildHeader(font, panel.transform);
        BuildStatus(font, panel.transform);
        BuildConversation(font, panel.transform);
        BuildImageArea(font, panel.transform);
        BuildQuestionArea(font, panel.transform);
        BuildActionButtons(font, panel.transform);
        RefreshCompactLayout(false);

        isPanelVisible = false;
        panelSlideProgress = 0f;
    }

    private void EnsureUiBuilt()
    {
        ResetUiReferences();
        uiBoundFromSceneHierarchy = false;

        if (useSceneHierarchyLayout && HasEditableHierarchy() && TryBindExistingUiHierarchy())
        {
            uiBoundFromSceneHierarchy = true;
            InitializeSceneHierarchyRuntimeState();
            return;
        }

        BuildUi();
        CaptureAuthoredLayoutPositions();
    }

    private bool HasEditableHierarchy()
    {
        return FindChildRecursive(transform, "AssistantPanel") != null ||
               FindChildRecursive(transform, "AssistantIconLayer") != null;
    }

    private void InitializeSceneHierarchyRuntimeState()
    {
        CaptureAuthoredLayoutPositions();
        ResetMovementCaches();

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.interactable = false;
        }

        isPanelVisible = false;
        panelSlideProgress = 0f;

        if (panelRoot != null)
        {
            panelRoot.anchoredPosition = GetPanelHiddenPosition();
        }
    }

    private void CaptureAuthoredLayoutPositions()
    {
        if (panelRoot != null)
        {
            authoredPanelShownPosition = panelRoot.anchoredPosition;
            hasAuthoredPanelShownPosition = true;
            authoredPanelSize = panelRoot.sizeDelta;
            hasAuthoredPanelSize = true;
        }

        if (iconRoot != null)
        {
            authoredIconBasePosition = iconRoot.anchoredPosition;
            hasAuthoredIconBasePosition = true;
        }

        if (questionRect != null)
        {
            authoredQuestionSize = questionRect.sizeDelta;
            hasAuthoredQuestionSize = true;
        }

        if (actionRowRect != null)
        {
            authoredActionRowPosition = actionRowRect.anchoredPosition;
            hasAuthoredActionRowPosition = true;
        }
    }

    private void ResetMovementCaches()
    {
        roamingCanvasWidth = -1f;
        roamingCanvasHeight = -1f;
        idleAnchorCanvasWidth = -1f;
        idleAnchorCanvasHeight = -1f;
        roamingPattern = -1;
        roamingVelocity = Vector2.zero;
        idleAnchorPosition = Vector2.zero;
    }

    private bool TryBindExistingUiHierarchy()
    {
        panelRoot = FindRectTransformByName("AssistantPanel");
        iconRoot = FindRectTransformByName("AssistantIcon");
        questionInput = FindComponentByName<InputField>("QuestionInput");
        sendButton = FindComponentByName<Button>("SendButton");
        recordButton = FindComponentByName<Button>("RecordButton");
        clearImageButton = FindComponentByName<Button>("ClearImageButton");
        captureScreenButton = FindComponentByName<Button>("CaptureScreenButton");

        if (panelRoot == null || iconRoot == null || questionInput == null || sendButton == null)
        {
            return false;
        }

        if (FindRectTransformByName("PanelDragHandle") == null)
        {
            CreatePanelDragHandle(panelRoot.transform);
        }

        iconCoreRect = FindRectTransformByName("IconCore");
        headerRect = FindRectTransformByName("HeaderRow");
        panelDragHandleRect = FindRectTransformByName("PanelDragHandle");
        statusRect = FindRectTransformByName("StatusText");
        conversationRect = FindRectTransformByName("ConversationScroll");
        imageInfoRect = FindRectTransformByName("ImageInfo");
        questionRect = FindRectTransformByName("QuestionInput");
        actionRowRect = FindRectTransformByName("ActionRow");
        previewRect = FindRectTransformByName("ImagePreview");

        panelBackgroundImage = panelRoot.GetComponent<Image>();
        panelDragHandleImage = panelDragHandleRect != null ? EnsureComponent<Image>(panelDragHandleRect.gameObject) : null;
        panelOutlineEffect = panelRoot.GetComponent<Outline>();
        panelShadowEffect = panelRoot.GetComponent<Shadow>();
        panelCanvasGroup = EnsureComponent<CanvasGroup>(panelRoot.gameObject);

        conversationBackgroundImage = conversationRect != null ? conversationRect.GetComponent<Image>() : null;
        conversationLayout = conversationRect != null ? conversationRect.GetComponent<LayoutElement>() : null;
        imagePreviewLayout = previewRect != null ? previewRect.GetComponent<LayoutElement>() : null;

        headerTitleText = FindComponentByName<Text>("Title");
        statusText = FindComponentByName<Text>("StatusText");
        conversationText = FindComponentByName<Text>("ConversationText");
        imageInfoText = FindComponentByName<Text>("ImageInfo");
        imagePreview = FindComponentByName<Image>("ImagePreview");

        iconLayerCanvas = FindComponentByName<Canvas>("AssistantIconLayer");
        iconLayerRaycaster = FindComponentByName<GraphicRaycaster>("AssistantIconLayer");
        iconButton = EnsureComponent<Button>(iconRoot.gameObject);
        iconGlowImage = FindComponentByName<Image>("IconGlow");
        iconCoreImage = FindComponentByName<Image>("IconCore");
        iconWaveImage = FindComponentByName<Image>("IconWave");
        iconFacePlateImage = FindComponentByName<Image>("FacePlate");
        iconAntennaImage = FindComponentByName<Image>("Antenna");
        iconAntennaTipImage = FindComponentByName<Image>("AntennaTip");
        iconMouthImage = FindComponentByName<Image>("Mouth");
        iconAvatarImage = FindComponentByName<Image>("AvatarImage");
        iconAvatarHighlightImage = FindComponentByName<Image>("AvatarHighlight");
        iconFreeformAvatarImage = FindComponentByName<Image>("AvatarFreeform");
        iconAvatarMaskImage = FindComponentByName<Image>("AvatarMask");
        iconAvatarMask = FindComponentByName<Mask>("AvatarMask");
        iconAvatarOutline = iconAvatarImage != null ? iconAvatarImage.GetComponent<Outline>() : null;
        iconAvatarShadow = iconAvatarImage != null ? iconAvatarImage.GetComponent<Shadow>() : null;
        iconFreeformAvatarOutline = iconFreeformAvatarImage != null ? iconFreeformAvatarImage.GetComponent<Outline>() : null;
        iconFreeformAvatarShadow = iconFreeformAvatarImage != null ? iconFreeformAvatarImage.GetComponent<Shadow>() : null;
        iconCoreOutline = iconCoreImage != null ? iconCoreImage.GetComponent<Outline>() : null;
        iconCoreShadow = iconCoreImage != null ? iconCoreImage.GetComponent<Shadow>() : null;
        iconEyeLeft = FindRectTransformByName("EyeLeft");
        iconEyeRight = FindRectTransformByName("EyeRight");

        Image iconHitArea = EnsureComponent<Image>(iconRoot.gameObject);
        iconHitArea.color = new Color(1f, 1f, 1f, 0f);
        iconHitArea.raycastTarget = true;
        iconButton.targetGraphic = iconHitArea;
        iconButton.transition = Selectable.Transition.None;
        iconButton.onClick.RemoveAllListeners();

        WenxinAssistantIconInteractionProxy clickProxy = EnsureComponent<WenxinAssistantIconInteractionProxy>(iconRoot.gameObject);
        clickProxy.Initialize(null, BeginIconDrag, DragIcon, EndIconDrag);

        BindButton(captureScreenButton, CaptureCurrentScreen);
        BindButton(clearImageButton, ClearImage);
        BindButton(recordButton, ToggleRecording);
        BindButton(sendButton, SendCurrentQuestion);
        BindQuestionInputEvents();
        ApplyConfiguredQuestionPlaceholder();

        if (panelDragHandleRect != null)
        {
            panelDragHandleImage.color = new Color(1f, 1f, 1f, 0.001f);
            panelDragHandleImage.raycastTarget = true;

            WenxinAssistantPanelDragProxy dragProxy = EnsureComponent<WenxinAssistantPanelDragProxy>(panelDragHandleRect.gameObject);
            dragProxy.Initialize(BeginPanelDrag, DragPanel, EndPanelDrag);
        }

        DisableGraphicRaycast(iconGlowImage);
        DisableGraphicRaycast(iconWaveImage);
        DisableGraphicRaycast(iconCoreImage);
        DisableGraphicRaycast(iconFacePlateImage);
        DisableGraphicRaycast(iconAntennaImage);
        DisableGraphicRaycast(iconAntennaTipImage);
        DisableGraphicRaycast(iconMouthImage);
        DisableGraphicRaycast(iconAvatarImage);
        DisableGraphicRaycast(iconAvatarHighlightImage);
        DisableGraphicRaycast(iconFreeformAvatarImage);
        DisableGraphicRaycast(iconAvatarMaskImage);

        ApplyFloatingIconAvatar();
        return true;
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void BindQuestionInputEvents()
    {
        if (questionInput == null)
        {
            return;
        }

        questionInput.onValueChanged.RemoveListener(OnQuestionInputValueChanged);
        questionInput.onValueChanged.AddListener(OnQuestionInputValueChanged);
    }

    private void OnQuestionInputValueChanged(string value)
    {
        RefreshQuestionInputLayout();
    }

    private void DisableGraphicRaycast(Graphic graphic)
    {
        if (graphic != null)
        {
            graphic.raycastTarget = false;
        }
    }

    private T EnsureComponent<T>(GameObject target) where T : Component
    {
        if (target == null)
        {
            return null;
        }

        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = target.AddComponent<T>();
        }

        return component;
    }

    private void ResetUiReferences()
    {
        iconRoot = null;
        iconCoreRect = null;
        panelRoot = null;
        headerRect = null;
        panelDragHandleRect = null;
        statusRect = null;
        conversationRect = null;
        imageInfoRect = null;
        questionRect = null;
        actionRowRect = null;
        previewRect = null;
        conversationLayout = null;
        imagePreviewLayout = null;
        actionRowHorizontalLayout = null;
        actionRowGridLayout = null;
        panelCanvasGroup = null;
        iconLayerCanvas = null;
        iconLayerRaycaster = null;
        panelBackgroundImage = null;
        panelDragHandleImage = null;
        panelOutlineEffect = null;
        panelShadowEffect = null;
        conversationBackgroundImage = null;
        headerTitleText = null;
        statusText = null;
        conversationText = null;
        imageInfoText = null;
        questionInput = null;
        imagePreview = null;
        iconGlowImage = null;
        iconCoreImage = null;
        iconWaveImage = null;
        iconFacePlateImage = null;
        iconAntennaImage = null;
        iconAntennaTipImage = null;
        iconMouthImage = null;
        iconAvatarImage = null;
        iconAvatarHighlightImage = null;
        iconFreeformAvatarImage = null;
        iconAvatarMaskImage = null;
        iconAvatarMask = null;
        iconAvatarOutline = null;
        iconAvatarShadow = null;
        iconFreeformAvatarOutline = null;
        iconFreeformAvatarShadow = null;
        iconCoreOutline = null;
        iconCoreShadow = null;
        iconEyeLeft = null;
        iconEyeRight = null;
        sendButton = null;
        recordButton = null;
        clearImageButton = null;
        captureScreenButton = null;
        iconButton = null;
    }

    private RectTransform FindRectTransformByName(string objectName)
    {
        Transform target = FindChildRecursive(transform, objectName);
        return target != null ? target as RectTransform : null;
    }

    private T FindComponentByName<T>(string objectName) where T : Component
    {
        Transform target = FindChildRecursive(transform, objectName);
        return target != null ? target.GetComponent<T>() : null;
    }

    private Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == objectName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = FindChildRecursive(parent.GetChild(i), objectName);
            if (child != null)
            {
                return child;
            }
        }

        return null;
    }

    private void BuildFloatingIcon(Font font)
    {
        GameObject layer = CreateUiObject("AssistantIconLayer", transform);
        RectTransform layerRect = layer.GetComponent<RectTransform>();
        layerRect.anchorMin = Vector2.zero;
        layerRect.anchorMax = Vector2.one;
        layerRect.offsetMin = Vector2.zero;
        layerRect.offsetMax = Vector2.zero;
        iconLayerCanvas = layer.AddComponent<Canvas>();
        iconLayerCanvas.overrideSorting = true;
        iconLayerCanvas.sortingOrder = 5000;
        iconLayerRaycaster = layer.AddComponent<GraphicRaycaster>();

        GameObject icon = CreateUiObject("AssistantIcon", layer.transform);
        iconRoot = icon.GetComponent<RectTransform>();
        iconRoot.anchorMin = new Vector2(0f, 0f);
        iconRoot.anchorMax = new Vector2(0f, 0f);
        iconRoot.pivot = new Vector2(0.5f, 0.5f);
        iconRoot.sizeDelta = new Vector2(132f, 132f);
        iconRoot.anchoredPosition = IconBasePosition;

        Image iconHitArea = icon.AddComponent<Image>();
        iconHitArea.color = new Color(1f, 1f, 1f, 0f);
        iconButton = icon.AddComponent<Button>();
        iconButton.targetGraphic = iconHitArea;
        iconButton.transition = Selectable.Transition.None;
        WenxinAssistantIconInteractionProxy clickProxy = icon.AddComponent<WenxinAssistantIconInteractionProxy>();
        clickProxy.Initialize(null, BeginIconDrag, DragIcon, EndIconDrag);

        GameObject glow = CreateUiObject("IconGlow", icon.transform);
        RectTransform glowRect = glow.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.sizeDelta = new Vector2(96f, 96f);
        iconGlowImage = glow.AddComponent<Image>();
        iconGlowImage.color = new Color(0.23f, 0.77f, 1f, 0.22f);
        iconGlowImage.sprite = GetBuiltinRoundSprite();
        iconGlowImage.raycastTarget = false;

        GameObject wave = CreateUiObject("IconWave", icon.transform);
        RectTransform waveRect = wave.GetComponent<RectTransform>();
        waveRect.anchorMin = new Vector2(0.5f, 0.5f);
        waveRect.anchorMax = new Vector2(0.5f, 0.5f);
        waveRect.pivot = new Vector2(0.5f, 0.5f);
        waveRect.sizeDelta = new Vector2(88f, 88f);
        iconWaveImage = wave.AddComponent<Image>();
        iconWaveImage.color = new Color(0.34f, 0.86f, 1f, 0.16f);
        iconWaveImage.sprite = GetBuiltinRoundSprite();
        iconWaveImage.raycastTarget = false;

        GameObject core = CreateUiObject("IconCore", icon.transform);
        iconCoreRect = core.GetComponent<RectTransform>();
        iconCoreRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconCoreRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconCoreRect.pivot = new Vector2(0.5f, 0.5f);
        iconCoreRect.sizeDelta = new Vector2(72f, 72f);
        iconCoreImage = core.AddComponent<Image>();
        iconCoreImage.color = new Color(0.08f, 0.36f, 0.62f, 0.95f);
        iconCoreImage.sprite = GetBuiltinRoundSprite();
        iconCoreImage.raycastTarget = false;
        iconCoreOutline = core.AddComponent<Outline>();
        iconCoreOutline.effectColor = new Color(0.47f, 0.87f, 1f, 0.4f);
        iconCoreOutline.effectDistance = new Vector2(1f, -1f);
        iconCoreShadow = core.AddComponent<Shadow>();
        iconCoreShadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
        iconCoreShadow.effectDistance = new Vector2(0f, -4f);
        GameObject avatar = CreateUiObject("AvatarImage", core.transform);
        GameObject avatarMask = CreateUiObject("AvatarMask", core.transform);
        RectTransform avatarMaskRect = avatarMask.GetComponent<RectTransform>();
        avatarMaskRect.anchorMin = new Vector2(0.5f, 0.5f);
        avatarMaskRect.anchorMax = new Vector2(0.5f, 0.5f);
        avatarMaskRect.pivot = new Vector2(0.5f, 0.5f);
        avatarMaskRect.sizeDelta = new Vector2(64f, 64f);
        iconAvatarMaskImage = avatarMask.AddComponent<Image>();
        iconAvatarMaskImage.sprite = GetBuiltinRoundSprite();
        iconAvatarMaskImage.color = new Color(1f, 1f, 1f, 0f);
        iconAvatarMaskImage.raycastTarget = false;
        iconAvatarMask = avatarMask.AddComponent<Mask>();
        iconAvatarMask.showMaskGraphic = false;

        avatar.transform.SetParent(avatarMask.transform, false);
        RectTransform avatarRect = avatar.GetComponent<RectTransform>();
        avatarRect.anchorMin = new Vector2(0.5f, 0.5f);
        avatarRect.anchorMax = new Vector2(0.5f, 0.5f);
        avatarRect.pivot = new Vector2(0.5f, 0.5f);
        avatarRect.sizeDelta = new Vector2(64f, 64f);
        iconAvatarImage = avatar.AddComponent<Image>();
        iconAvatarImage.color = new Color(1f, 1f, 1f, 0f);
        iconAvatarImage.preserveAspect = true;
        iconAvatarImage.raycastTarget = false;
        iconAvatarOutline = avatar.AddComponent<Outline>();
        iconAvatarOutline.effectColor = new Color(0.45f, 0.88f, 1f, 0.6f);
        iconAvatarOutline.effectDistance = new Vector2(1f, -1f);
        iconAvatarShadow = avatar.AddComponent<Shadow>();
        iconAvatarShadow.effectColor = new Color(0f, 0.32f, 0.45f, 0.45f);
        iconAvatarShadow.effectDistance = new Vector2(0f, -2f);
        avatar.SetActive(false);

        GameObject avatarHighlight = CreateUiObject("AvatarHighlight", core.transform);
        avatarHighlight.transform.SetParent(avatarMask.transform, false);
        RectTransform avatarHighlightRect = avatarHighlight.GetComponent<RectTransform>();
        avatarHighlightRect.anchorMin = new Vector2(0.5f, 0.5f);
        avatarHighlightRect.anchorMax = new Vector2(0.5f, 0.5f);
        avatarHighlightRect.pivot = new Vector2(0.5f, 0.5f);
        avatarHighlightRect.sizeDelta = new Vector2(64f, 64f);
        iconAvatarHighlightImage = avatarHighlight.AddComponent<Image>();
        iconAvatarHighlightImage.color = new Color(1f, 1f, 1f, 0.08f);
        iconAvatarHighlightImage.sprite = GetBuiltinRoundSprite();
        iconAvatarHighlightImage.raycastTarget = false;
        avatarHighlight.SetActive(false);

        GameObject freeformAvatar = CreateUiObject("AvatarFreeform", icon.transform);
        RectTransform freeformRect = freeformAvatar.GetComponent<RectTransform>();
        freeformRect.anchorMin = new Vector2(0.5f, 0.5f);
        freeformRect.anchorMax = new Vector2(0.5f, 0.5f);
        freeformRect.pivot = new Vector2(0.5f, 0.5f);
        freeformRect.sizeDelta = new Vector2(112f, 112f);
        iconFreeformAvatarImage = freeformAvatar.AddComponent<Image>();
        iconFreeformAvatarImage.color = new Color(1f, 1f, 1f, 0f);
        iconFreeformAvatarImage.preserveAspect = true;
        iconFreeformAvatarImage.raycastTarget = false;
        iconFreeformAvatarOutline = freeformAvatar.AddComponent<Outline>();
        iconFreeformAvatarOutline.effectColor = new Color(0.45f, 0.88f, 1f, 0.5f);
        iconFreeformAvatarOutline.effectDistance = new Vector2(1f, -1f);
        iconFreeformAvatarShadow = freeformAvatar.AddComponent<Shadow>();
        iconFreeformAvatarShadow.effectColor = new Color(0f, 0.2f, 0.35f, 0.35f);
        iconFreeformAvatarShadow.effectDistance = new Vector2(0f, -2f);
        iconFreeformAvatarOutline.enabled = false;
        iconFreeformAvatarShadow.enabled = false;
        freeformAvatar.SetActive(false);

        GameObject face = CreateUiObject("IconFace", core.transform);
        RectTransform faceRect = face.GetComponent<RectTransform>();
        faceRect.anchorMin = new Vector2(0.5f, 0.5f);
        faceRect.anchorMax = new Vector2(0.5f, 0.5f);
        faceRect.pivot = new Vector2(0.5f, 0.5f);
        faceRect.sizeDelta = new Vector2(42f, 30f);

        GameObject antenna = CreateUiObject("Antenna", core.transform);
        iconAntennaImage = antenna.AddComponent<Image>();
        iconAntennaImage.color = new Color(0.62f, 0.93f, 1f, 0.95f);
        iconAntennaImage.raycastTarget = false;
        RectTransform antennaRect = antenna.GetComponent<RectTransform>();
        antennaRect.anchorMin = new Vector2(0.5f, 0.5f);
        antennaRect.anchorMax = new Vector2(0.5f, 0.5f);
        antennaRect.pivot = new Vector2(0.5f, 0.5f);
        antennaRect.sizeDelta = new Vector2(4f, 12f);
        antennaRect.anchoredPosition = new Vector2(0f, 20f);

        GameObject antennaTip = CreateUiObject("AntennaTip", core.transform);
        iconAntennaTipImage = antennaTip.AddComponent<Image>();
        iconAntennaTipImage.sprite = GetBuiltinRoundSprite();
        iconAntennaTipImage.color = new Color(0.47f, 0.93f, 1f, 0.95f);
        iconAntennaTipImage.raycastTarget = false;
        RectTransform antennaTipRect = antennaTip.GetComponent<RectTransform>();
        antennaTipRect.anchorMin = new Vector2(0.5f, 0.5f);
        antennaTipRect.anchorMax = new Vector2(0.5f, 0.5f);
        antennaTipRect.pivot = new Vector2(0.5f, 0.5f);
        antennaTipRect.sizeDelta = new Vector2(10f, 10f);
        antennaTipRect.anchoredPosition = new Vector2(0f, 27f);

        GameObject facePlate = CreateUiObject("FacePlate", face.transform);
        iconFacePlateImage = facePlate.AddComponent<Image>();
        iconFacePlateImage.color = new Color(0.86f, 0.97f, 1f, 0.18f);
        iconFacePlateImage.raycastTarget = false;
        RectTransform facePlateRect = facePlate.GetComponent<RectTransform>();
        facePlateRect.anchorMin = new Vector2(0.5f, 0.5f);
        facePlateRect.anchorMax = new Vector2(0.5f, 0.5f);
        facePlateRect.pivot = new Vector2(0.5f, 0.5f);
        facePlateRect.sizeDelta = new Vector2(40f, 26f);
        facePlateRect.anchoredPosition = Vector2.zero;

        iconEyeLeft = CreateEye("EyeLeft", face.transform, new Vector2(-10f, 5f));
        iconEyeRight = CreateEye("EyeRight", face.transform, new Vector2(10f, 5f));

        GameObject mouth = CreateUiObject("Mouth", face.transform);
        iconMouthImage = mouth.AddComponent<Image>();
        iconMouthImage.color = new Color(0.62f, 0.93f, 1f, 0.95f);
        iconMouthImage.raycastTarget = false;
        RectTransform mouthRect = mouth.GetComponent<RectTransform>();
        mouthRect.anchorMin = new Vector2(0.5f, 0.5f);
        mouthRect.anchorMax = new Vector2(0.5f, 0.5f);
        mouthRect.pivot = new Vector2(0.5f, 0.5f);
        mouthRect.sizeDelta = new Vector2(14f, 4f);
        mouthRect.anchoredPosition = new Vector2(0f, -8f);

        ApplyFloatingIconAvatar();
        icon.transform.SetAsLastSibling();
    }

    private void BuildHeader(Font font, Transform parent)
    {
        GameObject row = CreateUiObject("HeaderRow", parent);
        headerRect = row.GetComponent<RectTransform>();
        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 8;
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = true;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childForceExpandWidth = true;

        GameObject title = CreateText("Title", GetHeaderTitle(), font, 18, TextAnchor.MiddleLeft, parent: row.transform);
        headerTitleText = title.GetComponent<Text>();
        headerTitleText.color = new Color(0.95f, 0.98f, 1f, 1f);
        headerTitleText.raycastTarget = false;
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
    }

    private void BuildStatus(Font font, Transform parent)
    {
        GameObject status = CreateText("StatusText", "", font, 16, TextAnchor.MiddleLeft, parent);
        statusRect = status.GetComponent<RectTransform>();
        statusText = status.GetComponent<Text>();
        statusText.color = new Color(0.72f, 0.82f, 0.92f, 1f);
        statusText.raycastTarget = false;
        status.SetActive(ShouldShowStatusBar());
    }

    private void BuildConversation(Font font, Transform parent)
    {
        GameObject scrollRoot = CreateUiObject("ConversationScroll", parent);
        conversationRect = scrollRoot.GetComponent<RectTransform>();
        conversationBackgroundImage = scrollRoot.AddComponent<Image>();
        conversationBackgroundImage.color = new Color(1f, 1f, 1f, 0.08f);
        ScrollRect scrollRect = scrollRoot.AddComponent<ScrollRect>();
        conversationLayout = scrollRoot.AddComponent<LayoutElement>();
        conversationLayout.preferredHeight = 158f;

        GameObject viewport = CreateUiObject("Viewport", scrollRoot.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(10f, 10f);
        viewportRect.offsetMax = new Vector2(-10f, -10f);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = CreateText("ConversationText", "", font, 14, TextAnchor.UpperLeft, viewport.transform);
        conversationText = content.GetComponent<Text>();
        conversationText.horizontalOverflow = HorizontalWrapMode.Wrap;
        conversationText.verticalOverflow = VerticalWrapMode.Overflow;
        conversationText.supportRichText = true;
        conversationText.color = Color.white;
        conversationText.lineSpacing = 1.1f;

        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        ContentSizeFitter textFitter = content.AddComponent<ContentSizeFitter>();
        textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
    }

    private void BuildImageArea(Font font, Transform parent)
    {
        GameObject info = CreateText("ImageInfo", "未附加实验截图，可点“截取画面”。", font, 12, TextAnchor.MiddleLeft, parent);
        imageInfoRect = info.GetComponent<RectTransform>();
        imageInfoText = info.GetComponent<Text>();
        imageInfoText.color = new Color(0.83f, 0.86f, 0.92f, 0.9f);
        imageInfoText.horizontalOverflow = HorizontalWrapMode.Wrap;
        imageInfoText.verticalOverflow = VerticalWrapMode.Overflow;
        info.SetActive(ShouldShowImageInfo());

        GameObject preview = CreateUiObject("ImagePreview", parent);
        previewRect = preview.GetComponent<RectTransform>();
        imagePreview = preview.AddComponent<Image>();
        imagePreview.color = new Color(1f, 1f, 1f, 0.08f);
        imagePreview.preserveAspect = true;
        imagePreviewLayout = preview.AddComponent<LayoutElement>();
        imagePreviewLayout.preferredHeight = 0f;
        imagePreviewLayout.minHeight = 0f;
        imagePreviewLayout.flexibleHeight = 0f;
        preview.SetActive(false);
    }

    private void BuildQuestionArea(Font font, Transform parent)
    {
        GameObject inputObject = CreateInputField("QuestionInput", GetConfiguredQuestionPlaceholder(), font, parent);
        questionRect = inputObject.GetComponent<RectTransform>();
        questionInput = inputObject.GetComponent<InputField>();
        questionInput.lineType = InputField.LineType.MultiLineNewline;
        questionInput.textComponent.alignment = TextAnchor.UpperLeft;
        questionInput.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        questionInput.textComponent.verticalOverflow = VerticalWrapMode.Overflow;
        Text placeholder = questionInput.placeholder as Text;
        if (placeholder != null)
        {
            placeholder.alignment = TextAnchor.UpperLeft;
        }
        BindQuestionInputEvents();
    }

    private void AppendConfiguredWelcomeText()
    {
        string welcomeText = config != null ? config.assistantWelcomeText : string.Empty;
        if (string.IsNullOrEmpty(welcomeText) || string.IsNullOrEmpty(welcomeText.Trim()))
        {
            return;
        }

        AppendLine(GetAssistantSpeakerLabel(), welcomeText.Trim());
    }

    private string GetConfiguredQuestionPlaceholder()
    {
        return config != null ? config.questionInputPlaceholder ?? string.Empty : string.Empty;
    }

    private void ApplyConfiguredQuestionPlaceholder()
    {
        if (questionInput == null)
        {
            return;
        }

        Text placeholder = questionInput.placeholder as Text;
        if (placeholder != null)
        {
            placeholder.text = GetConfiguredQuestionPlaceholder();
        }
    }

    private void CreatePanelDragHandle(Transform parent)
    {
        GameObject handle = CreateUiObject("PanelDragHandle", parent);
        panelDragHandleRect = handle.GetComponent<RectTransform>();
        panelDragHandleImage = handle.AddComponent<Image>();
        panelDragHandleImage.color = new Color(1f, 1f, 1f, 0.001f);
        panelDragHandleImage.raycastTarget = true;

        WenxinAssistantPanelDragProxy dragProxy = handle.AddComponent<WenxinAssistantPanelDragProxy>();
        dragProxy.Initialize(BeginPanelDrag, DragPanel, EndPanelDrag);
    }

    private void BuildActionButtons(Font font, Transform parent)
    {
        GameObject row = CreateUiObject("ActionRow", parent);
        actionRowRect = row.GetComponent<RectTransform>();
        actionRowHorizontalLayout = null;
        actionRowGridLayout = null;

        captureScreenButton = CreateButton("CaptureScreenButton", "截取画面", font, row.transform, new Color(0.58f, 0.21f, 0.68f, 1f));
        clearImageButton = CreateButton("ClearImageButton", "清空图片", font, row.transform, new Color(0.34f, 0.39f, 0.48f, 1f));
        recordButton = CreateButton("RecordButton", "", font, row.transform, new Color(0.11f, 0.62f, 0.43f, 1f));
        sendButton = CreateButton("SendButton", "发送", font, row.transform, new Color(0.11f, 0.49f, 0.86f, 1f));

        SetButtonSize(captureScreenButton, 60f, 30f);
        SetButtonSize(clearImageButton, 60f, 30f);
        SetButtonSize(recordButton, 74f, 30f);
        SetButtonSize(sendButton, 60f, 30f);

        captureScreenButton.GetComponentInChildren<Text>().fontSize = 13;
        clearImageButton.GetComponentInChildren<Text>().fontSize = 13;
        recordButton.GetComponentInChildren<Text>().fontSize = 13;
        sendButton.GetComponentInChildren<Text>().fontSize = 13;

        captureScreenButton.onClick.AddListener(CaptureCurrentScreen);
        clearImageButton.onClick.AddListener(ClearImage);
        recordButton.onClick.AddListener(ToggleRecording);
        sendButton.onClick.AddListener(SendCurrentQuestion);

        UpdateRecordButtonVisual(false);
    }

    private GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;
        return go;
    }

    private GameObject CreateText(string name, string value, Font font, int fontSize, TextAnchor anchor, Transform parent)
    {
        GameObject go = CreateUiObject(name, parent);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.text = value;
        text.alignment = anchor;
        text.color = Color.white;
        return go;
    }

    private GameObject CreateInputField(string name, string placeholder, Font font, Transform parent)
    {
        GameObject root = CreateUiObject(name, parent);
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.09f);
        Outline outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.08f);
        outline.effectDistance = new Vector2(1f, -1f);

        InputField inputField = root.AddComponent<InputField>();
        inputField.lineType = InputField.LineType.SingleLine;

        GameObject textObject = CreateText("Text", "", font, 16, TextAnchor.MiddleLeft, root.transform);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 8f);
        textRect.offsetMax = new Vector2(-12f, -8f);

        GameObject placeholderObject = CreateText("Placeholder", placeholder, font, 16, TextAnchor.MiddleLeft, root.transform);
        RectTransform placeholderRect = placeholderObject.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(12f, 8f);
        placeholderRect.offsetMax = new Vector2(-12f, -8f);
        Text placeholderText = placeholderObject.GetComponent<Text>();
        placeholderText.color = new Color(1f, 1f, 1f, 0.35f);

        inputField.textComponent = textObject.GetComponent<Text>();
        inputField.placeholder = placeholderText;
        Navigation navigation = inputField.navigation;
        navigation.mode = Navigation.Mode.None;
        inputField.navigation = navigation;
        return root;
    }

    private Button CreateButton(string name, string label, Font font, Transform parent, Color color)
    {
        GameObject go = CreateUiObject(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.12f);
        outline.effectDistance = new Vector2(1f, -1f);
        Button button = go.AddComponent<Button>();

        GameObject labelObject = CreateText("Label", label, font, 16, TextAnchor.MiddleCenter, go.transform);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        return button;
    }

    private void SetButtonSize(Button button, float width, float height)
    {
        LayoutElement element = button.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = width;
        element.preferredHeight = height;
    }

    private void SetTopStretch(RectTransform rect, float top, float height)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(10f, -(top + height));
        rect.offsetMax = new Vector2(-10f, -top);
    }

    private void SetTopLeftRect(RectTransform rect, float left, float top, float width, float height)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(10f + left, -top);
    }

    private void SetTopCenterRect(RectTransform rect, float top, float width, float height)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(0f, -top);
    }

    private bool UseManualSceneLayout()
    {
        return useSceneHierarchyLayout && preserveSceneLayout && uiBoundFromSceneHierarchy;
    }

    private void RefreshCompactLayout(bool hasPreview)
    {
        if (UseManualSceneLayout() && !forceAutoLayoutPass)
        {
            if (statusRect != null)
            {
                statusRect.gameObject.SetActive(ShouldShowStatusBar());
            }

            if (imageInfoRect != null)
            {
                imageInfoRect.gameObject.SetActive(ShouldShowImageInfo());
            }

            if (headerTitleText != null)
            {
                headerTitleText.text = GetHeaderTitle();
            }

            return;
        }

        bool showHeader = ShouldShowAssistantName();
        bool showStatus = ShouldShowStatusBar();
        bool showImageInfo = ShouldShowImageInfo();
        string variant = GetUiVariant();
        float panelWidth = 320f;
        float topPadding = 10f;
        float sectionGap = 10f;
        float headerHeight = showHeader ? 24f : 0f;
        float headerGap = showHeader ? 6f : 0f;
        float statusHeight = showStatus ? 24f : 0f;
        float statusGap = showStatus ? 8f : 0f;
        float imageInfoHeight = showImageInfo ? 18f : 0f;
        float previewHeight = hasPreview ? 40f : 0f;
        float questionHeight = 48f;
        float actionHeight = 30f;
        float conversationHeight = hasPreview ? 132f : 180f;
        float actionButtonTextSize = 13f;
        float actionSpacing = 8f;
        float captureWidth = 60f;
        float clearWidth = 60f;
        float recordWidth = 74f;
        float sendWidth = 60f;
        int headerFontSize = 18;
        int statusFontSize = 16;
        int conversationFontSize = 14;
        int imageInfoFontSize = 12;
        int inputFontSize = 16;

        switch (variant)
        {
            case "chat_card":
                panelWidth = 404f;
                topPadding = 14f;
                sectionGap = 12f;
                headerHeight = showHeader ? 28f : 0f;
                headerGap = showHeader ? 8f : 0f;
                statusHeight = showStatus ? 24f : 0f;
                statusGap = showStatus ? 10f : 0f;
                imageInfoHeight = showImageInfo ? 18f : 0f;
                questionHeight = 56f;
                actionHeight = 34f;
                conversationHeight = hasPreview ? 178f : 232f;
                previewHeight = hasPreview ? 54f : 0f;
                actionButtonTextSize = 14f;
                actionSpacing = 10f;
                captureWidth = 72f;
                clearWidth = 72f;
                recordWidth = 92f;
                sendWidth = 72f;
                headerFontSize = 20;
                statusFontSize = 15;
                conversationFontSize = 15;
                imageInfoFontSize = 12;
                inputFontSize = 16;
                break;
            case "dense_console":
                panelWidth = 338f;
                topPadding = 8f;
                sectionGap = 6f;
                headerHeight = showHeader ? 18f : 0f;
                headerGap = showHeader ? 4f : 0f;
                statusHeight = showStatus ? 18f : 0f;
                statusGap = showStatus ? 4f : 0f;
                imageInfoHeight = showImageInfo ? 14f : 0f;
                questionHeight = 40f;
                actionHeight = 156f;
                conversationHeight = hasPreview ? 148f : 188f;
                previewHeight = hasPreview ? 36f : 0f;
                actionButtonTextSize = 11f;
                actionSpacing = 4f;
                captureWidth = 66f;
                clearWidth = 66f;
                recordWidth = 66f;
                sendWidth = 66f;
                headerFontSize = 14;
                statusFontSize = 12;
                conversationFontSize = 12;
                imageInfoFontSize = 10;
                inputFontSize = 13;
                break;
            case "slim_sidebar":
                panelWidth = 292f;
                topPadding = 8f;
                sectionGap = 8f;
                headerHeight = showHeader ? 22f : 0f;
                headerGap = showHeader ? 5f : 0f;
                statusHeight = showStatus ? 20f : 0f;
                statusGap = showStatus ? 6f : 0f;
                imageInfoHeight = showImageInfo ? 16f : 0f;
                questionHeight = 42f;
                actionHeight = 62f;
                conversationHeight = hasPreview ? 136f : 176f;
                previewHeight = hasPreview ? 40f : 0f;
                actionButtonTextSize = 12f;
                actionSpacing = 6f;
                captureWidth = 128f;
                clearWidth = 128f;
                recordWidth = 128f;
                sendWidth = 128f;
                headerFontSize = 17;
                statusFontSize = 13;
                conversationFontSize = 13;
                imageInfoFontSize = 11;
                inputFontSize = 14;
                break;
        }

        questionHeight = GetDesiredQuestionHeight(panelWidth - 20f, inputFontSize, questionHeight);

        float panelHeight;
        float contentWidth = panelWidth - 20f;
        float currentTop = topPadding;

        if (variant == "chat_card")
        {
            SetTopStretch(headerRect, currentTop, headerHeight);
            currentTop += headerHeight + headerGap;
            SetTopStretch(statusRect, currentTop, statusHeight);
            currentTop += statusHeight + statusGap;
            SetTopStretch(questionRect, currentTop, contentWidth > 0f ? questionHeight : 0f);
            currentTop += questionHeight + 8f;
            SetTopStretch(actionRowRect, currentTop, actionHeight);
            currentTop += actionHeight + 10f;
            SetTopStretch(conversationRect, currentTop, conversationHeight);
            currentTop += conversationHeight;

            if (showImageInfo)
            {
                currentTop += 8f;
                SetTopStretch(imageInfoRect, currentTop, imageInfoHeight);
                currentTop += imageInfoHeight;
            }
            else
            {
                SetTopStretch(imageInfoRect, currentTop, 0f);
            }

            if (hasPreview)
            {
                currentTop += 8f;
                SetTopCenterRect(previewRect, currentTop, Mathf.Min(110f, contentWidth), previewHeight);
                currentTop += previewHeight;
            }
            else
            {
                SetTopCenterRect(previewRect, currentTop, 0f, 0f);
            }

            panelHeight = currentTop + topPadding;
        }
        else if (variant == "dense_console")
        {
            float railWidth = 66f;
            float leftWidth = contentWidth - railWidth - 8f;

            SetTopStretch(headerRect, currentTop, headerHeight);
            currentTop += headerHeight + headerGap;
            SetTopStretch(statusRect, currentTop, statusHeight);
            currentTop += statusHeight + statusGap;

            SetTopLeftRect(conversationRect, 0f, currentTop, leftWidth, conversationHeight);
            SetTopLeftRect(actionRowRect, leftWidth + 8f, currentTop, railWidth, actionHeight);
            currentTop += conversationHeight + 6f;

            if (hasPreview)
            {
                SetTopLeftRect(previewRect, 0f, currentTop, leftWidth, previewHeight);
                currentTop += previewHeight + 4f;
            }
            else
            {
                SetTopLeftRect(previewRect, 0f, currentTop, 0f, 0f);
            }

            if (showImageInfo)
            {
                SetTopLeftRect(imageInfoRect, 0f, currentTop, leftWidth, imageInfoHeight);
                currentTop += imageInfoHeight + 6f;
            }
            else
            {
                SetTopLeftRect(imageInfoRect, 0f, currentTop, 0f, 0f);
            }

            SetTopStretch(questionRect, currentTop, questionHeight);
            currentTop += questionHeight;
            panelHeight = currentTop + topPadding;
        }
        else if (variant == "slim_sidebar")
        {
            SetTopStretch(headerRect, currentTop, headerHeight);
            currentTop += headerHeight + headerGap;
            SetTopStretch(statusRect, currentTop, statusHeight);
            currentTop += statusHeight + statusGap;
            SetTopStretch(actionRowRect, currentTop, actionHeight);
            currentTop += actionHeight + 8f;
            SetTopStretch(questionRect, currentTop, questionHeight);
            currentTop += questionHeight + 8f;
            SetTopStretch(conversationRect, currentTop, conversationHeight);
            currentTop += conversationHeight;

            if (hasPreview)
            {
                currentTop += 8f;
                SetTopStretch(previewRect, currentTop, previewHeight);
                currentTop += previewHeight;
            }
            else
            {
                SetTopStretch(previewRect, currentTop, 0f);
            }

            if (showImageInfo)
            {
                currentTop += 6f;
                SetTopStretch(imageInfoRect, currentTop, imageInfoHeight);
                currentTop += imageInfoHeight;
            }
            else
            {
                SetTopStretch(imageInfoRect, currentTop, 0f);
            }

            panelHeight = currentTop + topPadding;
        }
        else
        {
            float statusTop = topPadding + headerHeight + headerGap;
            float conversationTop = statusTop + statusHeight + statusGap;
            float imageInfoTop = conversationTop + conversationHeight + (showImageInfo ? 8f : 0f);
            float previewTop = imageInfoTop + imageInfoHeight + (showImageInfo ? 8f : 0f);
            float questionTop = previewTop + previewHeight + sectionGap;
            float actionTop = questionTop + questionHeight + sectionGap;

            SetTopStretch(headerRect, topPadding, headerHeight);
            SetTopStretch(statusRect, statusTop, statusHeight);
            SetTopStretch(conversationRect, conversationTop, conversationHeight);
            SetTopStretch(imageInfoRect, imageInfoTop, imageInfoHeight);
            SetTopStretch(previewRect, previewTop, previewHeight);
            SetTopStretch(questionRect, questionTop, questionHeight);
            SetTopStretch(actionRowRect, actionTop, actionHeight);
            panelHeight = actionTop + actionHeight + topPadding;
        }

        if (panelRoot != null)
        {
            panelRoot.sizeDelta = new Vector2(panelWidth, panelHeight);
            if (!isPanelVisible && panelSlideProgress <= 0.001f)
            {
                panelRoot.anchoredPosition = GetPanelHiddenPosition();
            }
        }

        if (panelDragHandleRect != null)
        {
            float dragHeight = Mathf.Max(22f, headerHeight > 0f ? headerHeight + 4f : 26f);
            SetTopStretch(panelDragHandleRect, 0f, dragHeight);
            panelDragHandleRect.SetAsLastSibling();
        }

        if (statusRect != null)
        {
            statusRect.gameObject.SetActive(showStatus);
        }

        if (imageInfoRect != null)
        {
            imageInfoRect.gameObject.SetActive(showImageInfo);
        }

        if (headerTitleText != null)
        {
            headerTitleText.text = GetHeaderTitle();
            headerTitleText.fontSize = headerFontSize;
        }

        if (statusText != null)
        {
            statusText.fontSize = statusFontSize;
        }

        if (conversationText != null)
        {
            conversationText.fontSize = conversationFontSize;
        }

        if (imageInfoText != null)
        {
            imageInfoText.fontSize = imageInfoFontSize;
        }

        ApplyUiVariantStyle(variant, inputFontSize, actionButtonTextSize, actionSpacing, captureWidth, clearWidth, recordWidth, sendWidth);
    }

    private void TogglePanelVisibility()
    {
        if (Time.unscaledTime < suppressIconToggleUntilTime || isDraggingIcon)
        {
            return;
        }

        if (Time.unscaledTime - lastIconToggleTime < 0.12f)
        {
            return;
        }

        lastIconToggleTime = Time.unscaledTime;
        isPanelVisible = !isPanelVisible;
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.blocksRaycasts = isPanelVisible;
            panelCanvasGroup.interactable = isPanelVisible;
        }
    }

    private void HandlePointerDragFallbacks()
    {
        Vector2 mousePosition = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            if (iconRoot != null && RectTransformUtility.RectangleContainsScreenPoint(iconRoot, mousePosition, null))
            {
                iconPointerPressed = true;
                iconPointerPressPosition = mousePosition;
            }

            if (IsPointerOverPanelDragSurface(mousePosition))
            {
                panelPointerPressed = true;
                panelPointerPressPosition = mousePosition;
            }
        }

        if (iconPointerPressed && Input.GetMouseButton(0))
        {
            if (!isDraggingIcon && (mousePosition - iconPointerPressPosition).sqrMagnitude > 25f)
            {
                BeginIconDragAtScreen(mousePosition);
            }

            if (isDraggingIcon)
            {
                DragIconAtScreen(mousePosition);
            }
        }

        if (panelPointerPressed && Input.GetMouseButton(0))
        {
            if (!isDraggingPanel && (mousePosition - panelPointerPressPosition).sqrMagnitude > 25f)
            {
                BeginPanelDragAtScreen(mousePosition);
            }

            if (isDraggingPanel)
            {
                DragPanelAtScreen(mousePosition);
            }
        }

        if (!Input.GetMouseButtonUp(0))
        {
            return;
        }

        if (isDraggingIcon)
        {
            EndIconDragAtScreen(mousePosition);
        }
        else if (iconPointerPressed &&
                 iconRoot != null &&
                 RectTransformUtility.RectangleContainsScreenPoint(iconRoot, mousePosition, null) &&
                 Time.unscaledTime >= suppressIconToggleUntilTime)
        {
            TogglePanelVisibility();
        }

        if (isDraggingPanel)
        {
            EndPanelDragAtScreen(mousePosition);
        }

        iconPointerPressed = false;
        panelPointerPressed = false;
    }

    private bool IsPointerOverPanelDragSurface(Vector2 screenPosition)
    {
        if (!isPanelVisible || panelRoot == null)
        {
            return false;
        }

        if (questionRect != null && RectTransformUtility.RectangleContainsScreenPoint(questionRect, screenPosition, null))
        {
            return false;
        }

        if (actionRowRect != null && RectTransformUtility.RectangleContainsScreenPoint(actionRowRect, screenPosition, null))
        {
            return false;
        }

        if (panelDragHandleRect != null && RectTransformUtility.RectangleContainsScreenPoint(panelDragHandleRect, screenPosition, null))
        {
            return true;
        }

        if (conversationRect != null && RectTransformUtility.RectangleContainsScreenPoint(conversationRect, screenPosition, null))
        {
            return true;
        }

        if (imageInfoRect != null && RectTransformUtility.RectangleContainsScreenPoint(imageInfoRect, screenPosition, null))
        {
            return true;
        }

        if (previewRect != null && RectTransformUtility.RectangleContainsScreenPoint(previewRect, screenPosition, null))
        {
            return true;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(panelRoot, screenPosition, null);
    }

    private void BeginIconDrag(PointerEventData eventData)
    {
        BeginIconDragAtScreen(eventData.position);
    }

    private void DragIcon(PointerEventData eventData)
    {
        DragIconAtScreen(eventData.position);
    }

    private void EndIconDrag(PointerEventData eventData)
    {
        EndIconDragAtScreen(eventData.position);
    }

    private void HandleQuickSend()
    {
        if (!isPanelVisible || questionInput == null || !questionInput.isFocused || isBusy)
        {
            return;
        }

        bool commandPressed = Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
        bool controlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool submitPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

        if (submitPressed && (commandPressed || controlPressed))
        {
            SendCurrentQuestion();
        }
    }

    private void BeginPanelDrag(PointerEventData eventData)
    {
        BeginPanelDragAtScreen(eventData.position);
    }

    private void DragPanel(PointerEventData eventData)
    {
        DragPanelAtScreen(eventData.position);
    }

    private void EndPanelDrag(PointerEventData eventData)
    {
        EndPanelDragAtScreen(eventData.position);
    }

    private void BeginIconDragAtScreen(Vector2 screenPosition)
    {
        if (iconRoot == null)
        {
            return;
        }

        Vector2 anchoredPosition;
        if (!TryGetIconAnchoredPositionFromScreen(screenPosition, out anchoredPosition))
        {
            return;
        }

        iconDragOffset = iconRoot.anchoredPosition - anchoredPosition;
        isDraggingIcon = true;
        suppressIconToggleUntilTime = Time.unscaledTime + 0.22f;
    }

    private void DragIconAtScreen(Vector2 screenPosition)
    {
        if (iconRoot == null)
        {
            return;
        }

        Vector2 anchoredPosition;
        if (!TryGetIconAnchoredPositionFromScreen(screenPosition, out anchoredPosition))
        {
            return;
        }

        ApplyPinnedIconPosition(ClampIconPositionToCanvas(anchoredPosition + iconDragOffset));
    }

    private void EndIconDragAtScreen(Vector2 screenPosition)
    {
        if (iconRoot == null)
        {
            return;
        }

        Vector2 anchoredPosition;
        if (!TryGetIconAnchoredPositionFromScreen(screenPosition, out anchoredPosition))
        {
            anchoredPosition = iconRoot.anchoredPosition;
        }

        string snappedEdge;
        Vector2 snappedPosition = GetNearestEdgeSnapPosition(ClampIconPositionToCanvas(anchoredPosition + iconDragOffset), out snappedEdge);
        ApplyPinnedIconPosition(snappedPosition);
        activeMovementEdge = snappedEdge;
        isDraggingIcon = false;
        suppressIconToggleUntilTime = Time.unscaledTime + 0.22f;
    }

    private void BeginPanelDragAtScreen(Vector2 screenPosition)
    {
        if (panelRoot == null || !isPanelVisible)
        {
            return;
        }

        Vector2 anchoredPosition;
        if (!TryGetPanelAnchoredPositionFromScreen(screenPosition, out anchoredPosition))
        {
            return;
        }

        panelDragOffset = panelRoot.anchoredPosition - anchoredPosition;
        isDraggingPanel = true;
    }

    private void DragPanelAtScreen(Vector2 screenPosition)
    {
        if (panelRoot == null || !isPanelVisible)
        {
            return;
        }

        Vector2 anchoredPosition;
        if (!TryGetPanelAnchoredPositionFromScreen(screenPosition, out anchoredPosition))
        {
            return;
        }

        panelRoot.anchoredPosition = ClampPanelShownPosition(anchoredPosition + panelDragOffset);
    }

    private void EndPanelDragAtScreen(Vector2 screenPosition)
    {
        if (panelRoot == null)
        {
            return;
        }

        Vector2 anchoredPosition;
        if (!TryGetPanelAnchoredPositionFromScreen(screenPosition, out anchoredPosition))
        {
            anchoredPosition = panelRoot.anchoredPosition;
        }

        string snappedEdge;
        Vector2 snappedPosition = GetNearestPanelSnapPosition(ClampPanelShownPosition(anchoredPosition + panelDragOffset), out snappedEdge);
        activePanelSnapEdge = snappedEdge;
        hasUserPinnedPanelShownPosition = true;
        userPinnedPanelShownPosition = snappedPosition;
        authoredPanelShownPosition = snappedPosition;
        hasAuthoredPanelShownPosition = true;
        panelRoot.anchoredPosition = snappedPosition;
        isDraggingPanel = false;
    }

    private void CaptureCurrentScreen()
    {
        if (isBusy)
        {
            return;
        }

        StartCoroutine(CaptureCurrentScreenCoroutine());
    }

    private IEnumerator CaptureCurrentScreenCoroutine()
    {
        SetStatus("正在截取当前运行画面...");
        yield return new WaitForEndOfFrame();

        Rect captureRect = GetExperimentCaptureRect();
        int width = Mathf.RoundToInt(captureRect.width);
        int height = Mathf.RoundToInt(captureRect.height);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(captureRect, 0, 0, false);
        texture.Apply(false, false);

        byte[] pngBytes = texture.EncodeToPNG();
        if (pngBytes == null || pngBytes.Length == 0)
        {
            SetStatus("截屏失败，请重试。");
            Destroy(texture);
            yield break;
        }

        loadedImageBytes = pngBytes;

        if (imagePreview != null)
        {
            imagePreview.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            imagePreview.color = Color.white;
        }

        UpdateImageInfo("已截取实验主画面，不包含右侧助手面板。");
        SetPreviewVisible(true);
        SetStatus("实验主画面已截取，可直接结合问题发送。");
    }

    private void ClearImage()
    {
        loadedImageBytes = null;
        if (imagePreview != null)
        {
            imagePreview.sprite = null;
            imagePreview.color = new Color(1f, 1f, 1f, 0.08f);
            imagePreview.gameObject.SetActive(false);
        }
        SetPreviewVisible(false);
        UpdateImageInfo("当前未附加实验截图。可点击“截取画面”。");
        SetStatus(BuildReadyText());
    }

    private void ToggleRecording()
    {
        if (isBusy)
        {
            return;
        }

        if (isRecording)
        {
            StopRecordingAndRecognize();
            return;
        }

        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            SetStatus("没有检测到麦克风设备。");
            return;
        }

        recordingClip = Microphone.Start(null, false, 20, config.speechSampleRate);
        isRecording = true;
        UpdateRecordButtonVisual(true);
        SetStatus("正在录音，再点一次按钮结束并识别。");
    }

    private void StopRecordingAndRecognize()
    {
        if (!isRecording)
        {
            return;
        }

        int position = Microphone.GetPosition(null);
        Microphone.End(null);
        isRecording = false;
        UpdateRecordButtonVisual(false);

        if (recordingClip == null || position <= 0)
        {
            SetStatus("录音内容为空，请重试。");
            return;
        }

        float[] samples = new float[position * recordingClip.channels];
        recordingClip.GetData(samples, 0);
        byte[] wavData = WavUtility.FromAudioClip(samples, recordingClip.channels, recordingClip.frequency);
        StartCoroutine(RecognizeSpeechCoroutine(wavData));
    }

    private void SendCurrentQuestion()
    {
        if (isBusy)
        {
            return;
        }

        string question = questionInput != null ? questionInput.text.Trim() : "";
        if (string.IsNullOrEmpty(question) && loadedImageBytes == null)
        {
            SetStatus("请输入问题，或者先加载图片。");
            return;
        }

        StartCoroutine(SendQuestionCoroutine(question));
    }

    private IEnumerator SendQuestionCoroutine(string question)
    {
        if (!EnsureConfigured())
        {
            yield break;
        }

        isBusy = true;
        SetButtonsInteractable(false);
        SetStatus("正在连接文心智能体...");

        if (loadedImageBytes != null)
        {
            yield return StartCoroutine(SendImageQuestionCoroutine(question));
            FinishBusy();
            yield break;
        }

        if (string.IsNullOrEmpty(conversationId))
        {
            yield return StartCoroutine(CreateConversationCoroutine());
            if (string.IsNullOrEmpty(conversationId))
            {
                FinishBusy();
                yield break;
            }
        }

        AppConversationRunRequest request = BuildConversationRunRequest(question);
        string requestJson = JsonUtility.ToJson(request);

        using (UnityWebRequest webRequest = new UnityWebRequest(AppConversationRunUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("X-Appbuilder-Authorization", "Bearer " + config.appBuilderApiKey);

            yield return webRequest.SendWebRequest();

            if (HasRequestError(webRequest))
            {
                SetStatus("专属智能体调用失败: " + webRequest.error + "\n" + webRequest.downloadHandler.text);
                FinishBusy();
                yield break;
            }

            AppConversationRunResponse response = JsonUtility.FromJson<AppConversationRunResponse>(webRequest.downloadHandler.text);
            string answer = response != null ? response.answer : "";
            if (string.IsNullOrEmpty(answer))
            {
                answer = "没有拿到可展示的回答，请检查接口返回格式。";
            }

            if (response != null && !string.IsNullOrEmpty(response.conversation_id))
            {
                conversationId = response.conversation_id;
            }

            if (!string.IsNullOrEmpty(question))
            {
                AppendLine("你", question);
                questionInput.text = "";
                RefreshQuestionInputLayout();
            }

            AppendLine(GetAssistantSpeakerLabel(), answer);
            SetStatus(BuildReadyText());
        }

        FinishBusy();
    }

    private IEnumerator SendImageQuestionCoroutine(string question)
    {
        SetStatus("正在分析当前图片...");

        if (loadedImageBytes == null || loadedImageBytes.Length == 0)
        {
            SetStatus("图片为空，无法进行图像分析。");
            yield break;
        }

        string mimeType = DetectImageMimeType(loadedImageBytes);
        string dataUrl = "data:" + mimeType + ";base64," + Convert.ToBase64String(loadedImageBytes);
        string prompt = BuildVisionPrompt(question);
        VisionRequest request = BuildVisionRequest(dataUrl, prompt);
        string requestJson = JsonUtility.ToJson(request);

        using (UnityWebRequest webRequest = new UnityWebRequest(VisionChatUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", "Bearer " + config.appBuilderApiKey);

            yield return webRequest.SendWebRequest();

            if (HasRequestError(webRequest))
            {
                SetStatus("图像分析失败: " + webRequest.error + "\n" + webRequest.downloadHandler.text);
                yield break;
            }

            VisionResponse response = JsonUtility.FromJson<VisionResponse>(webRequest.downloadHandler.text);
            string answer = ExtractVisionAnswer(response);
            if (string.IsNullOrEmpty(answer))
            {
                answer = "没有拿到图像分析结果，请检查视觉模型返回格式。";
            }

            AppendLine("你", string.IsNullOrEmpty(question) ? "[发送了一张图片]" : question + "\n[附带当前运行画面]");
            if (questionInput != null)
            {
                questionInput.text = "";
                RefreshQuestionInputLayout();
            }
            AppendLine(GetAssistantSpeakerLabel(), answer);
            SetStatus("已使用视觉理解模型分析图片。");
        }
    }

    private IEnumerator CreateConversationCoroutine()
    {
        AppConversationCreateRequest request = new AppConversationCreateRequest();
        request.app_id = config.appId;
        string json = JsonUtility.ToJson(request);

        using (UnityWebRequest webRequest = new UnityWebRequest(AppConversationCreateUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("X-Appbuilder-Authorization", "Bearer " + config.appBuilderApiKey);

            yield return webRequest.SendWebRequest();

            if (HasRequestError(webRequest))
            {
                SetStatus("创建专属智能体会话失败: " + webRequest.error + "\n" + webRequest.downloadHandler.text);
                yield break;
            }

            AppConversationCreateResponse response = JsonUtility.FromJson<AppConversationCreateResponse>(webRequest.downloadHandler.text);
            if (response != null)
            {
                conversationId = response.conversation_id;
            }

            if (string.IsNullOrEmpty(conversationId))
            {
                SetStatus("创建专属智能体会话失败: 未返回 conversation_id。");
            }
        }
    }

    private IEnumerator RecognizeSpeechCoroutine(byte[] wavData)
    {
        if (!EnsureConfigured())
        {
            yield break;
        }

        isBusy = true;
        SetButtonsInteractable(false);
        SetStatus("正在识别语音...");

        string speechBase64 = Convert.ToBase64String(wavData);
        SpeechRecognitionRequest request = new SpeechRecognitionRequest();
        request.format = config.speechFormat;
        request.rate = config.speechSampleRate;
        request.dev_pid = config.speechDevPid;
        request.channel = 1;
        request.cuid = SystemInfo.deviceUniqueIdentifier;
        request.len = wavData.Length;
        request.speech = speechBase64;

        string json = JsonUtility.ToJson(request);

        using (UnityWebRequest webRequest = new UnityWebRequest(SpeechRecognitionUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", "Bearer " + GetSpeechApiKey());

            yield return webRequest.SendWebRequest();

            if (HasRequestError(webRequest))
            {
                SetStatus("语音识别失败: " + webRequest.error + "\n" + webRequest.downloadHandler.text);
                FinishBusy();
                yield break;
            }

            SpeechRecognitionResponse response = JsonUtility.FromJson<SpeechRecognitionResponse>(webRequest.downloadHandler.text);
            string result = "";
            if (response != null && response.result != null && response.result.Length > 0)
            {
                result = response.result[0];
            }

            if (string.IsNullOrEmpty(result))
            {
                SetStatus("语音识别成功，但没有返回文本。");
                FinishBusy();
                yield break;
            }

            questionInput.text = result.Trim();
            RefreshQuestionInputLayout();
            SetStatus("语音识别完成，已填入输入框。");
        }

        FinishBusy();
    }

    private AppConversationRunRequest BuildConversationRunRequest(string question)
    {
        AppConversationRunRequest request = new AppConversationRunRequest();
        request.app_id = config.appId;
        request.conversation_id = conversationId;
        request.stream = false;
        request.query = string.IsNullOrEmpty(question) ? "请回答这个虚拟仿真实验中的相关问题。" : question;
        return request;
    }

    private string BuildVisionPrompt(string question)
    {
        string projectName = GetConfiguredProjectName();
        string typeName = GetConfiguredExperimentType();
        string basePrompt = "你正在分析一个" + projectName + "的当前运行画面。当前场景是" +
                            SceneManager.GetActiveScene().name +
                            "，实验类型是" + typeName +
                            "。请先识别图中可见的题目、病菌、器官、按钮、提示信息或病理相关元素，再结合用户问题作答。";

        if (string.IsNullOrEmpty(question))
        {
            return basePrompt + "请直接描述这张画面里最重要的实验内容和可操作项。";
        }

        return basePrompt + "用户问题：" + question;
    }

    private VisionRequest BuildVisionRequest(string dataUrl, string prompt)
    {
        VisionRequest request = new VisionRequest();
        request.model = config.visionModel;
        request.messages = new[]
        {
            new VisionMessage
            {
                role = "user",
                content = new[]
                {
                    new VisionContentItem
                    {
                        type = "text",
                        text = prompt
                    },
                    new VisionContentItem
                    {
                        type = "image_url",
                        image_url = new VisionImageUrl
                        {
                            url = dataUrl
                        }
                    }
                }
            }
        };
        return request;
    }

    private string ExtractVisionAnswer(VisionResponse response)
    {
        if (response == null || response.choices == null || response.choices.Length == 0)
        {
            return "";
        }

        VisionChoice choice = response.choices[0];
        if (choice == null || choice.message == null)
        {
            return "";
        }

        return choice.message.content ?? "";
    }

    private string DetectImageMimeType(byte[] bytes)
    {
        if (bytes != null && bytes.Length >= 8)
        {
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            {
                return "image/png";
            }

            if (bytes[0] == 0xFF && bytes[1] == 0xD8)
            {
                return "image/jpeg";
            }

            if (bytes[0] == 0x42 && bytes[1] == 0x4D)
            {
                return "image/bmp";
            }
        }

        return "image/png";
    }

    private Rect GetExperimentCaptureRect()
    {
        float captureWidth = Screen.width;
        if (panelRoot != null && isPanelVisible)
        {
            Vector3[] panelCorners = new Vector3[4];
            panelRoot.GetWorldCorners(panelCorners);
            captureWidth = Mathf.Clamp(panelCorners[0].x - 12f, 100f, Screen.width);
        }
        else if (iconRoot != null)
        {
            Vector3[] iconCorners = new Vector3[4];
            iconRoot.GetWorldCorners(iconCorners);
            if (iconCorners[0].x > Screen.width * 0.7f)
            {
                captureWidth = Mathf.Clamp(iconCorners[0].x - 12f, 100f, Screen.width);
            }
        }

        return new Rect(0f, 0f, captureWidth, Screen.height);
    }

    private void UpdateImageInfo(string message)
    {
        if (imageInfoText != null)
        {
            imageInfoText.text = message;
        }
    }

    private void UpdateFloatingIconAnimation()
    {
        if (iconRoot == null)
        {
            return;
        }

        iconAnimationTime += Time.unscaledDeltaTime;
        float bob = Mathf.Sin(iconAnimationTime * 1.6f) * 14f;
        float pulse = 1f + Mathf.Sin(iconAnimationTime * 2.2f) * 0.06f;
        if (!isDraggingIcon)
        {
            Vector2 iconPosition = GetAnimatedIconPosition();
            if (IsFloatMovementMode())
            {
                iconPosition.y += bob;
            }
            iconRoot.anchoredPosition = iconPosition;
        }
        iconRoot.SetAsLastSibling();

        UpdateAvatarFrameAnimation();

        if (iconCoreImage != null)
        {
            if (usePhotoAvatar)
            {
                iconCoreImage.rectTransform.localScale = Vector3.one;
                iconCoreImage.color = new Color(0.08f, 0.36f, 0.62f, 0f);
            }
            else
            {
                iconCoreImage.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
                float pulseLerp = Mathf.Clamp01((pulse - 0.94f) / 0.12f);
                iconCoreImage.color = Color.Lerp(new Color(0.08f, 0.36f, 0.62f, 0.95f), new Color(0.11f, 0.48f, 0.75f, 0.98f), pulseLerp);
            }
        }

        if (iconGlowImage != null)
        {
            float glowScale = 1.08f + Mathf.Sin(iconAnimationTime * 2.2f) * 0.12f;
            iconGlowImage.rectTransform.localScale = new Vector3(glowScale, glowScale, 1f);
            Color glowColor = iconGlowImage.color;
            glowColor.a = isPanelVisible ? 0.14f : 0.26f;
            iconGlowImage.color = glowColor;
        }

        if (iconWaveImage != null)
        {
            float waveScale = 1.02f + Mathf.Repeat(iconAnimationTime * 0.55f, 1f) * 0.42f;
            iconWaveImage.rectTransform.localScale = new Vector3(waveScale, waveScale, 1f);
            Color waveColor = iconWaveImage.color;
            waveColor.a = 0.22f - Mathf.Repeat(iconAnimationTime * 0.55f, 1f) * 0.18f;
            iconWaveImage.color = waveColor;
        }

        if (iconFacePlateImage != null)
        {
            Color facePlateColor = iconFacePlateImage.color;
            facePlateColor.a = 0.16f + (Mathf.Sin(iconAnimationTime * 2.2f) * 0.04f + 0.04f);
            iconFacePlateImage.color = facePlateColor;
        }

        if (iconAvatarHighlightImage != null && usePhotoAvatar)
        {
            Color highlightColor = iconAvatarHighlightImage.color;
            highlightColor.a = 0.06f + (Mathf.Sin(iconAnimationTime * 2.4f) * 0.03f + 0.03f);
            iconAvatarHighlightImage.color = highlightColor;
        }

        if (iconMouthImage != null)
        {
            iconMouthImage.rectTransform.sizeDelta = new Vector2(12f + Mathf.Sin(iconAnimationTime * 2.8f) * 1.5f, 4f);
        }

        if (iconAntennaImage != null)
        {
            Color antennaColor = iconAntennaImage.color;
            antennaColor.a = 0.78f + Mathf.Sin(iconAnimationTime * 3.2f) * 0.17f;
            iconAntennaImage.color = antennaColor;
        }

        UpdateBlinkAnimation();
    }

    private void UpdateBlinkAnimation()
    {
        if (iconEyeLeft == null || iconEyeRight == null)
        {
            return;
        }

        blinkTimer += Time.unscaledDeltaTime;
        float cycle = Mathf.Repeat(blinkTimer, 4.2f);
        float blink = 1f;
        if (cycle > 3.75f)
        {
            float t = (cycle - 3.75f) / 0.45f;
            blink = Mathf.Abs(Mathf.Cos(t * Mathf.PI * 2f));
            blink = Mathf.Clamp(blink, 0.12f, 1f);
        }

        iconEyeLeft.localScale = new Vector3(1f, blink, 1f);
        iconEyeRight.localScale = new Vector3(1f, blink, 1f);
    }

    private void UpdateAvatarFrameAnimation()
    {
        if (avatarFrames.Count <= 1)
        {
            return;
        }

        int frameIndex = Mathf.FloorToInt(iconAnimationTime * GetAvatarFrameRate()) % avatarFrames.Count;
        if (frameIndex == currentAvatarFrameIndex)
        {
            return;
        }

        currentAvatarFrameIndex = frameIndex;
        Sprite frame = avatarFrames[frameIndex];

        if (iconAvatarImage != null && iconAvatarImage.gameObject.activeSelf)
        {
            iconAvatarImage.sprite = frame;
        }

        if (iconFreeformAvatarImage != null && iconFreeformAvatarImage.gameObject.activeSelf)
        {
            iconFreeformAvatarImage.sprite = frame;
        }
    }

    private void ApplyFloatingIconAvatar()
    {
        avatarFrames.Clear();
        avatarFrames.AddRange(LoadAssistantAvatarFrames());

        Texture2D avatarTexture = null;
        if (avatarFrames.Count == 0)
        {
            avatarTexture = LoadAssistantAvatarTexture();
            if (avatarTexture != null)
            {
                avatarFrames.Add(Sprite.Create(
                    avatarTexture,
                    new Rect(0f, 0f, avatarTexture.width, avatarTexture.height),
                    new Vector2(0.5f, 0.5f)));
            }
        }

        currentAvatarFrameIndex = -1;
        usePhotoAvatar = avatarFrames.Count > 0;
        useFreeformAvatar = usePhotoAvatar && string.Equals(config.assistantAvatarPresentation, "freeform", StringComparison.OrdinalIgnoreCase);

        float avatarSize = Mathf.Max(72f, config.assistantAvatarSize);
        if (iconAvatarImage != null)
        {
            iconAvatarImage.rectTransform.sizeDelta = new Vector2(avatarSize - 12f, avatarSize - 12f);
        }
        if (iconAvatarHighlightImage != null)
        {
            iconAvatarHighlightImage.rectTransform.sizeDelta = new Vector2(avatarSize - 12f, avatarSize - 12f);
        }
        if (iconFreeformAvatarImage != null)
        {
            iconFreeformAvatarImage.rectTransform.sizeDelta = new Vector2(avatarSize, avatarSize);
        }

        if (iconAvatarImage != null)
        {
            if (usePhotoAvatar && !useFreeformAvatar)
            {
                iconAvatarImage.sprite = avatarFrames[0];
                iconAvatarImage.color = Color.white;
                iconAvatarImage.gameObject.SetActive(true);
                if (iconAvatarOutline != null) iconAvatarOutline.enabled = true;
                if (iconAvatarShadow != null) iconAvatarShadow.enabled = true;
            }
            else
            {
                iconAvatarImage.sprite = null;
                iconAvatarImage.color = new Color(1f, 1f, 1f, 0f);
                iconAvatarImage.gameObject.SetActive(false);
                if (iconAvatarOutline != null) iconAvatarOutline.enabled = false;
                if (iconAvatarShadow != null) iconAvatarShadow.enabled = false;
            }
        }

        if (iconFreeformAvatarImage != null)
        {
            if (usePhotoAvatar && useFreeformAvatar)
            {
                iconFreeformAvatarImage.sprite = avatarFrames[0];
                iconFreeformAvatarImage.color = Color.white;
                iconFreeformAvatarImage.gameObject.SetActive(true);
                if (iconFreeformAvatarOutline != null) iconFreeformAvatarOutline.enabled = true;
                if (iconFreeformAvatarShadow != null) iconFreeformAvatarShadow.enabled = true;
            }
            else
            {
                iconFreeformAvatarImage.sprite = null;
                iconFreeformAvatarImage.color = new Color(1f, 1f, 1f, 0f);
                iconFreeformAvatarImage.gameObject.SetActive(false);
                if (iconFreeformAvatarOutline != null) iconFreeformAvatarOutline.enabled = false;
                if (iconFreeformAvatarShadow != null) iconFreeformAvatarShadow.enabled = false;
            }
        }

        if (iconAvatarHighlightImage != null)
        {
            iconAvatarHighlightImage.gameObject.SetActive(usePhotoAvatar && !useFreeformAvatar);
        }

        if (iconAvatarMaskImage != null)
        {
            iconAvatarMaskImage.rectTransform.sizeDelta = new Vector2(avatarSize - 8f, avatarSize - 8f);
            iconAvatarMaskImage.enabled = false;
        }

        if (iconAvatarMask != null)
        {
            iconAvatarMask.enabled = false;
            iconAvatarMask.showMaskGraphic = false;
        }

        SetRobotFaceVisible(!usePhotoAvatar);

        if (iconCoreImage != null)
        {
            iconCoreImage.enabled = !usePhotoAvatar;
            iconCoreImage.color = usePhotoAvatar
                ? new Color(0.08f, 0.36f, 0.62f, 0f)
                : new Color(0.08f, 0.36f, 0.62f, 0.95f);
        }

        if (iconGlowImage != null)
        {
            iconGlowImage.gameObject.SetActive(!usePhotoAvatar);
        }

        if (iconWaveImage != null)
        {
            iconWaveImage.gameObject.SetActive(!usePhotoAvatar);
        }

        if (iconCoreOutline != null)
        {
            iconCoreOutline.enabled = !usePhotoAvatar;
            iconCoreOutline.effectColor = usePhotoAvatar
                ? new Color(0.47f, 0.87f, 1f, 0.65f)
                : new Color(0.47f, 0.87f, 1f, 0.4f);
        }

        if (iconCoreShadow != null)
        {
            iconCoreShadow.enabled = !usePhotoAvatar;
        }

        if (iconCoreRect != null)
        {
            iconCoreRect.sizeDelta = usePhotoAvatar ? Vector2.zero : new Vector2(72f, 72f);
        }

    }

    private void SetRobotFaceVisible(bool visible)
    {
        if (iconFacePlateImage != null)
        {
            iconFacePlateImage.gameObject.SetActive(visible);
        }
        if (iconAntennaImage != null)
        {
            iconAntennaImage.gameObject.SetActive(visible);
        }
        if (iconAntennaTipImage != null)
        {
            iconAntennaTipImage.gameObject.SetActive(visible);
        }
        if (iconEyeLeft != null)
        {
            iconEyeLeft.gameObject.SetActive(visible);
        }
        if (iconEyeRight != null)
        {
            iconEyeRight.gameObject.SetActive(visible);
        }
        if (iconMouthImage != null)
        {
            iconMouthImage.gameObject.SetActive(visible);
        }
    }

    private Texture2D LoadAssistantAvatarTexture()
    {
        string avatarPath = ResolveAssistantAvatarPath();
        if (string.IsNullOrEmpty(avatarPath) || !File.Exists(avatarPath))
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(avatarPath);
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes, false))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            return texture;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Load assistant avatar failed: " + ex.Message);
            return null;
        }
    }

    private List<Sprite> LoadAssistantAvatarFrames()
    {
        List<Sprite> frames = new List<Sprite>();
        string folderPath = ResolveAssistantAvatarFramesFolderPath();
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            return frames;
        }

        try
        {
            string[] files = Directory.GetFiles(folderPath);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < files.Length; i++)
            {
                string extension = Path.GetExtension(files[i]).ToLowerInvariant();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                {
                    continue;
                }

                byte[] bytes = File.ReadAllBytes(files[i]);
                if (bytes == null || bytes.Length == 0)
                {
                    continue;
                }

                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes, false))
                {
                    UnityEngine.Object.Destroy(texture);
                    continue;
                }

                frames.Add(Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f)));
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Load assistant avatar frames failed: " + ex.Message);
        }

        return frames;
    }

    private string ResolveAssistantAvatarPath()
    {
        if (config == null || string.IsNullOrEmpty(config.assistantAvatarImage))
        {
            return "";
        }

        if (Path.IsPathRooted(config.assistantAvatarImage))
        {
            return config.assistantAvatarImage;
        }

        return Path.Combine(Application.streamingAssetsPath, config.assistantAvatarImage);
    }

    private string ResolveAssistantAvatarFramesFolderPath()
    {
        if (config == null || string.IsNullOrEmpty(config.assistantAvatarFramesFolder))
        {
            return "";
        }

        if (Path.IsPathRooted(config.assistantAvatarFramesFolder))
        {
            return config.assistantAvatarFramesFolder;
        }

        return Path.Combine(Application.streamingAssetsPath, config.assistantAvatarFramesFolder);
    }

    private float GetAvatarFrameRate()
    {
        return Mathf.Max(1f, config != null ? config.assistantAvatarFrameRate : 6f);
    }

    private bool IsEdgeClockwiseMovementMode()
    {
        return config != null && string.Equals(config.assistantMovementMode, "edge_cw", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsEdgeCounterClockwiseMovementMode()
    {
        return config != null &&
               (string.Equals(config.assistantMovementMode, "edge_ccw", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(config.assistantMovementMode, "edge", StringComparison.OrdinalIgnoreCase));
    }

    private bool IsFloatMovementMode()
    {
        return config != null && string.Equals(config.assistantMovementMode, "float", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsSidePingPongMovementMode()
    {
        return config != null && string.Equals(config.assistantMovementMode, "side_pingpong", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsScreenRandomMovementMode()
    {
        return config != null && string.Equals(config.assistantMovementMode, "screen_random", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsRandomMovementMode()
    {
        return config != null && string.Equals(config.assistantMovementMode, "random", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsFixedMovementMode()
    {
        return config == null ||
               string.Equals(config.assistantMovementMode, "fixed", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(config.assistantMovementMode, "none", StringComparison.OrdinalIgnoreCase) ||
               string.IsNullOrEmpty(config.assistantMovementMode);
    }

    private Vector2 GetAnimatedIconPosition()
    {
        if (hasUserPinnedIconPosition && (IsFixedMovementMode() || IsFloatMovementMode()))
        {
            return userPinnedIconPosition;
        }

        if (UseManualSceneLayout() && (IsFixedMovementMode() || IsFloatMovementMode()))
        {
            return hasAuthoredIconBasePosition ? authoredIconBasePosition : IconBasePosition;
        }

        RectTransform canvasRect = transform as RectTransform;
        float canvasWidth = 1920f;
        float canvasHeight = 1080f;
        if (canvasRect != null)
        {
            if (canvasRect.rect.width > 0f)
            {
                canvasWidth = canvasRect.rect.width;
            }
            if (canvasRect.rect.height > 0f)
            {
                canvasHeight = canvasRect.rect.height;
            }
        }

        float size = Mathf.Max(72f, config != null ? config.assistantAvatarSize : 96f);
        float margin = size * 0.5f + 14f;

        if (IsSidePingPongMovementMode())
        {
            return GetSidePingPongPosition(canvasWidth, canvasHeight, margin);
        }

        if (IsEdgeClockwiseMovementMode())
        {
            return GetEdgePathPositionByDistance(canvasWidth, canvasHeight, margin, iconAnimationTime * GetMoveSpeed(), true);
        }

        if (IsEdgeCounterClockwiseMovementMode())
        {
            return GetEdgePathPositionByDistance(canvasWidth, canvasHeight, margin, iconAnimationTime * GetMoveSpeed(), false);
        }

        if (IsScreenRandomMovementMode())
        {
            return GetScreenRandomPosition(canvasWidth, canvasHeight, margin);
        }

        if (IsRandomMovementMode())
        {
            return GetRandomMovementPosition(canvasWidth, canvasHeight, margin);
        }

        return GetIdleAnchorPosition(canvasWidth, canvasHeight, margin);
    }

    private bool TryGetIconCanvasMetrics(out float canvasWidth, out float canvasHeight, out float margin)
    {
        canvasWidth = 1920f;
        canvasHeight = 1080f;
        float size = Mathf.Max(72f, config != null ? config.assistantAvatarSize : 96f);
        margin = size * 0.5f + 14f;

        RectTransform layerRect = iconRoot != null ? iconRoot.parent as RectTransform : null;
        if (layerRect == null)
        {
            RectTransform canvasRect = transform as RectTransform;
            if (canvasRect != null)
            {
                layerRect = canvasRect;
            }
        }

        if (layerRect == null)
        {
            return false;
        }

        if (layerRect.rect.width > 0f)
        {
            canvasWidth = layerRect.rect.width;
        }
        if (layerRect.rect.height > 0f)
        {
            canvasHeight = layerRect.rect.height;
        }

        return true;
    }

    private bool TryGetIconAnchoredPositionFromScreen(Vector2 screenPosition, out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;

        if (iconRoot == null)
        {
            return false;
        }

        RectTransform layerRect = iconRoot.parent as RectTransform;
        if (layerRect == null)
        {
            return false;
        }

        Camera eventCamera = null;
        if (iconLayerCanvas != null && iconLayerCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = iconLayerCanvas.worldCamera;
        }

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layerRect, screenPosition, eventCamera, out localPoint))
        {
            return false;
        }

        anchoredPosition = localPoint + new Vector2(layerRect.rect.width * layerRect.pivot.x, layerRect.rect.height * layerRect.pivot.y);
        return true;
    }

    private Vector2 ClampIconPositionToCanvas(Vector2 anchoredPosition)
    {
        float canvasWidth;
        float canvasHeight;
        float margin;
        TryGetIconCanvasMetrics(out canvasWidth, out canvasHeight, out margin);

        float left = margin;
        float right = Mathf.Max(left + 1f, canvasWidth - margin);
        float bottom = margin;
        float top = Mathf.Max(bottom + 1f, canvasHeight - margin);

        return new Vector2(
            Mathf.Clamp(anchoredPosition.x, left, right),
            Mathf.Clamp(anchoredPosition.y, bottom, top));
    }

    private Vector2 GetNearestEdgeSnapPosition(Vector2 anchoredPosition, out string snappedEdge)
    {
        float canvasWidth;
        float canvasHeight;
        float margin;
        TryGetIconCanvasMetrics(out canvasWidth, out canvasHeight, out margin);

        float left = margin;
        float right = Mathf.Max(left + 1f, canvasWidth - margin);
        float bottom = margin;
        float top = Mathf.Max(bottom + 1f, canvasHeight - margin);

        float distanceLeft = Mathf.Abs(anchoredPosition.x - left);
        float distanceRight = Mathf.Abs(right - anchoredPosition.x);
        float distanceBottom = Mathf.Abs(anchoredPosition.y - bottom);
        float distanceTop = Mathf.Abs(top - anchoredPosition.y);

        snappedEdge = "left";
        float minDistance = distanceLeft;
        Vector2 snappedPosition = new Vector2(left, anchoredPosition.y);

        if (distanceRight < minDistance)
        {
            minDistance = distanceRight;
            snappedEdge = "right";
            snappedPosition = new Vector2(right, anchoredPosition.y);
        }

        if (distanceBottom < minDistance)
        {
            minDistance = distanceBottom;
            snappedEdge = "bottom";
            snappedPosition = new Vector2(anchoredPosition.x, bottom);
        }

        if (distanceTop < minDistance)
        {
            snappedEdge = "top";
            snappedPosition = new Vector2(anchoredPosition.x, top);
        }

        snappedPosition.x = Mathf.Clamp(snappedPosition.x, left, right);
        snappedPosition.y = Mathf.Clamp(snappedPosition.y, bottom, top);
        return snappedPosition;
    }

    private void ApplyPinnedIconPosition(Vector2 position)
    {
        hasUserPinnedIconPosition = true;
        userPinnedIconPosition = position;
        authoredIconBasePosition = position;
        hasAuthoredIconBasePosition = true;
        idleAnchorPosition = position;
        idleAnchorCanvasWidth = -1f;
        idleAnchorCanvasHeight = -1f;
        roamingCanvasWidth = -1f;
        roamingCanvasHeight = -1f;
        roamingPattern = -1;

        if (iconRoot != null)
        {
            iconRoot.anchoredPosition = position;
        }
    }

    private float GetDesiredQuestionHeight(float availableWidth, int inputFontSize, float minimumHeight)
    {
        if (questionInput == null || questionInput.textComponent == null)
        {
            return minimumHeight;
        }

        float contentWidth = Mathf.Max(120f, availableWidth - 24f);
        float lineHeight = Mathf.Max(20f, inputFontSize * 1.35f);
        float maxContentHeight = lineHeight * QuestionInputMaxLines;
        string measureText = string.IsNullOrEmpty(questionInput.text) ? " " : questionInput.text + " ";
        TextGenerationSettings settings = questionInput.textComponent.GetGenerationSettings(new Vector2(contentWidth, 0f));
        settings.generateOutOfBounds = true;
        float preferredHeight = questionInput.textComponent.cachedTextGeneratorForLayout.GetPreferredHeight(measureText, settings) / questionInput.textComponent.pixelsPerUnit;
        float clampedContentHeight = Mathf.Clamp(Mathf.Max(lineHeight, preferredHeight), lineHeight, maxContentHeight);
        return Mathf.Max(minimumHeight, clampedContentHeight + 16f);
    }

    private void RefreshQuestionInputLayout()
    {
        if (questionInput == null || questionRect == null)
        {
            return;
        }

        if (UseManualSceneLayout() && !forceAutoLayoutPass)
        {
            ApplyManualQuestionInputLayout();
            return;
        }

        RefreshCompactLayout(loadedImageBytes != null && loadedImageBytes.Length > 0);
    }

    private void ApplyManualQuestionInputLayout()
    {
        if (panelRoot == null || questionRect == null || !hasAuthoredQuestionSize)
        {
            return;
        }

        float availableWidth = questionRect.rect.width > 0f ? questionRect.rect.width : questionRect.sizeDelta.x;
        float desiredHeight = GetDesiredQuestionHeight(availableWidth, questionInput != null && questionInput.textComponent != null ? questionInput.textComponent.fontSize : 16, authoredQuestionSize.y > 0f ? authoredQuestionSize.y : 48f);
        float delta = desiredHeight - authoredQuestionSize.y;

        questionRect.sizeDelta = new Vector2(questionRect.sizeDelta.x, desiredHeight);

        if (panelRoot != null && hasAuthoredPanelSize)
        {
            panelRoot.sizeDelta = new Vector2(authoredPanelSize.x, authoredPanelSize.y + delta);
            if (isPanelVisible)
            {
                panelRoot.anchoredPosition = GetPanelShownPosition();
            }
            else
            {
                panelRoot.anchoredPosition = GetPanelHiddenPosition();
            }
        }
    }

    private Vector2 GetIdleAnchorPosition(float canvasWidth, float canvasHeight, float margin)
    {
        if (hasUserPinnedIconPosition)
        {
            return userPinnedIconPosition;
        }

        if (UseManualSceneLayout() && hasAuthoredIconBasePosition)
        {
            return authoredIconBasePosition;
        }

        float left = margin;
        float right = Mathf.Max(left + 1f, canvasWidth - margin);
        float bottom = margin;
        float top = Mathf.Max(bottom + 1f, canvasHeight - margin);

        if (Mathf.Abs(idleAnchorCanvasWidth - canvasWidth) > 0.1f ||
            Mathf.Abs(idleAnchorCanvasHeight - canvasHeight) > 0.1f ||
            idleAnchorPosition == Vector2.zero)
        {
            idleAnchorCanvasWidth = canvasWidth;
            idleAnchorCanvasHeight = canvasHeight;
            activeIdleAnchor = ResolveIdleAnchor();

            if (string.Equals(activeIdleAnchor, "edge_random", StringComparison.OrdinalIgnoreCase))
            {
                activeIdleAnchor = PickRandomEdge() + "_edge_random";
            }

            idleAnchorPosition = ResolveIdleAnchorPosition(left, right, bottom, top, activeIdleAnchor);
        }

        return idleAnchorPosition;
    }

    private Vector2 GetRandomMovementPosition(float canvasWidth, float canvasHeight, float margin)
    {
        if (roamingPattern < 0 ||
            Mathf.Abs(roamingCanvasWidth - canvasWidth) > 0.1f ||
            Mathf.Abs(roamingCanvasHeight - canvasHeight) > 0.1f)
        {
            roamingCanvasWidth = canvasWidth;
            roamingCanvasHeight = canvasHeight;
            SelectRandomRoamingPattern(canvasWidth, canvasHeight, margin, true);
        }

        roamingPatternTimer += Time.unscaledDeltaTime;
        if (roamingPatternTimer >= roamingPatternDuration)
        {
            SelectRandomRoamingPattern(canvasWidth, canvasHeight, margin, false);
        }

        float speed = GetMoveSpeed();
        float left = margin;
        float right = Mathf.Max(left + 1f, canvasWidth - margin);
        float bottom = margin;
        float top = Mathf.Max(bottom + 1f, canvasHeight - margin);

        switch (roamingPattern)
        {
            case 0:
            {
                if (string.Equals(activeMovementEdge, "left", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(activeMovementEdge, "right", StringComparison.OrdinalIgnoreCase))
                {
                    float vertical = Mathf.Max(1f, top - bottom);
                    float phaseDistance = Mathf.PingPong(roamingPatternTimer * speed, vertical);
                    roamingPosition = new Vector2(roamingSideX, bottom + phaseDistance);
                }
                else
                {
                    float horizontal = Mathf.Max(1f, right - left);
                    float phaseDistance = Mathf.PingPong(roamingPatternTimer * speed, horizontal);
                    roamingPosition = new Vector2(left + phaseDistance, roamingSideY);
                }
                break;
            }
            case 1:
            case 2:
            {
                float perimeterDistance = roamingPerimeterOffset + roamingPatternTimer * speed * roamingPerimeterDirection;
                roamingPosition = GetEdgePathPositionByDistance(canvasWidth, canvasHeight, margin, perimeterDistance, roamingPerimeterDirection > 0);
                break;
            }
            default:
            {
                roamingPosition += roamingVelocity * Time.unscaledDeltaTime;

                if (roamingPosition.x < left)
                {
                    roamingPosition.x = left;
                    roamingVelocity.x = Mathf.Abs(roamingVelocity.x);
                }
                else if (roamingPosition.x > right)
                {
                    roamingPosition.x = right;
                    roamingVelocity.x = -Mathf.Abs(roamingVelocity.x);
                }

                if (roamingPosition.y < bottom)
                {
                    roamingPosition.y = bottom;
                    roamingVelocity.y = Mathf.Abs(roamingVelocity.y);
                }
                else if (roamingPosition.y > top)
                {
                    roamingPosition.y = top;
                    roamingVelocity.y = -Mathf.Abs(roamingVelocity.y);
                }

                if (UnityEngine.Random.value < 0.015f)
                {
                    float turnAngle = UnityEngine.Random.Range(-40f, 40f) * Mathf.Deg2Rad;
                    float cos = Mathf.Cos(turnAngle);
                    float sin = Mathf.Sin(turnAngle);
                    Vector2 v = roamingVelocity;
                    roamingVelocity = new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
                }
                break;
            }
        }

        return roamingPosition;
    }

    private Vector2 GetEdgePathPosition(float canvasWidth, float canvasHeight, float margin)
    {
        return GetEdgePathPositionByDistance(
            canvasWidth,
            canvasHeight,
            margin,
            iconAnimationTime * GetMoveSpeed(),
            true);
    }

    private Vector2 GetEdgePathPositionByDistance(float canvasWidth, float canvasHeight, float margin, float rawDistance, bool clockwise)
    {
        float left = margin;
        float right = Mathf.Max(left + 1f, canvasWidth - margin);
        float bottom = margin;
        float top = Mathf.Max(bottom + 1f, canvasHeight - margin);
        float horizontal = Mathf.Max(1f, right - left);
        float vertical = Mathf.Max(1f, top - bottom);
        float perimeter = (horizontal + vertical) * 2f;
        float distance = Mathf.Repeat(rawDistance, perimeter);

        if (clockwise)
        {
            if (distance < horizontal)
            {
                return new Vector2(left + distance, bottom);
            }
            distance -= horizontal;

            if (distance < vertical)
            {
                return new Vector2(right, bottom + distance);
            }
            distance -= vertical;

            if (distance < horizontal)
            {
                return new Vector2(right - distance, top);
            }
            distance -= horizontal;

            return new Vector2(left, top - distance);
        }

        if (distance < vertical)
        {
            return new Vector2(right, bottom + distance);
        }
        distance -= vertical;

        if (distance < horizontal)
        {
            return new Vector2(right - distance, top);
        }
        distance -= horizontal;

        if (distance < vertical)
        {
            return new Vector2(left, top - distance);
        }
        distance -= vertical;

        return new Vector2(left + distance, bottom);
    }

    private Vector2 GetSidePingPongPosition(float canvasWidth, float canvasHeight, float margin)
    {
        float left = margin;
        float right = Mathf.Max(left + 1f, canvasWidth - margin);
        float bottom = margin;
        float top = Mathf.Max(bottom + 1f, canvasHeight - margin);
        float horizontal = Mathf.Max(1f, right - left);
        float vertical = Mathf.Max(1f, top - bottom);

        if (string.IsNullOrEmpty(activeMovementEdge) ||
            Mathf.Abs(roamingCanvasWidth - canvasWidth) > 0.1f ||
            Mathf.Abs(roamingCanvasHeight - canvasHeight) > 0.1f)
        {
            roamingCanvasWidth = canvasWidth;
            roamingCanvasHeight = canvasHeight;
            activeMovementEdge = ResolveConfiguredMovementEdge();
            if (string.Equals(activeMovementEdge, "random", StringComparison.OrdinalIgnoreCase))
            {
                activeMovementEdge = PickRandomEdge();
            }
            roamingPosition = GetSidePingPongAnchor(left, right, bottom, top, activeMovementEdge);
        }

        float distance = Mathf.PingPong(iconAnimationTime * GetMoveSpeed(), 
            (string.Equals(activeMovementEdge, "left", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(activeMovementEdge, "right", StringComparison.OrdinalIgnoreCase)) ? vertical : horizontal);

        if (string.Equals(activeMovementEdge, "left", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(left, bottom + distance);
        }
        if (string.Equals(activeMovementEdge, "top", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(left + distance, top);
        }
        if (string.Equals(activeMovementEdge, "bottom", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(left + distance, bottom);
        }

        return new Vector2(right, bottom + distance);
    }

    private Vector2 GetScreenRandomPosition(float canvasWidth, float canvasHeight, float margin)
    {
        float left = margin;
        float right = Mathf.Max(left + 1f, canvasWidth - margin);
        float bottom = margin;
        float top = Mathf.Max(bottom + 1f, canvasHeight - margin);

        if (roamingVelocity.sqrMagnitude < 0.001f ||
            Mathf.Abs(roamingCanvasWidth - canvasWidth) > 0.1f ||
            Mathf.Abs(roamingCanvasHeight - canvasHeight) > 0.1f)
        {
            roamingCanvasWidth = canvasWidth;
            roamingCanvasHeight = canvasHeight;
            roamingPosition = new Vector2(
                UnityEngine.Random.Range(left, right),
                UnityEngine.Random.Range(bottom, top));
            Vector2 direction = UnityEngine.Random.insideUnitCircle;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = new Vector2(0.85f, 0.45f);
            }
            roamingVelocity = direction * GetMoveSpeed();
        }

        roamingPosition += roamingVelocity * Time.unscaledDeltaTime;

        if (roamingPosition.x < left)
        {
            roamingPosition.x = left;
            roamingVelocity.x = Mathf.Abs(roamingVelocity.x);
        }
        else if (roamingPosition.x > right)
        {
            roamingPosition.x = right;
            roamingVelocity.x = -Mathf.Abs(roamingVelocity.x);
        }

        if (roamingPosition.y < bottom)
        {
            roamingPosition.y = bottom;
            roamingVelocity.y = Mathf.Abs(roamingVelocity.y);
        }
        else if (roamingPosition.y > top)
        {
            roamingPosition.y = top;
            roamingVelocity.y = -Mathf.Abs(roamingVelocity.y);
        }

        return roamingPosition;
    }

    private void SelectRandomRoamingPattern(float canvasWidth, float canvasHeight, float margin, bool initial)
    {
        float left = margin;
        float right = Mathf.Max(left + 1f, canvasWidth - margin);
        float bottom = margin;
        float top = Mathf.Max(bottom + 1f, canvasHeight - margin);
        float horizontal = Mathf.Max(1f, right - left);
        float vertical = Mathf.Max(1f, top - bottom);
        float perimeter = (horizontal + vertical) * 2f;
        float speed = GetMoveSpeed();

        roamingPattern = UnityEngine.Random.Range(0, 4);
        roamingPatternTimer = 0f;
        roamingPatternDuration = initial
            ? UnityEngine.Random.Range(2.4f, 4.0f)
            : UnityEngine.Random.Range(3.2f, 6.4f);

        switch (roamingPattern)
        {
            case 0:
                activeMovementEdge = ResolveConfiguredMovementEdge();
                if (string.Equals(activeMovementEdge, "random", StringComparison.OrdinalIgnoreCase))
                {
                    activeMovementEdge = PickRandomEdge();
                }
                roamingPosition = GetSidePingPongAnchor(left, right, bottom, top, activeMovementEdge);
                break;
            case 1:
                roamingPerimeterDirection = 1;
                roamingPerimeterOffset = UnityEngine.Random.Range(0f, perimeter);
                roamingPosition = GetEdgePathPositionByDistance(canvasWidth, canvasHeight, margin, roamingPerimeterOffset, true);
                break;
            case 2:
                roamingPerimeterDirection = -1;
                roamingPerimeterOffset = UnityEngine.Random.Range(0f, perimeter);
                roamingPosition = GetEdgePathPositionByDistance(canvasWidth, canvasHeight, margin, roamingPerimeterOffset, false);
                break;
            default:
                roamingPosition = new Vector2(
                    UnityEngine.Random.Range(left, right),
                    UnityEngine.Random.Range(bottom, top));
                Vector2 direction = UnityEngine.Random.insideUnitCircle;
                if (direction.sqrMagnitude < 0.001f)
                {
                    direction = new Vector2(0.82f, 0.38f);
                }
                roamingVelocity = direction.normalized * speed;
                break;
        }
    }

    private float GetMoveSpeed()
    {
        return config != null ? Mathf.Max(40f, config.assistantMoveSpeed) : 180f;
    }

    private string ResolveConfiguredMovementEdge()
    {
        if (config == null || string.IsNullOrEmpty(config.assistantMovementEdge))
        {
            return "random";
        }

        return config.assistantMovementEdge.Trim().ToLowerInvariant();
    }

    private string PickRandomEdge()
    {
        string[] edges = { "top", "right", "bottom", "left" };
        return edges[UnityEngine.Random.Range(0, edges.Length)];
    }

    private string ResolveIdleAnchor()
    {
        if (config == null || string.IsNullOrEmpty(config.assistantIdleAnchor))
        {
            return "bottom_right";
        }

        return config.assistantIdleAnchor.Trim().ToLowerInvariant();
    }

    private Vector2 ResolveIdleAnchorPosition(float left, float right, float bottom, float top, string anchor)
    {
        if (string.Equals(anchor, "top_left", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(left, top);
        }
        if (string.Equals(anchor, "top_right", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(right, top);
        }
        if (string.Equals(anchor, "bottom_left", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(left, bottom);
        }
        if (string.Equals(anchor, "left_edge_random", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(left, UnityEngine.Random.Range(bottom, top));
        }
        if (string.Equals(anchor, "top_edge_random", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(UnityEngine.Random.Range(left, right), top);
        }
        if (string.Equals(anchor, "right_edge_random", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(right, UnityEngine.Random.Range(bottom, top));
        }
        if (string.Equals(anchor, "bottom_edge_random", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(UnityEngine.Random.Range(left, right), bottom);
        }

        return new Vector2(right, bottom);
    }

    private Vector2 GetSidePingPongAnchor(float left, float right, float bottom, float top, string edge)
    {
        if (string.Equals(edge, "left", StringComparison.OrdinalIgnoreCase))
        {
            roamingSideX = left;
            roamingSideY = UnityEngine.Random.Range(bottom, top);
            return new Vector2(left, roamingSideY);
        }
        if (string.Equals(edge, "top", StringComparison.OrdinalIgnoreCase))
        {
            roamingSideX = UnityEngine.Random.Range(left, right);
            roamingSideY = top;
            return new Vector2(roamingSideX, top);
        }
        if (string.Equals(edge, "bottom", StringComparison.OrdinalIgnoreCase))
        {
            roamingSideX = UnityEngine.Random.Range(left, right);
            roamingSideY = bottom;
            return new Vector2(roamingSideX, bottom);
        }

        roamingSideX = right;
        roamingSideY = UnityEngine.Random.Range(bottom, top);
        return new Vector2(right, roamingSideY);
    }

    private void UpdatePanelAnimation()
    {
        if (panelRoot == null || panelCanvasGroup == null)
        {
            return;
        }

        if (isDraggingPanel)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.blocksRaycasts = true;
            panelCanvasGroup.interactable = true;
            return;
        }

        float target = isPanelVisible ? 1f : 0f;
        panelSlideProgress = Mathf.MoveTowards(panelSlideProgress, target, Time.unscaledDeltaTime * 5f);
        float eased = isPanelVisible ? EaseOutBack(panelSlideProgress) : EaseInCubic(panelSlideProgress);
        panelRoot.anchoredPosition = Vector2.Lerp(GetPanelHiddenPosition(), GetPanelShownPosition(), eased);
        panelCanvasGroup.alpha = Mathf.Clamp01(panelSlideProgress * 1.15f);

        if (!isPanelVisible && panelSlideProgress <= 0.001f)
        {
            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.interactable = false;
        }
    }

    private RectTransform CreateEye(string name, Transform parent, Vector2 anchoredPosition)
    {
        GameObject eye = CreateUiObject(name, parent);
        Image eyeImage = eye.AddComponent<Image>();
        eyeImage.color = new Color(0.55f, 0.95f, 1f, 1f);
        RectTransform rect = eye.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(9f, 9f);
        rect.anchoredPosition = anchoredPosition;
        return rect;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        float p = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

    private float EaseInCubic(float t)
    {
        return t * t * t;
    }

    private void SetPreviewVisible(bool visible)
    {
        if (imagePreviewLayout != null)
        {
            imagePreviewLayout.preferredHeight = visible ? 44f : 0f;
        }

        if (imagePreview != null)
        {
            imagePreview.gameObject.SetActive(visible);
        }

        if (conversationLayout != null)
        {
            conversationLayout.preferredHeight = visible ? 138f : 188f;
        }

        if (visible)
        {
            EnsureManualPreviewVisible();
        }

        RefreshCompactLayout(visible);
    }

    private void EnsureManualPreviewVisible()
    {
        if (!UseManualSceneLayout() || previewRect == null || imagePreview == null)
        {
            return;
        }

        if (GetRectWorldWidth(previewRect) > 8f && GetRectWorldHeight(previewRect) > 8f)
        {
            return;
        }

        RectTransform referenceRect = actionRowRect != null ? actionRowRect : questionRect;
        if (referenceRect == null)
        {
            referenceRect = conversationRect;
        }

        if (referenceRect == null)
        {
            return;
        }

        Vector3[] referenceCorners = new Vector3[4];
        referenceRect.GetWorldCorners(referenceCorners);
        Vector3 referenceTopCenter = (referenceCorners[1] + referenceCorners[2]) * 0.5f;

        previewRect.anchorMin = new Vector2(0f, 0f);
        previewRect.anchorMax = new Vector2(0f, 0f);
        previewRect.pivot = new Vector2(0.5f, 0f);
        previewRect.sizeDelta = new Vector2(92f, 52f);
        previewRect.position = referenceTopCenter + new Vector3(0f, 8f, 0f);
        previewRect.localScale = Vector3.one;
    }

    private float GetRectWorldWidth(RectTransform rect)
    {
        if (rect == null)
        {
            return 0f;
        }

        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return Mathf.Abs(corners[3].x - corners[0].x);
    }

    private float GetRectWorldHeight(RectTransform rect)
    {
        if (rect == null)
        {
            return 0f;
        }

        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return Mathf.Abs(corners[1].y - corners[0].y);
    }

    private void AppendLine(string speaker, string text)
    {
        if (conversationText == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(conversationText.text))
        {
            conversationText.text += "\n\n";
        }

        if (string.IsNullOrEmpty(speaker))
        {
            conversationText.text += text;
            return;
        }

        conversationText.text += "<b>" + speaker + "：</b>\n" + text;
    }

    private string GetSpeechApiKey()
    {
        if (!string.IsNullOrEmpty(config.speechApiKey))
        {
            return config.speechApiKey;
        }

        return config.appBuilderApiKey;
    }

    private bool EnsureConfigured()
    {
        if (!string.IsNullOrEmpty(config.appId) && !string.IsNullOrEmpty(config.appBuilderApiKey))
        {
            return true;
        }

        SetStatus("未配置专属智能体的 appId 或 appBuilderApiKey。请编辑 " + WenxinAssistantConfig.GetConfigPath());
        return false;
    }

    private string BuildReadyText()
    {
        List<string> parts = new List<string>();
        parts.Add(SceneManager.GetActiveScene().name);

        if (ShouldShowProjectName())
        {
            parts.Add(GetConfiguredProjectName());
        }

        if (ShouldShowExperimentType())
        {
            parts.Add(GetConfiguredExperimentType());
        }

        return string.Join(" | ", parts.ToArray());
    }

    private string GetConfiguredProjectName()
    {
        if (config != null && !string.IsNullOrEmpty(config.assistantProjectName))
        {
            return config.assistantProjectName;
        }

        return "虚拟仿真实验";
    }

    private string GetConfiguredExperimentType()
    {
        if (config != null && !string.IsNullOrEmpty(config.assistantExperimentType))
        {
            return config.assistantExperimentType;
        }

        return "当前实验";
    }

    private void UpdateContextStatus()
    {
        if (!isBusy)
        {
            SetStatus(BuildReadyText());
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private bool ShouldShowAssistantName()
    {
        return config == null || config.showAssistantName;
    }

    private bool ShouldShowProjectName()
    {
        return config != null && config.showAssistantProjectName && !string.IsNullOrEmpty(config.assistantProjectName);
    }

    private string GetUiVariant()
    {
        return "compact";
    }

    private bool ShouldShowStatusBar()
    {
        return config != null && config.showStatusBar;
    }

    private bool ShouldShowImageInfo()
    {
        return config != null && config.showImageInfo;
    }

    private Vector2 GetPanelShownPosition()
    {
        if (hasUserPinnedPanelShownPosition)
        {
            return SnapPanelShownPositionToEdge(userPinnedPanelShownPosition, activePanelSnapEdge);
        }

        if (UseManualSceneLayout() && hasAuthoredPanelShownPosition)
        {
            return authoredPanelShownPosition;
        }

        float bottom = 108f;
        string variant = GetUiVariant();
        if (variant == "chat_card")
        {
            bottom = 92f;
        }
        else if (variant == "dense_console" || variant == "slim_sidebar")
        {
            bottom = 88f;
        }

        return new Vector2(PanelShownOffset.x, bottom);
    }

    private Vector2 GetPanelHiddenPosition()
    {
        float width = panelRoot != null ? panelRoot.sizeDelta.x : 320f;
        float height = panelRoot != null ? panelRoot.sizeDelta.y : 420f;
        Vector2 shown = GetPanelShownPosition();

        if (string.Equals(activePanelSnapEdge, "left", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(shown.x - width - 40f, shown.y);
        }

        if (string.Equals(activePanelSnapEdge, "top", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(shown.x, shown.y + height + 40f);
        }

        if (string.Equals(activePanelSnapEdge, "bottom", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(shown.x, shown.y - height - 40f);
        }

        return new Vector2(width + 40f, shown.y);
    }

    private bool TryGetPanelAnchoredPositionFromScreen(Vector2 screenPosition, out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;

        if (panelRoot == null)
        {
            return false;
        }

        RectTransform parentRect = panelRoot.parent as RectTransform;
        if (parentRect == null)
        {
            return false;
        }

        Camera eventCamera = null;
        Canvas rootCanvas = GetComponent<Canvas>();
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = rootCanvas.worldCamera;
        }

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, eventCamera, out localPoint))
        {
            return false;
        }

        float anchorX = (panelRoot.anchorMin.x - parentRect.pivot.x) * parentRect.rect.width;
        float anchorY = (panelRoot.anchorMin.y - parentRect.pivot.y) * parentRect.rect.height;
        anchoredPosition = localPoint - new Vector2(anchorX, anchorY);
        return true;
    }

    private bool TryGetPanelCanvasSize(out float canvasWidth, out float canvasHeight)
    {
        canvasWidth = 1920f;
        canvasHeight = 1080f;

        RectTransform parentRect = panelRoot != null ? panelRoot.parent as RectTransform : null;
        if (parentRect == null)
        {
            RectTransform canvasRect = transform as RectTransform;
            if (canvasRect != null)
            {
                parentRect = canvasRect;
            }
        }

        if (parentRect == null)
        {
            return false;
        }

        if (parentRect.rect.width > 0f)
        {
            canvasWidth = parentRect.rect.width;
        }
        if (parentRect.rect.height > 0f)
        {
            canvasHeight = parentRect.rect.height;
        }

        return true;
    }

    private Vector2 ClampPanelShownPosition(Vector2 anchoredPosition)
    {
        float canvasWidth;
        float canvasHeight;
        TryGetPanelCanvasSize(out canvasWidth, out canvasHeight);

        float width = panelRoot != null ? panelRoot.sizeDelta.x : 320f;
        float height = panelRoot != null ? panelRoot.sizeDelta.y : 420f;
        float minX = -(canvasWidth - width - PanelEdgePadding);
        float maxX = -PanelEdgePadding;
        float minY = PanelEdgePadding;
        float maxY = Mathf.Max(minY, canvasHeight - height - PanelEdgePadding);

        return new Vector2(
            Mathf.Clamp(anchoredPosition.x, minX, maxX),
            Mathf.Clamp(anchoredPosition.y, minY, maxY));
    }

    private Vector2 GetNearestPanelSnapPosition(Vector2 anchoredPosition, out string snappedEdge)
    {
        float canvasWidth;
        float canvasHeight;
        TryGetPanelCanvasSize(out canvasWidth, out canvasHeight);

        float width = panelRoot != null ? panelRoot.sizeDelta.x : 320f;
        float height = panelRoot != null ? panelRoot.sizeDelta.y : 420f;
        float panelRight = canvasWidth + anchoredPosition.x;
        float panelLeft = panelRight - width;
        float panelBottom = anchoredPosition.y;
        float panelTop = panelBottom + height;

        float distanceLeft = Mathf.Abs(panelLeft - PanelEdgePadding);
        float distanceRight = Mathf.Abs((canvasWidth - PanelEdgePadding) - panelRight);
        float distanceBottom = Mathf.Abs(panelBottom - PanelEdgePadding);
        float distanceTop = Mathf.Abs((canvasHeight - PanelEdgePadding) - panelTop);

        snappedEdge = "left";
        float minDistance = distanceLeft;

        if (distanceRight < minDistance)
        {
            minDistance = distanceRight;
            snappedEdge = "right";
        }

        if (distanceBottom < minDistance)
        {
            minDistance = distanceBottom;
            snappedEdge = "bottom";
        }

        if (distanceTop < minDistance)
        {
            snappedEdge = "top";
        }

        return SnapPanelShownPositionToEdge(anchoredPosition, snappedEdge);
    }

    private Vector2 SnapPanelShownPositionToEdge(Vector2 anchoredPosition, string snappedEdge)
    {
        float canvasWidth;
        float canvasHeight;
        TryGetPanelCanvasSize(out canvasWidth, out canvasHeight);

        float width = panelRoot != null ? panelRoot.sizeDelta.x : 320f;
        float height = panelRoot != null ? panelRoot.sizeDelta.y : 420f;
        float minX = -(canvasWidth - width - PanelEdgePadding);
        float maxX = -PanelEdgePadding;
        float minY = PanelEdgePadding;
        float maxY = Mathf.Max(minY, canvasHeight - height - PanelEdgePadding);

        if (string.Equals(snappedEdge, "left", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(minX, Mathf.Clamp(anchoredPosition.y, minY, maxY));
        }

        if (string.Equals(snappedEdge, "top", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(Mathf.Clamp(anchoredPosition.x, minX, maxX), maxY);
        }

        if (string.Equals(snappedEdge, "bottom", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(Mathf.Clamp(anchoredPosition.x, minX, maxX), minY);
        }

        return new Vector2(maxX, Mathf.Clamp(anchoredPosition.y, minY, maxY));
    }

    private void ApplyUiVariantStyle(string variant, int inputFontSize, float actionButtonTextSize, float actionSpacing, float captureWidth, float clearWidth, float recordWidth, float sendWidth)
    {
        if (panelBackgroundImage != null)
        {
            Color panelColor = new Color(0.06f, 0.09f, 0.14f, 0.94f);
            Color outlineColor = new Color(0.25f, 0.65f, 1f, 0.22f);
            Color shadowColor = new Color(0f, 0f, 0f, 0.35f);

            if (variant == "chat_card")
            {
                panelColor = new Color(0.09f, 0.14f, 0.2f, 0.96f);
                outlineColor = new Color(0.48f, 0.82f, 1f, 0.26f);
                shadowColor = new Color(0.01f, 0.04f, 0.08f, 0.42f);
            }
            else if (variant == "dense_console")
            {
                panelColor = new Color(0.03f, 0.06f, 0.1f, 0.97f);
                outlineColor = new Color(0.18f, 0.58f, 0.96f, 0.16f);
                shadowColor = new Color(0f, 0f, 0f, 0.28f);
            }
            else if (variant == "slim_sidebar")
            {
                panelColor = new Color(0.05f, 0.11f, 0.16f, 0.96f);
                outlineColor = new Color(0.24f, 0.74f, 1f, 0.18f);
                shadowColor = new Color(0f, 0f, 0f, 0.3f);
            }

            panelBackgroundImage.color = panelColor;

            if (panelOutlineEffect != null)
            {
                panelOutlineEffect.effectColor = outlineColor;
            }

            if (panelShadowEffect != null)
            {
                panelShadowEffect.effectColor = shadowColor;
            }
        }

        if (conversationBackgroundImage != null)
        {
            Color conversationColor = new Color(1f, 1f, 1f, 0.08f);
            if (variant == "chat_card")
            {
                conversationColor = new Color(0.93f, 0.97f, 1f, 0.14f);
            }
            else if (variant == "dense_console")
            {
                conversationColor = new Color(0.82f, 0.9f, 1f, 0.07f);
            }
            else if (variant == "slim_sidebar")
            {
                conversationColor = new Color(0.9f, 0.96f, 1f, 0.09f);
            }

            conversationBackgroundImage.color = conversationColor;
        }

        if (questionInput != null)
        {
            Image questionBackground = questionInput.GetComponent<Image>();
            Outline questionOutline = questionInput.GetComponent<Outline>();
            if (questionBackground != null)
            {
                Color inputColor = new Color(1f, 1f, 1f, 0.09f);
                if (variant == "chat_card")
                {
                    inputColor = new Color(0.94f, 0.98f, 1f, 0.12f);
                }
                else if (variant == "dense_console")
                {
                    inputColor = new Color(1f, 1f, 1f, 0.06f);
                }
                questionBackground.color = inputColor;
            }

            if (questionOutline != null)
            {
                questionOutline.effectColor = variant == "chat_card"
                    ? new Color(0.48f, 0.82f, 1f, 0.18f)
                    : new Color(1f, 1f, 1f, 0.08f);
            }

            if (questionInput.textComponent != null)
            {
                questionInput.textComponent.fontSize = inputFontSize;
            }

            Text placeholder = questionInput.placeholder as Text;
            if (placeholder != null)
            {
                placeholder.fontSize = inputFontSize;
            }
        }

        UpdateActionButtonStyle(captureScreenButton, captureWidth, actionButtonTextSize, GetVariantButtonColor(variant, 0));
        UpdateActionButtonStyle(clearImageButton, clearWidth, actionButtonTextSize, GetVariantButtonColor(variant, 1));
        UpdateActionButtonStyle(recordButton, recordWidth, actionButtonTextSize, GetVariantButtonColor(variant, 2));
        UpdateActionButtonStyle(sendButton, sendWidth, actionButtonTextSize, GetVariantButtonColor(variant, 3));
        LayoutActionButtons(variant, actionSpacing, captureWidth, clearWidth, recordWidth, sendWidth);
        ApplyIconVariantStyle(variant);
    }

    private void LayoutActionButtons(string variant, float spacing, float captureWidth, float clearWidth, float recordWidth, float sendWidth)
    {
        if (actionRowRect == null)
        {
            return;
        }

        RectTransform captureRect = captureScreenButton != null ? captureScreenButton.GetComponent<RectTransform>() : null;
        RectTransform clearRect = clearImageButton != null ? clearImageButton.GetComponent<RectTransform>() : null;
        RectTransform recordRect = recordButton != null ? recordButton.GetComponent<RectTransform>() : null;
        RectTransform sendRect = sendButton != null ? sendButton.GetComponent<RectTransform>() : null;

        if (variant == "slim_sidebar")
        {
            float contentWidth = Mathf.Max(200f, panelRoot != null ? panelRoot.sizeDelta.x - 20f : 220f);
            float cellWidth = (contentWidth - spacing) * 0.5f;
            float cellHeight = 26f;

            SetButtonAnchoredRect(captureRect, 0f, 0f, cellWidth, cellHeight);
            SetButtonAnchoredRect(clearRect, cellWidth + spacing, 0f, cellWidth, cellHeight);
            SetButtonAnchoredRect(recordRect, 0f, cellHeight + spacing, cellWidth, cellHeight);
            SetButtonAnchoredRect(sendRect, cellWidth + spacing, cellHeight + spacing, cellWidth, cellHeight);
            return;
        }

        if (variant == "dense_console")
        {
            float width = actionRowRect != null ? actionRowRect.sizeDelta.x : 66f;
            float cellHeight = 33f;
            SetButtonAnchoredRect(captureRect, 0f, 0f, width, cellHeight);
            SetButtonAnchoredRect(clearRect, 0f, cellHeight + spacing, width, cellHeight);
            SetButtonAnchoredRect(recordRect, 0f, (cellHeight + spacing) * 2f, width, cellHeight);
            SetButtonAnchoredRect(sendRect, 0f, (cellHeight + spacing) * 3f, width, cellHeight);
            return;
        }

        float x = 0f;
        SetButtonAnchoredRect(captureRect, x, 0f, captureWidth, actionRowRect.sizeDelta.y);
        x += captureWidth + spacing;
        SetButtonAnchoredRect(clearRect, x, 0f, clearWidth, actionRowRect.sizeDelta.y);
        x += clearWidth + spacing;
        SetButtonAnchoredRect(recordRect, x, 0f, recordWidth, actionRowRect.sizeDelta.y);
        x += recordWidth + spacing;
        SetButtonAnchoredRect(sendRect, x, 0f, sendWidth, actionRowRect.sizeDelta.y);
    }

    private void SetButtonAnchoredRect(RectTransform rect, float left, float top, float width, float height)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(left, -top);
    }

    private void ApplyIconVariantStyle(string variant)
    {
        float iconSize = 132f;
        float glowSize = 96f;
        float waveSize = 88f;
        float coreSize = 72f;
        Color glowColor = new Color(0.23f, 0.77f, 1f, 0.22f);
        Color waveColor = new Color(0.34f, 0.86f, 1f, 0.16f);
        Color outlineColor = new Color(0.47f, 0.87f, 1f, 0.65f);

        if (variant == "chat_card")
        {
            iconSize = 152f;
            glowSize = 114f;
            waveSize = 104f;
            coreSize = 82f;
            glowColor = new Color(0.52f, 0.84f, 1f, 0.28f);
            waveColor = new Color(0.65f, 0.9f, 1f, 0.18f);
            outlineColor = new Color(0.7f, 0.92f, 1f, 0.72f);
        }
        else if (variant == "dense_console")
        {
            iconSize = 108f;
            glowSize = 78f;
            waveSize = 72f;
            coreSize = 58f;
            glowColor = new Color(0.16f, 0.66f, 1f, 0.18f);
            waveColor = new Color(0.2f, 0.78f, 1f, 0.12f);
            outlineColor = new Color(0.34f, 0.77f, 1f, 0.58f);
        }
        else if (variant == "slim_sidebar")
        {
            iconSize = 118f;
            glowSize = 84f;
            waveSize = 76f;
            coreSize = 62f;
            glowColor = new Color(0.12f, 0.72f, 1f, 0.16f);
            waveColor = new Color(0.18f, 0.82f, 1f, 0.1f);
            outlineColor = new Color(0.3f, 0.83f, 1f, 0.5f);
        }

        if (iconRoot != null)
        {
            iconRoot.sizeDelta = new Vector2(iconSize, iconSize);
        }

        if (iconGlowImage != null)
        {
            iconGlowImage.rectTransform.sizeDelta = new Vector2(glowSize, glowSize);
            iconGlowImage.color = glowColor;
        }

        if (iconWaveImage != null)
        {
            iconWaveImage.rectTransform.sizeDelta = new Vector2(waveSize, waveSize);
            iconWaveImage.color = waveColor;
        }

        if (iconCoreRect != null && !usePhotoAvatar)
        {
            iconCoreRect.sizeDelta = new Vector2(coreSize, coreSize);
        }

        if (usePhotoAvatar && iconAvatarImage != null && iconAvatarImage.gameObject.activeSelf)
        {
            float photoSize = Mathf.Max(72f, iconSize - 12f);
            iconAvatarImage.rectTransform.sizeDelta = new Vector2(photoSize, photoSize);
            if (iconAvatarOutline != null)
            {
                iconAvatarOutline.effectColor = outlineColor;
            }
        }

        if (usePhotoAvatar && iconFreeformAvatarImage != null && iconFreeformAvatarImage.gameObject.activeSelf)
        {
            float freeformSize = Mathf.Max(72f, iconSize - 4f);
            iconFreeformAvatarImage.rectTransform.sizeDelta = new Vector2(freeformSize, freeformSize);
            if (iconFreeformAvatarOutline != null)
            {
                iconFreeformAvatarOutline.effectColor = outlineColor;
            }
        }
    }

    private void UpdateActionButtonStyle(Button button, float width, float fontSize, Color color)
    {
        if (button == null)
        {
            return;
        }

        LayoutElement element = button.GetComponent<LayoutElement>();
        if (element != null)
        {
            element.preferredWidth = width;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }

        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.fontSize = Mathf.RoundToInt(fontSize);
        }
    }

    private Sprite GetBuiltinRoundSprite()
    {
        if (generatedRoundSprite != null)
        {
            return generatedRoundSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.name = "WenxinAssistantRoundSprite";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        float radius = size * 0.5f - 1.5f;
        float feather = 1.75f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((radius - distance) / feather);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, false);
        generatedRoundSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return generatedRoundSprite;
    }

    private Color GetVariantButtonColor(string variant, int buttonIndex)
    {
        if (variant == "chat_card")
        {
            if (buttonIndex == 0) return new Color(0.49f, 0.34f, 0.82f, 1f);
            if (buttonIndex == 1) return new Color(0.42f, 0.48f, 0.58f, 1f);
            if (buttonIndex == 2) return new Color(0.12f, 0.68f, 0.52f, 1f);
            return new Color(0.15f, 0.56f, 0.96f, 1f);
        }

        if (variant == "dense_console")
        {
            if (buttonIndex == 0) return new Color(0.46f, 0.18f, 0.56f, 1f);
            if (buttonIndex == 1) return new Color(0.25f, 0.31f, 0.39f, 1f);
            if (buttonIndex == 2) return new Color(0.08f, 0.49f, 0.35f, 1f);
            return new Color(0.09f, 0.43f, 0.76f, 1f);
        }

        if (variant == "slim_sidebar")
        {
            if (buttonIndex == 0) return new Color(0.5f, 0.21f, 0.63f, 1f);
            if (buttonIndex == 1) return new Color(0.31f, 0.37f, 0.44f, 1f);
            if (buttonIndex == 2) return new Color(0.1f, 0.56f, 0.4f, 1f);
            return new Color(0.1f, 0.47f, 0.82f, 1f);
        }

        if (buttonIndex == 0) return new Color(0.58f, 0.21f, 0.68f, 1f);
        if (buttonIndex == 1) return new Color(0.34f, 0.39f, 0.48f, 1f);
        if (buttonIndex == 2) return new Color(0.11f, 0.62f, 0.43f, 1f);
        return new Color(0.11f, 0.49f, 0.86f, 1f);
    }

    private bool ShouldShowExperimentType()
    {
        return config == null || (config.showAssistantExperimentType && !string.IsNullOrEmpty(config.assistantExperimentType));
    }

    private string GetHeaderTitle()
    {
        return ShouldShowAssistantName() ? config.assistantName : "";
    }

    private string GetAssistantSpeakerLabel()
    {
        return ShouldShowAssistantName() ? config.assistantName : "";
    }

    public void EnableSceneHierarchyLayout()
    {
        useSceneHierarchyLayout = true;
        preserveSceneLayout = true;
    }

#if UNITY_EDITOR
    [ContextMenu("Wenxin Assistant/Rebuild Editable Hierarchy")]
    private void RebuildEditableHierarchyContextMenu()
    {
        BuildEditableHierarchyInEditor();
    }

    [ContextMenu("Wenxin Assistant/Capture Current Layout")]
    private void CaptureCurrentLayoutContextMenu()
    {
        CaptureAuthoredLayoutPositions();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void BuildEditableHierarchyInEditor()
    {
        useSceneHierarchyLayout = true;
        preserveSceneLayout = true;
        config = WenxinAssistantConfig.Load();

        ClearEditableChildrenImmediate();
        ResetUiReferences();
        ResetMovementCaches();

        forceAutoLayoutPass = true;
        BuildUi();
        forceAutoLayoutPass = false;
        uiBoundFromSceneHierarchy = true;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.blocksRaycasts = true;
            panelCanvasGroup.interactable = true;
        }

        isPanelVisible = true;
        panelSlideProgress = 1f;

        if (panelRoot != null)
        {
            panelRoot.anchoredPosition = GetPanelShownPosition();
        }

        CaptureAuthoredLayoutPositions();

        UnityEditor.EditorUtility.SetDirty(gameObject);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private void ClearEditableChildrenImmediate()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
#endif

    private class WenxinAssistantIconInteractionProxy : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Action clickCallback;
        private Action<PointerEventData> beginDragCallback;
        private Action<PointerEventData> dragCallback;
        private Action<PointerEventData> endDragCallback;
        private bool dragHappened;

        public void Initialize(
            Action onClick,
            Action<PointerEventData> onBeginDrag,
            Action<PointerEventData> onDrag,
            Action<PointerEventData> onEndDrag)
        {
            clickCallback = onClick;
            beginDragCallback = onBeginDrag;
            dragCallback = onDrag;
            endDragCallback = onEndDrag;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (dragHappened)
            {
                dragHappened = false;
                return;
            }

            if (clickCallback != null)
            {
                clickCallback();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragHappened = true;
            if (beginDragCallback != null)
            {
                beginDragCallback(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragCallback != null)
            {
                dragCallback(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (endDragCallback != null)
            {
                endDragCallback(eventData);
            }
        }
    }

    private class WenxinAssistantPanelDragProxy : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Action<PointerEventData> beginDragCallback;
        private Action<PointerEventData> dragCallback;
        private Action<PointerEventData> endDragCallback;

        public void Initialize(
            Action<PointerEventData> onBeginDrag,
            Action<PointerEventData> onDrag,
            Action<PointerEventData> onEndDrag)
        {
            beginDragCallback = onBeginDrag;
            dragCallback = onDrag;
            endDragCallback = onEndDrag;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (beginDragCallback != null)
            {
                beginDragCallback(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragCallback != null)
            {
                dragCallback(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (endDragCallback != null)
            {
                endDragCallback(eventData);
            }
        }
    }

    private void FinishBusy()
    {
        isBusy = false;
        SetButtonsInteractable(true);
        if (!isRecording)
        {
            UpdateRecordButtonVisual(false);
        }
    }

    private void UpdateRecordButtonVisual(bool recording)
    {
        if (recordButton == null)
        {
            return;
        }

        Text label = recordButton.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = "";
        }

        Image image = recordButton.GetComponent<Image>();
        if (image != null)
        {
            image.color = recording
                ? new Color(0.76f, 0.2f, 0.24f, 1f)
                : new Color(0.11f, 0.62f, 0.43f, 1f);
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (sendButton != null) sendButton.interactable = interactable;
        if (recordButton != null) recordButton.interactable = interactable;
        if (clearImageButton != null) clearImageButton.interactable = interactable;
        if (captureScreenButton != null) captureScreenButton.interactable = interactable;
    }

    private bool HasRequestError(UnityWebRequest request)
    {
#if UNITY_2020_2_OR_NEWER
        return request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError;
#else
        return request.isNetworkError || request.isHttpError;
#endif
    }

    [Serializable]
    private class AppConversationCreateRequest
    {
        public string app_id;
    }

    [Serializable]
    private class AppConversationCreateResponse
    {
        public string request_id;
        public string conversation_id;
    }

    [Serializable]
    private class AppConversationRunRequest
    {
        public string app_id;
        public string query;
        public bool stream;
        public string conversation_id;
        public string[] file_ids;
    }

    [Serializable]
    private class AppConversationRunResponse
    {
        public string request_id;
        public string answer;
        public string conversation_id;
        public string message_id;
    }

    [Serializable]
    private class SpeechRecognitionRequest
    {
        public string format;
        public int rate;
        public int dev_pid;
        public int channel;
        public string cuid;
        public int len;
        public string speech;
    }

    [Serializable]
    private class SpeechRecognitionResponse
    {
        public string[] result;
        public int err_no;
        public string err_msg;
    }

    [Serializable]
    private class VisionRequest
    {
        public string model;
        public VisionMessage[] messages;
    }

    [Serializable]
    private class VisionMessage
    {
        public string role;
        public VisionContentItem[] content;
    }

    [Serializable]
    private class VisionContentItem
    {
        public string type;
        public string text;
        public VisionImageUrl image_url;
    }

    [Serializable]
    private class VisionImageUrl
    {
        public string url;
    }

    [Serializable]
    private class VisionResponse
    {
        public VisionChoice[] choices;
    }

    [Serializable]
    private class VisionChoice
    {
        public VisionMessageResult message;
    }

    [Serializable]
    private class VisionMessageResult
    {
        public string content;
    }
}
