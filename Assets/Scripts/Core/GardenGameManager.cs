using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZhuozhengYuan
{
    public class GardenGameManager : MonoBehaviour
    {
        private struct RouteSampleNode
        {
            public bool isValid;
            public Vector3 position;
            public Collider groundCollider;
        }

        private static readonly Vector2Int[] RouteNeighborOffsets =
        {
            new Vector2Int(-1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 1),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };

        private static readonly RaycastHit[] RouteRaycastBuffer = new RaycastHit[64];
        private static readonly Collider[] RouteOverlapBuffer = new Collider[32];

        public StarterAssetsThirdPersonBridge playerController;
        public StarterAssetsThirdPersonBridge playerViewModeController;
        public PlayerInteractor playerInteractor;
        public PrototypeRuntimeUI runtimeUI;
        public Chapter01CanvasUI chapter01CanvasUI;
        public IntroSequenceController introController;
        public Chapter01Director chapter01Director;
        public Chapter02Director chapter02Director;
        public Chapter01AuthoredRouteGuide chapter01RouteGuide;
        public GardenModelHiddenCollisionBuilder hiddenCollisionBuilder;
        public bool createStartAreaGroundIfNeeded = true;
        public Vector2 startAreaGroundSize = new Vector2(260f, 260f);
        public float startAreaGroundThickness = 0.6f;
        public float startAreaGroundSurfaceOffset = -0.02f;
        public bool createChapter01AreaGroundIfNeeded = true;
        public Vector2 chapter01AreaGroundPadding = new Vector2(70f, 55f);
        public float chapter01AreaGroundThickness = 0.6f;
        public float chapter01AreaGroundSurfaceOffset = -0.02f;
        public bool createChapter01RouteGroundIfNeeded = true;
        public int chapter01RouteSegmentCount = 20;
        public float chapter01RouteWidth = 120f;
        public float chapter01RouteThickness = 0.6f;
        public float chapter01RouteSurfaceOffset = -0.02f;
        public float chapter01RouteGridSpacing = 24f;
        public float chapter01RouteSearchHalfWidth = 360f;
        public float chapter01RouteSearchStartPadding = 30f;
        public float chapter01RouteSearchEndPadding = 80f;
        public float chapter01RouteVerticalSearchPadding = 120f;
        public float chapter01RouteMaxSlopeAngle = 42f;
        public float chapter01RouteMaxStepHeight = 10f;
        public float chapter01RouteHeadroomHeight = 1.7f;
        public float chapter01RouteHeadroomRadius = 0.35f;
        public float chapter01RouteObstacleCheckHeight = 0.95f;
        public float chapter01RouteEdgeSampleSpacing = 10f;
        public float chapter01RouteSupportWidthMax = 42f;
        public bool createChapter01RouteGuideIfNeeded = false;
        public float chapter01RouteGuideWidth = 4f;
        public float chapter01RouteGuideThickness = 0.08f;
        public float chapter01RouteGuideElevation = 0.05f;
        public Color chapter01RouteGuideColor = new Color(1f, 0.86f, 0.35f, 0.95f);
        public bool createChapter01GateGuideIfNeeded = false;
        public float chapter01GateGuideRadius = 2.2f;
        public float chapter01GateGuideThickness = 0.08f;
        public float chapter01GateGuideElevation = 0.08f;
        public string[] chapter01BlockedSurfaceKeywords =
        {
            "water",
            "pool",
            "lake",
            "roof",
            "standingseam",
            "gaf",
            "tile_navy",
            "屋面",
            "水"
        };
        public string walkableGroundRootName = "WalkableGroundRoot";
        public int totalPages = 5;
        public bool autoPlayIntro = false;

        public static GardenGameManager Instance { get; private set; }

        public SaveData CurrentSaveData { get; private set; }

        public bool CanPlayerInteract
        {
            get
            {
                return !_introActive
                    && !_dialogueActive
                    && !_directionChoiceActive
                    && !_chapter02QuizActive
                    && (chapter01Director == null || !chapter01Director.HasActiveGatePuzzle);
            }
        }

        private bool _introActive;
        private bool _dialogueActive;
        private bool _directionChoiceActive;
        private bool _chapter02QuizActive;
        private IChapter01RuntimeUIPresenter _chapter01Presenter;
        private IChapter02QuizPresenter _chapter02QuizPresenter;
        private bool _chapter01RouteResolved;
        private List<Vector3> _chapter01ResolvedRoutePath;
        private Transform _chapter01ResolvedLeftGate;
        private Transform _chapter01ResolvedRightGate;
        private readonly Dictionary<int, int[]> _meshSubMeshTriangleLimits = new Dictionary<int, int[]>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            createChapter01RouteGuideIfNeeded = false;
            createChapter01GateGuideIfNeeded = false;
            CurrentSaveData = SaveSystem.Load();
            EnsureHiddenModelColliders();

            if (runtimeUI != null)
            {
                runtimeUI.gameManager = this;
            }

            _chapter01Presenter = ResolveChapter01Presenter();
            if (chapter01CanvasUI != null)
            {
                chapter01CanvasUI.gameManager = this;
            }
            if (runtimeUI != null)
            {
                runtimeUI.suppressChapter01Overlay = _chapter01Presenter != null && !ReferenceEquals(_chapter01Presenter, runtimeUI);
            }
            if (_chapter01Presenter != null)
            {
                _chapter01Presenter.SetPageCount(CurrentSaveData.collectedPages, totalPages);
                _chapter01Presenter.SetFadeAlpha(ShouldPlayIntroOnStart() ? 1f : 0f);
            }

            _chapter02QuizPresenter = ResolveChapter02QuizPresenter();

            if (playerInteractor != null)
            {
                playerInteractor.gameManager = this;
            }

            EnsurePlayerViewModeController();

            if (introController != null)
            {
                introController.manager = this;
                introController.PrepareRuntimeSupports();
            }

            EnsureStartAreaGroundIfNeeded();
            EnsureChapter01AreaGroundIfNeeded();
            EnsureChapter01RouteGroundIfNeeded();
            EnsureChapter01RouteGuideIfNeeded();

            if (chapter01Director != null)
            {
                chapter01Director.Initialize(this, CurrentSaveData);
            }

            if (chapter02Director == null)
            {
                chapter02Director = FindObjectOfType<Chapter02Director>();
            }

            if (chapter02Director != null)
            {
                chapter02Director.Initialize(this, CurrentSaveData);
            }

            EnsureChapter01RouteGuide();
            if (chapter01RouteGuide != null)
            {
                chapter01RouteGuide.Initialize(this, chapter01Director, introController);
            }

            RefreshPlayerRuntimeState();
        }

        private void Start()
        {
            if (ShouldPlayIntroOnStart())
            {
                StartCoroutine(BeginIntroRoutine());
                return;
            }

            _chapter01Presenter?.SetFadeAlpha(0f);

            if (introController != null)
            {
                introController.SnapPlayerToPostPoseIfAvailable();
            }

            CompleteIntro(true);
        }

        public void SetInteractionPrompt(string prompt)
        {
            _chapter01Presenter?.SetInteractionPrompt(prompt);
        }

        public void SetObjective(string objective)
        {
            _chapter01Presenter?.SetObjective(objective);
        }

        public void SetChapter02Objective(string objective)
        {
            SetObjective(objective);
        }

        public void ShowToast(string message, float duration = 2.2f)
        {
            _chapter01Presenter?.ShowToast(message, duration);
        }

        public void ShowDirectionResult(string title, string message, Color accentColor, float duration = 2.6f)
        {
            _chapter01Presenter?.ShowDirectionResult(title, message, accentColor, duration);
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

        public void ShowChapter02Quiz(string title, string progressText, string questionText, string[] options, Action<int> onSelected)
        {
            if (_chapter02QuizPresenter == null)
            {
                _chapter02QuizPresenter = ResolveChapter02QuizPresenter();
            }

            if (_chapter02QuizPresenter == null)
            {
                Debug.LogWarning("GardenGameManager.ShowChapter02Quiz was called without a Chapter02 quiz presenter.");
                return;
            }

            _chapter02QuizPresenter.ShowChapter02Quiz(title, progressText, questionText, options, onSelected);
        }

        public void HideChapter02Quiz()
        {
            if (_chapter02QuizPresenter == null)
            {
                _chapter02QuizPresenter = ResolveChapter02QuizPresenter();
            }

            if (_chapter02QuizPresenter != null)
            {
                _chapter02QuizPresenter.HideChapter02Quiz();
            }
        }

        public void ShowGateCalibration(Chapter01GateCalibrationViewData data)
        {
            _chapter01Presenter?.ShowGateCalibration(data);
        }

        public void HideGateCalibration()
        {
            _chapter01Presenter?.HideGateCalibration();
        }

        public void AddCollectedPages(int amount)
        {
            CurrentSaveData.collectedPages = Mathf.Clamp(CurrentSaveData.collectedPages + amount, 0, totalPages);

            _chapter01Presenter?.SetPageCount(CurrentSaveData.collectedPages, totalPages);
        }

        public void MarkIntroPlayed()
        {
            CurrentSaveData.introPlayed = true;
            SaveProgress();
        }

        public void CompleteIntro(bool skipSave = false)
        {
            _introActive = false;
            RefreshPlayerRuntimeState();

            if (chapter01RouteGuide != null)
            {
                chapter01RouteGuide.RebuildGuide();
            }

            if (!skipSave)
            {
                SaveProgress();
            }

            if (chapter01Director != null)
            {
                chapter01Director.OnIntroFinished();
            }

            if (chapter02Director != null)
            {
                chapter02Director.OnIntroFinished();
            }
        }

        public void SetDialogueActive(bool isActive)
        {
            _dialogueActive = isActive;
            RefreshPlayerRuntimeState();
        }

        public void SetDirectionChoiceActive(bool isActive)
        {
            _directionChoiceActive = isActive;
            RefreshPlayerRuntimeState();
        }

        public void SetChapter02QuizActive(bool isActive)
        {
            _chapter02QuizActive = isActive;
            RefreshPlayerRuntimeState();
        }

        public void SaveProgress()
        {
            SaveSystem.Save(CurrentSaveData);
        }

        public bool TryGetResolvedChapter01RoutePathCopy(out List<Vector3> routePath, out Transform leftGate, out Transform rightGate)
        {
            if (TryResolveChapter01RoutePath(out List<Vector3> resolvedRoutePath, out leftGate, out rightGate))
            {
                routePath = new List<Vector3>(resolvedRoutePath);
                return true;
            }

            routePath = null;
            leftGate = null;
            rightGate = null;
            return false;
        }

        private IEnumerator BeginIntroRoutine()
        {
            _introActive = true;
            RefreshPlayerRuntimeState();

            if (runtimeUI != null)
            {
                runtimeUI.gameManager = this;
            }

            _chapter01Presenter?.SetPageCount(CurrentSaveData.collectedPages, totalPages);

            yield return null;

            if (introController != null)
            {
                yield return introController.PlayIntroSequence();
            }
        }

        private void RefreshPlayerRuntimeState()
        {
            bool gameplayAllowed = !_introActive && !_dialogueActive && !_directionChoiceActive && !_chapter02QuizActive;

            if (playerViewModeController != null)
            {
                playerViewModeController.SetControlLocked(!gameplayAllowed);
                playerViewModeController.SetCursorForGameplay(gameplayAllowed);
                return;
            }

            if (playerController != null)
            {
                playerController.SetControlLocked(!gameplayAllowed);
                playerController.SetCursorForGameplay(gameplayAllowed);
            }
        }

        private void EnsurePlayerViewModeController()
        {
            if (playerController == null)
            {
                playerController = FindObjectOfType<StarterAssetsThirdPersonBridge>();
            }

            if (playerController == null)
            {
                return;
            }

            playerViewModeController = playerController;

            if (playerInteractor != null)
            {
                playerInteractor.viewModeController = playerViewModeController;
                if (playerInteractor.playerCamera == null)
                {
                    playerInteractor.playerCamera = playerController.ActiveCamera;
                }
            }
        }

        private IChapter02QuizPresenter ResolveChapter02QuizPresenter()
        {
            if (runtimeUI is IChapter02QuizPresenter runtimeQuizPresenter)
            {
                return runtimeQuizPresenter;
            }

            MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IChapter02QuizPresenter presenter)
                {
                    return presenter;
                }
            }

            return null;
        }

        private IChapter01RuntimeUIPresenter ResolveChapter01Presenter()
        {
            if (chapter01CanvasUI == null)
            {
                chapter01CanvasUI = FindObjectOfType<Chapter01CanvasUI>(true);
            }

            if (chapter01CanvasUI == null)
            {
                chapter01CanvasUI = Chapter01CanvasUI.CreateDefault();
            }

            return chapter01CanvasUI != null
                ? chapter01CanvasUI
                : runtimeUI as IChapter01RuntimeUIPresenter;
        }

        private void EnsureChapter01RouteGuide()
        {
            if (chapter01RouteGuide == null)
            {
                chapter01RouteGuide = GetComponent<Chapter01AuthoredRouteGuide>();
            }

            if (chapter01RouteGuide == null)
            {
                chapter01RouteGuide = gameObject.AddComponent<Chapter01AuthoredRouteGuide>();
            }
        }

        private void EnsureHiddenModelColliders()
        {
            if (hiddenCollisionBuilder == null)
            {
                hiddenCollisionBuilder = GetComponent<GardenModelHiddenCollisionBuilder>();
            }

            if (hiddenCollisionBuilder == null)
            {
                hiddenCollisionBuilder = gameObject.AddComponent<GardenModelHiddenCollisionBuilder>();
            }

            hiddenCollisionBuilder.EnsureHiddenColliders();
        }

        private void EnsureStartAreaGroundIfNeeded()
        {
            if (!createStartAreaGroundIfNeeded || introController == null || introController.playerPostIntroPose == null)
            {
                return;
            }

            Transform spawnPose = introController.playerPostIntroPose;
            Transform parent = ResolveWalkableGroundRoot();
            Transform existingGround = parent != null ? parent.Find("StartAreaHiddenGround") : null;
            GameObject supportObject = existingGround != null ? existingGround.gameObject : new GameObject("StartAreaHiddenGround");

            if (parent != null && supportObject.transform.parent != parent)
            {
                supportObject.transform.SetParent(parent, false);
            }

            float thickness = Mathf.Max(0.1f, startAreaGroundThickness);
            float bottomOffset = GetCharacterBottomOffset();
            float topY = spawnPose.position.y + startAreaGroundSurfaceOffset - bottomOffset;
            float width = Mathf.Max(2f, startAreaGroundSize.x);
            float length = Mathf.Max(2f, startAreaGroundSize.y);
            Vector3 supportCenter = new Vector3(spawnPose.position.x, topY - thickness * 0.5f, spawnPose.position.z);

            if (TryGetGardenModelBounds(out Bounds gardenBounds))
            {
                width = Mathf.Max(width, gardenBounds.size.x + 12f);
                length = Mathf.Max(length, gardenBounds.size.z + 12f);
                supportCenter = new Vector3(gardenBounds.center.x, topY - thickness * 0.5f, gardenBounds.center.z);
            }

            supportObject.transform.position = supportCenter;

            BoxCollider collider = supportObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = supportObject.AddComponent<BoxCollider>();
            }

            collider.size = new Vector3(width, thickness, length);
        }

        private bool TryGetGardenModelBounds(out Bounds gardenBounds)
        {
            gardenBounds = default;

            Transform gardenRoot = null;
            if (hiddenCollisionBuilder != null && hiddenCollisionBuilder.targetRoot != null)
            {
                gardenRoot = hiddenCollisionBuilder.targetRoot;
            }

            if (gardenRoot == null)
            {
                GameObject gardenObject = GameObject.Find("GardenModel");
                if (gardenObject != null)
                {
                    gardenRoot = gardenObject.transform;
                }
            }

            if (gardenRoot == null)
            {
                return false;
            }

            Renderer[] renderers = gardenRoot.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;

            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    gardenBounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                gardenBounds.Encapsulate(renderer.bounds);
            }

            return hasBounds;
        }

        private void EnsureChapter01AreaGroundIfNeeded()
        {
            if (!createChapter01AreaGroundIfNeeded || chapter01Director == null)
            {
                return;
            }

            Transform leftGate = chapter01Director.leftGate != null ? chapter01Director.leftGate.transform : null;
            Transform rightGate = chapter01Director.rightGate != null ? chapter01Director.rightGate.transform : null;
            Transform flowSelector = chapter01Director.flowSelector != null ? chapter01Director.flowSelector.transform : null;

            if (leftGate == null || rightGate == null || flowSelector == null)
            {
                return;
            }

            Vector3[] points =
            {
                leftGate.position,
                rightGate.position,
                flowSelector.position
            };

            float minX = points[0].x;
            float maxX = points[0].x;
            float minZ = points[0].z;
            float maxZ = points[0].z;
            float accumulatedY = 0f;

            for (int index = 0; index < points.Length; index++)
            {
                Vector3 point = points[index];
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
                accumulatedY += point.y;
            }

            float averageY = accumulatedY / points.Length;
            float width = Mathf.Max(12f, (maxX - minX) + chapter01AreaGroundPadding.x * 2f);
            float length = Mathf.Max(12f, (maxZ - minZ) + chapter01AreaGroundPadding.y * 2f);
            float thickness = Mathf.Max(0.1f, chapter01AreaGroundThickness);
            float topY = averageY + chapter01AreaGroundSurfaceOffset - GetCharacterBottomOffset();
            Vector3 center = new Vector3((minX + maxX) * 0.5f, topY - thickness * 0.5f, (minZ + maxZ) * 0.5f);

            Transform parent = ResolveWalkableGroundRoot();
            Transform existingGround = parent != null ? parent.Find("Chapter01HiddenGround") : null;
            GameObject supportObject = existingGround != null ? existingGround.gameObject : new GameObject("Chapter01HiddenGround");

            if (parent != null && supportObject.transform.parent != parent)
            {
                supportObject.transform.SetParent(parent, false);
            }

            supportObject.transform.position = center;

            BoxCollider collider = supportObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = supportObject.AddComponent<BoxCollider>();
            }

            collider.size = new Vector3(width, thickness, length);
        }

        private void EnsureChapter01RouteGroundIfNeeded()
        {
            if (!createChapter01RouteGroundIfNeeded)
            {
                return;
            }

            if (!TryResolveChapter01RoutePath(out List<Vector3> routePath, out _, out _))
            {
                return;
            }

            CreateRouteGroundSegments(routePath);
        }

        private void EnsureChapter01RouteGuideIfNeeded()
        {
            DestroyGuideRoot("Chapter01RouteGuideRoot");
            DestroyGuideRoot("Chapter01GateGuideRoot");
        }

        private void DestroyGuideRoot(string rootName)
        {
            if (string.IsNullOrEmpty(rootName))
            {
                return;
            }

            Transform parent = ResolveWalkableGroundRoot();
            Transform guideRoot = parent != null ? parent.Find(rootName) : null;
            if (guideRoot == null)
            {
                GameObject guideObject = GameObject.Find(rootName);
                guideRoot = guideObject != null ? guideObject.transform : null;
            }

            if (guideRoot != null)
            {
                Destroy(guideRoot.gameObject);
            }
        }

        private void CreateRouteGroundSegments(List<Vector3> routePath)
        {
            if (routePath == null || routePath.Count < 2)
            {
                return;
            }

            Transform parent = ResolveWalkableGroundRoot();
            Transform routeRoot = parent != null ? parent.Find("Chapter01RouteGroundRoot") : null;
            if (routeRoot == null)
            {
                GameObject routeRootObject = new GameObject("Chapter01RouteGroundRoot");
                routeRoot = routeRootObject.transform;
                if (parent != null)
                {
                    routeRoot.SetParent(parent, false);
                }
            }

            int segmentCount = routePath.Count - 1;
            float thickness = Mathf.Max(0.1f, chapter01RouteThickness);
            float width = Mathf.Clamp(chapter01RouteWidth, 6f, Mathf.Max(12f, chapter01RouteSupportWidthMax));
            float bottomOffset = GetCharacterBottomOffset();

            for (int index = 0; index < segmentCount; index++)
            {
                Vector3 segmentStart = routePath[index];
                Vector3 segmentEnd = routePath[index + 1];
                Vector3 flatSegmentDirection = segmentEnd - segmentStart;
                flatSegmentDirection.y = 0f;

                if (flatSegmentDirection.sqrMagnitude < 0.01f)
                {
                    continue;
                }

                float segmentLength = Mathf.Max(4f, flatSegmentDirection.magnitude + width * 0.35f);
                float topY = ((segmentStart.y + segmentEnd.y) * 0.5f) + chapter01RouteSurfaceOffset - bottomOffset;
                Vector3 segmentCenter = new Vector3(
                    (segmentStart.x + segmentEnd.x) * 0.5f,
                    topY - thickness * 0.5f,
                    (segmentStart.z + segmentEnd.z) * 0.5f);

                string segmentName = "RouteSegment_" + index.ToString("00");
                Transform existingSegment = routeRoot.Find(segmentName);
                GameObject segmentObject = existingSegment != null ? existingSegment.gameObject : new GameObject(segmentName);
                if (segmentObject.transform.parent != routeRoot)
                {
                    segmentObject.transform.SetParent(routeRoot, false);
                }

                segmentObject.transform.position = segmentCenter;
                segmentObject.transform.rotation = Quaternion.LookRotation(flatSegmentDirection.normalized, Vector3.up);

                BoxCollider collider = segmentObject.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = segmentObject.AddComponent<BoxCollider>();
                }

                collider.size = new Vector3(width, thickness, segmentLength);
            }

            for (int index = routeRoot.childCount - 1; index >= segmentCount; index--)
            {
                Transform extraChild = routeRoot.GetChild(index);
                if (extraChild != null)
                {
                    Destroy(extraChild.gameObject);
                }
            }
        }

        private void CreateRouteGuideStrips(List<Vector3> routePath)
        {
            if (routePath == null || routePath.Count < 2)
            {
                return;
            }

            Transform parent = ResolveWalkableGroundRoot();
            Transform guideRoot = parent != null ? parent.Find("Chapter01RouteGuideRoot") : null;
            if (guideRoot == null)
            {
                GameObject guideRootObject = new GameObject("Chapter01RouteGuideRoot");
                guideRoot = guideRootObject.transform;
                if (parent != null)
                {
                    guideRoot.SetParent(parent, false);
                }
            }

            int segmentCount = routePath.Count - 1;
            float thickness = Mathf.Max(0.02f, chapter01RouteGuideThickness);
            float width = Mathf.Max(0.5f, chapter01RouteGuideWidth);

            for (int index = 0; index < segmentCount; index++)
            {
                Vector3 segmentStart = routePath[index];
                Vector3 segmentEnd = routePath[index + 1];
                Vector3 flatSegmentDirection = segmentEnd - segmentStart;
                flatSegmentDirection.y = 0f;

                if (flatSegmentDirection.sqrMagnitude < 0.01f)
                {
                    continue;
                }

                float segmentLength = Mathf.Max(2f, flatSegmentDirection.magnitude + width * 0.25f);
                float routeSurfaceY = ((segmentStart.y + segmentEnd.y) * 0.5f) + chapter01RouteSurfaceOffset - GetCharacterBottomOffset();
                Vector3 segmentCenter = new Vector3(
                    (segmentStart.x + segmentEnd.x) * 0.5f,
                    routeSurfaceY + chapter01RouteGuideElevation + thickness * 0.5f,
                    (segmentStart.z + segmentEnd.z) * 0.5f);

                string segmentName = "GuideStrip_" + index.ToString("00");
                Transform existingSegment = guideRoot.Find(segmentName);
                GameObject segmentObject = existingSegment != null ? existingSegment.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
                segmentObject.name = segmentName;
                if (segmentObject.transform.parent != guideRoot)
                {
                    segmentObject.transform.SetParent(guideRoot, false);
                }

                Collider existingCollider = segmentObject.GetComponent<Collider>();
                if (existingCollider != null)
                {
                    Destroy(existingCollider);
                }

                segmentObject.transform.position = segmentCenter;
                segmentObject.transform.rotation = Quaternion.LookRotation(flatSegmentDirection.normalized, Vector3.up);
                segmentObject.transform.localScale = new Vector3(width, thickness, segmentLength);

                ApplyGuideVisual(segmentObject);
            }

            for (int index = guideRoot.childCount - 1; index >= segmentCount; index--)
            {
                Transform extraChild = guideRoot.GetChild(index);
                if (extraChild != null)
                {
                    Destroy(extraChild.gameObject);
                }
            }
        }

        private void CreateGateGuideMarkers(Transform leftGate, Transform rightGate)
        {
            Transform parent = ResolveWalkableGroundRoot();
            Transform guideRoot = parent != null ? parent.Find("Chapter01GateGuideRoot") : null;
            if (guideRoot == null)
            {
                GameObject guideRootObject = new GameObject("Chapter01GateGuideRoot");
                guideRoot = guideRootObject.transform;
                if (parent != null)
                {
                    guideRoot.SetParent(parent, false);
                }
            }

            CreateGateGuideMarker(guideRoot, "LeftGateGuide", leftGate);
            CreateGateGuideMarker(guideRoot, "RightGateGuide", rightGate);
        }

        private void CreateGateGuideMarker(Transform guideRoot, string objectName, Transform gateTransform)
        {
            if (guideRoot == null || gateTransform == null)
            {
                return;
            }

            Transform existingMarker = guideRoot.Find(objectName);
            GameObject markerObject = existingMarker != null ? existingMarker.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            markerObject.name = objectName;
            if (markerObject.transform.parent != guideRoot)
            {
                markerObject.transform.SetParent(guideRoot, false);
            }

            Collider existingCollider = markerObject.GetComponent<Collider>();
            if (existingCollider != null)
            {
                Destroy(existingCollider);
            }

            float thickness = Mathf.Max(0.02f, chapter01GateGuideThickness);
            float radius = Mathf.Max(0.4f, chapter01GateGuideRadius);
            float bottomOffset = GetCharacterBottomOffset();
            float topY = gateTransform.position.y + chapter01AreaGroundSurfaceOffset - bottomOffset;

            markerObject.transform.position = new Vector3(
                gateTransform.position.x,
                topY + chapter01GateGuideElevation + thickness * 0.5f,
                gateTransform.position.z);
            markerObject.transform.rotation = Quaternion.identity;
            markerObject.transform.localScale = new Vector3(radius, thickness * 0.5f, radius);

            ApplyGuideVisual(markerObject);
        }

        private void ApplyGuideVisual(GameObject guideObject)
        {
            if (guideObject == null)
            {
                return;
            }

            Renderer renderer = guideObject.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Material sharedMaterial = renderer.sharedMaterial;
            if (sharedMaterial == null || sharedMaterial.shader == null || sharedMaterial.name != "Chapter01RouteGuideMaterial")
            {
                Shader shader = Shader.Find("Unlit/Color");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                Material material = new Material(shader);
                material.name = "Chapter01RouteGuideMaterial";
                material.color = chapter01RouteGuideColor;
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", chapter01RouteGuideColor * 1.4f);
                }

                renderer.sharedMaterial = material;
                return;
            }

            sharedMaterial.color = chapter01RouteGuideColor;
            if (sharedMaterial.HasProperty("_EmissionColor"))
            {
                sharedMaterial.EnableKeyword("_EMISSION");
                sharedMaterial.SetColor("_EmissionColor", chapter01RouteGuideColor * 1.4f);
            }
        }

        private bool TryResolveChapter01RoutePath(out List<Vector3> routePath, out Transform leftGate, out Transform rightGate)
        {
            if (!_chapter01RouteResolved)
            {
                _chapter01RouteResolved = true;
                _chapter01ResolvedRoutePath = null;
                _chapter01ResolvedLeftGate = null;
                _chapter01ResolvedRightGate = null;

                if (TryGetChapter01RouteAnchors(out Vector3 start, out Vector3 gateCenter, out Transform resolvedLeftGate, out Transform resolvedRightGate, out _))
                {
                    _chapter01ResolvedLeftGate = resolvedLeftGate;
                    _chapter01ResolvedRightGate = resolvedRightGate;
                    if (TryFindLandRoute(start, gateCenter, out List<Vector3> resolvedPath))
                    {
                        _chapter01ResolvedRoutePath = resolvedPath;
                    }
                    else
                    {
                        Debug.LogWarning("GardenGameManager could not resolve a land-only route from the start point to the chapter 01 gate area.");
                    }
                }
            }

            routePath = _chapter01ResolvedRoutePath;
            leftGate = _chapter01ResolvedLeftGate;
            rightGate = _chapter01ResolvedRightGate;
            return routePath != null && routePath.Count >= 2;
        }

        private bool TryFindLandRoute(Vector3 start, Vector3 target, out List<Vector3> routePath)
        {
            routePath = null;

            Vector3 flatDirection = target - start;
            flatDirection.y = 0f;
            float flatDistance = flatDirection.magnitude;
            if (flatDistance < 1f)
            {
                return false;
            }

            Vector3 forward = flatDirection / flatDistance;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float gridSpacing = Mathf.Max(8f, chapter01RouteGridSpacing);
            float searchHalfWidth = Mathf.Max(gridSpacing * 4f, chapter01RouteSearchHalfWidth);
            float startPadding = Mathf.Max(gridSpacing, chapter01RouteSearchStartPadding);
            float endPadding = Mathf.Max(gridSpacing, chapter01RouteSearchEndPadding);
            float topY = Mathf.Max(start.y, target.y) + Mathf.Max(40f, chapter01RouteVerticalSearchPadding);
            float bottomY = Mathf.Min(start.y, target.y) - Mathf.Max(40f, chapter01RouteVerticalSearchPadding);

            Vector3 searchOrigin = new Vector3(start.x, 0f, start.z) - (forward * startPadding) - (right * searchHalfWidth);
            float searchLength = flatDistance + startPadding + endPadding;
            int columns = Mathf.Max(3, Mathf.CeilToInt(searchLength / gridSpacing) + 1);
            int rows = Mathf.Max(3, Mathf.CeilToInt((searchHalfWidth * 2f) / gridSpacing) + 1);
            RouteSampleNode[] nodes = new RouteSampleNode[rows * columns];

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    Vector3 sampleFlatPosition = searchOrigin + (forward * (column * gridSpacing)) + (right * (row * gridSpacing));
                    int nodeIndex = GetRouteNodeIndex(row, column, columns);
                    if (TrySampleWalkableRoutePoint(sampleFlatPosition, topY, bottomY, out Vector3 sampledPosition, out Collider groundCollider))
                    {
                        nodes[nodeIndex].isValid = true;
                        nodes[nodeIndex].position = sampledPosition;
                        nodes[nodeIndex].groundCollider = groundCollider;
                    }
                }
            }

            int startIndex = FindClosestValidRouteNode(nodes, rows, columns, start, gridSpacing * 3f);
            int targetIndex = FindClosestValidRouteNode(nodes, rows, columns, target, gridSpacing * 3f);
            if (startIndex < 0 || targetIndex < 0)
            {
                return false;
            }

            if (!RunRouteSearch(nodes, rows, columns, startIndex, targetIndex, topY, bottomY, out routePath))
            {
                return false;
            }

            if (routePath == null || routePath.Count == 0)
            {
                return false;
            }

            routePath[0] = new Vector3(start.x, routePath[0].y, start.z);
            routePath[routePath.Count - 1] = new Vector3(target.x, routePath[routePath.Count - 1].y, target.z);
            routePath = SimplifyRoutePath(routePath, topY, bottomY);
            return routePath.Count >= 2;
        }

        private bool TrySampleWalkableRoutePoint(Vector3 flatPosition, float topY, float bottomY, out Vector3 sampledPosition, out Collider groundCollider)
        {
            sampledPosition = Vector3.zero;
            groundCollider = null;

            float rayDistance = Mathf.Max(1f, topY - bottomY);
            Vector3 rayOrigin = new Vector3(flatPosition.x, topY, flatPosition.z);
            int hitCount = Physics.RaycastNonAlloc(rayOrigin, Vector3.down, RouteRaycastBuffer, rayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (hitCount <= 0)
            {
                return false;
            }

            SortRaycastHitsByDistance(RouteRaycastBuffer, hitCount);
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = RouteRaycastBuffer[index];
                if (!IsWalkableSurfaceHit(hit))
                {
                    continue;
                }

                if (!HasRouteHeadroom(hit.point, hit.collider))
                {
                    continue;
                }

                sampledPosition = hit.point;
                groundCollider = hit.collider;
                return true;
            }

            return false;
        }

        private bool IsWalkableSurfaceHit(RaycastHit hit)
        {
            Collider collider = hit.collider;
            if (collider == null || !collider.enabled || collider.isTrigger)
            {
                return false;
            }

            if (IsTransientRouteHelper(collider.transform))
            {
                return false;
            }

            if (Vector3.Angle(hit.normal, Vector3.up) > chapter01RouteMaxSlopeAngle)
            {
                return false;
            }

            if (SurfaceHasBlockedKeyword(hit))
            {
                return false;
            }

            return true;
        }

        private bool HasRouteHeadroom(Vector3 groundPoint, Collider groundCollider)
        {
            float controllerRadius = playerController != null && playerController.ActiveCharacterController != null
                ? playerController.ActiveCharacterController.radius
                : chapter01RouteHeadroomRadius;
            float controllerHeight = playerController != null && playerController.ActiveCharacterController != null
                ? playerController.ActiveCharacterController.height
                : chapter01RouteHeadroomHeight;

            float radius = Mathf.Max(0.2f, Mathf.Min(chapter01RouteHeadroomRadius, controllerRadius));
            float height = Mathf.Max(radius * 2f + 0.1f, Mathf.Max(chapter01RouteHeadroomHeight, controllerHeight * 0.9f));
            Vector3 capsuleStart = groundPoint + (Vector3.up * (radius + 0.05f));
            Vector3 capsuleEnd = groundPoint + (Vector3.up * Mathf.Max(radius + 0.05f, height - radius));
            int overlapCount = Physics.OverlapCapsuleNonAlloc(capsuleStart, capsuleEnd, radius, RouteOverlapBuffer, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            for (int index = 0; index < overlapCount; index++)
            {
                Collider overlapCollider = RouteOverlapBuffer[index];
                if (overlapCollider == null || !overlapCollider.enabled || overlapCollider.isTrigger)
                {
                    continue;
                }

                if (overlapCollider == groundCollider || IsSameTransformTree(overlapCollider.transform, groundCollider != null ? groundCollider.transform : null))
                {
                    continue;
                }

                if (IsTransientRouteHelper(overlapCollider.transform))
                {
                    continue;
                }

                if (overlapCollider.bounds.max.y <= groundPoint.y + 0.15f)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private bool RunRouteSearch(RouteSampleNode[] nodes, int rows, int columns, int startIndex, int targetIndex, float topY, float bottomY, out List<Vector3> routePath)
        {
            routePath = null;
            int nodeCount = nodes.Length;
            float[] gScores = new float[nodeCount];
            float[] fScores = new float[nodeCount];
            int[] cameFrom = new int[nodeCount];
            bool[] openSet = new bool[nodeCount];
            bool[] closedSet = new bool[nodeCount];
            List<int> openList = new List<int>(128);

            for (int index = 0; index < nodeCount; index++)
            {
                gScores[index] = float.PositiveInfinity;
                fScores[index] = float.PositiveInfinity;
                cameFrom[index] = -1;
            }

            gScores[startIndex] = 0f;
            fScores[startIndex] = EstimateRouteHeuristic(nodes[startIndex].position, nodes[targetIndex].position);
            openSet[startIndex] = true;
            openList.Add(startIndex);

            while (openList.Count > 0)
            {
                int currentIndex = GetLowestFScoreIndex(openList, fScores);
                if (currentIndex == targetIndex)
                {
                    routePath = ReconstructRoutePath(nodes, cameFrom, currentIndex);
                    return routePath != null && routePath.Count >= 2;
                }

                openList.Remove(currentIndex);
                openSet[currentIndex] = false;
                closedSet[currentIndex] = true;

                int currentRow = currentIndex / columns;
                int currentColumn = currentIndex % columns;

                for (int offsetIndex = 0; offsetIndex < RouteNeighborOffsets.Length; offsetIndex++)
                {
                    Vector2Int offset = RouteNeighborOffsets[offsetIndex];
                    int neighborRow = currentRow + offset.y;
                    int neighborColumn = currentColumn + offset.x;

                    if (neighborRow < 0 || neighborRow >= rows || neighborColumn < 0 || neighborColumn >= columns)
                    {
                        continue;
                    }

                    int neighborIndex = GetRouteNodeIndex(neighborRow, neighborColumn, columns);
                    if (closedSet[neighborIndex] || !nodes[neighborIndex].isValid)
                    {
                        continue;
                    }

                    if (!CanTraverseRouteEdge(nodes[currentIndex], nodes[neighborIndex], topY, bottomY))
                    {
                        continue;
                    }

                    float tentativeG = gScores[currentIndex] + Vector3.Distance(nodes[currentIndex].position, nodes[neighborIndex].position);
                    if (tentativeG >= gScores[neighborIndex])
                    {
                        continue;
                    }

                    cameFrom[neighborIndex] = currentIndex;
                    gScores[neighborIndex] = tentativeG;
                    fScores[neighborIndex] = tentativeG + EstimateRouteHeuristic(nodes[neighborIndex].position, nodes[targetIndex].position);

                    if (!openSet[neighborIndex])
                    {
                        openSet[neighborIndex] = true;
                        openList.Add(neighborIndex);
                    }
                }
            }

            return false;
        }

        private bool CanTraverseRouteEdge(RouteSampleNode fromNode, RouteSampleNode toNode, float topY, float bottomY)
        {
            if (!fromNode.isValid || !toNode.isValid)
            {
                return false;
            }

            if (Mathf.Abs(fromNode.position.y - toNode.position.y) > chapter01RouteMaxStepHeight)
            {
                return false;
            }

            float segmentDistance = Vector3.Distance(fromNode.position, toNode.position);
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(segmentDistance / Mathf.Max(2f, chapter01RouteEdgeSampleSpacing)));
            float previousY = fromNode.position.y;

            for (int sampleIndex = 1; sampleIndex < sampleCount; sampleIndex++)
            {
                float t = (float)sampleIndex / sampleCount;
                Vector3 sampleFlatPosition = Vector3.Lerp(fromNode.position, toNode.position, t);
                if (!TrySampleWalkableRoutePoint(sampleFlatPosition, topY, bottomY, out Vector3 samplePosition, out _))
                {
                    return false;
                }

                if (Mathf.Abs(samplePosition.y - previousY) > chapter01RouteMaxStepHeight)
                {
                    return false;
                }

                previousY = samplePosition.y;
            }

            return !SegmentHitsBlockingObstacle(fromNode.position, toNode.position);
        }

        private bool SegmentHitsBlockingObstacle(Vector3 start, Vector3 end)
        {
            Vector3 rayStart = start + (Vector3.up * chapter01RouteObstacleCheckHeight);
            Vector3 rayEnd = end + (Vector3.up * chapter01RouteObstacleCheckHeight);
            Vector3 direction = rayEnd - rayStart;
            float distance = direction.magnitude;
            if (distance <= 0.05f)
            {
                return false;
            }

            Ray ray = new Ray(rayStart, direction / distance);
            RaycastHit[] hits = Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int index = 0; index < hits.Length; index++)
            {
                RaycastHit hit = hits[index];
                Collider collider = hit.collider;
                if (collider == null || !collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                if (IsTransientRouteHelper(collider.transform))
                {
                    continue;
                }

                if (hit.normal.y > 0.45f && hit.point.y <= Mathf.Max(start.y, end.y) + 0.35f)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private List<Vector3> ReconstructRoutePath(RouteSampleNode[] nodes, int[] cameFrom, int currentIndex)
        {
            List<Vector3> reversedPath = new List<Vector3>();
            while (currentIndex >= 0)
            {
                reversedPath.Add(nodes[currentIndex].position);
                currentIndex = cameFrom[currentIndex];
            }

            reversedPath.Reverse();
            return reversedPath;
        }

        private List<Vector3> SimplifyRoutePath(List<Vector3> routePath, float topY, float bottomY)
        {
            if (routePath == null || routePath.Count <= 2)
            {
                return routePath;
            }

            List<Vector3> simplifiedPath = new List<Vector3>();
            int anchorIndex = 0;
            simplifiedPath.Add(routePath[anchorIndex]);

            while (anchorIndex < routePath.Count - 1)
            {
                int bestIndex = anchorIndex + 1;
                for (int candidateIndex = routePath.Count - 1; candidateIndex > anchorIndex + 1; candidateIndex--)
                {
                    RouteSampleNode fromNode = new RouteSampleNode
                    {
                        isValid = true,
                        position = routePath[anchorIndex]
                    };
                    RouteSampleNode toNode = new RouteSampleNode
                    {
                        isValid = true,
                        position = routePath[candidateIndex]
                    };

                    if (CanTraverseRouteEdge(fromNode, toNode, topY, bottomY))
                    {
                        bestIndex = candidateIndex;
                        break;
                    }
                }

                simplifiedPath.Add(routePath[bestIndex]);
                anchorIndex = bestIndex;
            }

            return simplifiedPath;
        }

        private int FindClosestValidRouteNode(RouteSampleNode[] nodes, int rows, int columns, Vector3 worldPoint, float maxDistance)
        {
            int closestIndex = -1;
            float closestDistance = maxDistance * maxDistance;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int nodeIndex = GetRouteNodeIndex(row, column, columns);
                    if (!nodes[nodeIndex].isValid)
                    {
                        continue;
                    }

                    Vector3 offset = nodes[nodeIndex].position - worldPoint;
                    offset.y = 0f;
                    float distanceSqr = offset.sqrMagnitude;
                    if (distanceSqr < closestDistance)
                    {
                        closestDistance = distanceSqr;
                        closestIndex = nodeIndex;
                    }
                }
            }

            return closestIndex;
        }

        private bool SurfaceHasBlockedKeyword(RaycastHit hit)
        {
            Collider collider = hit.collider;
            if (collider == null)
            {
                return false;
            }

            Material hitMaterial = TryResolveHitMaterial(hit);
            if (hitMaterial != null)
            {
                return MaterialNameHasBlockedKeyword(hitMaterial.name);
            }

            return false;
        }

        private Material TryResolveHitMaterial(RaycastHit hit)
        {
            Collider collider = hit.collider;
            if (collider == null)
            {
                return null;
            }

            Renderer renderer = collider.GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = collider.GetComponentInParent<Renderer>(true);
            }

            if (renderer == null)
            {
                return null;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                return null;
            }

            if (materials.Length == 1)
            {
                return materials[0];
            }

            MeshCollider meshCollider = collider as MeshCollider;
            if (meshCollider == null || meshCollider.sharedMesh == null || hit.triangleIndex < 0)
            {
                return materials[0];
            }

            int subMeshIndex = GetHitSubMeshIndex(meshCollider.sharedMesh, hit.triangleIndex);
            if (subMeshIndex >= 0 && subMeshIndex < materials.Length)
            {
                return materials[subMeshIndex];
            }

            return materials[0];
        }

        private int GetHitSubMeshIndex(Mesh mesh, int triangleIndex)
        {
            if (mesh == null || triangleIndex < 0)
            {
                return -1;
            }

            int meshId = mesh.GetInstanceID();
            int[] triangleLimits;
            if (!_meshSubMeshTriangleLimits.TryGetValue(meshId, out triangleLimits))
            {
                triangleLimits = new int[mesh.subMeshCount];
                int runningTriangleCount = 0;
                for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
                {
                    runningTriangleCount += (int)(mesh.GetIndexCount(subMeshIndex) / 3);
                    triangleLimits[subMeshIndex] = runningTriangleCount;
                }

                _meshSubMeshTriangleLimits[meshId] = triangleLimits;
            }

            for (int subMeshIndex = 0; subMeshIndex < triangleLimits.Length; subMeshIndex++)
            {
                if (triangleIndex < triangleLimits[subMeshIndex])
                {
                    return subMeshIndex;
                }
            }

            return -1;
        }

        private bool MaterialNameHasBlockedKeyword(string materialName)
        {
            if (string.IsNullOrEmpty(materialName))
            {
                return false;
            }

            string lowerName = materialName.ToLowerInvariant();
            for (int keywordIndex = 0; keywordIndex < chapter01BlockedSurfaceKeywords.Length; keywordIndex++)
            {
                string keyword = chapter01BlockedSurfaceKeywords[keywordIndex];
                if (string.IsNullOrEmpty(keyword))
                {
                    continue;
                }

                if (lowerName.Contains(keyword.ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsTransientRouteHelper(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            Transform current = target;
            while (current != null)
            {
                string objectName = current.name;
                if (string.Equals(objectName, "Chapter01RouteGroundRoot", StringComparison.Ordinal)
                    || string.Equals(objectName, "Chapter01RouteGuideRoot", StringComparison.Ordinal)
                    || string.Equals(objectName, "Chapter01GateGuideRoot", StringComparison.Ordinal)
                    || objectName.StartsWith("RouteSegment_", StringComparison.Ordinal)
                    || objectName.StartsWith("GuideStrip_", StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsSameTransformTree(Transform left, Transform right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left == right || left.IsChildOf(right) || right.IsChildOf(left);
        }

        private static int GetRouteNodeIndex(int row, int column, int columns)
        {
            return (row * columns) + column;
        }

        private static int GetLowestFScoreIndex(List<int> openList, float[] fScores)
        {
            int bestIndex = openList[0];
            float bestScore = fScores[bestIndex];

            for (int index = 1; index < openList.Count; index++)
            {
                int candidate = openList[index];
                float candidateScore = fScores[candidate];
                if (candidateScore < bestScore)
                {
                    bestScore = candidateScore;
                    bestIndex = candidate;
                }
            }

            return bestIndex;
        }

        private static float EstimateRouteHeuristic(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            return delta.magnitude;
        }

        private static void SortRaycastHitsByDistance(RaycastHit[] hits, int hitCount)
        {
            for (int outer = 0; outer < hitCount - 1; outer++)
            {
                int bestIndex = outer;
                float bestDistance = hits[outer].distance;
                for (int inner = outer + 1; inner < hitCount; inner++)
                {
                    if (hits[inner].distance < bestDistance)
                    {
                        bestDistance = hits[inner].distance;
                        bestIndex = inner;
                    }
                }

                if (bestIndex != outer)
                {
                    RaycastHit swap = hits[outer];
                    hits[outer] = hits[bestIndex];
                    hits[bestIndex] = swap;
                }
            }
        }

        private bool TryGetChapter01RouteAnchors(out Vector3 start, out Vector3 gateCenter, out Transform leftGate, out Transform rightGate, out Transform flowSelector)
        {
            start = Vector3.zero;
            gateCenter = Vector3.zero;
            leftGate = null;
            rightGate = null;
            flowSelector = null;

            if (introController == null || introController.playerPostIntroPose == null || chapter01Director == null)
            {
                return false;
            }

            leftGate = chapter01Director.leftGate != null ? chapter01Director.leftGate.transform : null;
            rightGate = chapter01Director.rightGate != null ? chapter01Director.rightGate.transform : null;
            flowSelector = chapter01Director.flowSelector != null ? chapter01Director.flowSelector.transform : null;

            if (leftGate == null || rightGate == null || flowSelector == null)
            {
                return false;
            }

            start = introController.playerPostIntroPose.position;
            gateCenter = (leftGate.position + rightGate.position) * 0.5f;
            return true;
        }

        private float GetCharacterBottomOffset()
        {
            CharacterController controller = null;
            if (playerViewModeController != null)
            {
                controller = playerViewModeController.ActiveCharacterController;
            }

            if (controller == null && playerController != null)
            {
                controller = playerController.ActiveCharacterController;
            }

            if (controller == null)
            {
                return 0f;
            }

            return controller.center.y - controller.height * 0.5f;
        }

        private Transform ResolveWalkableGroundRoot()
        {
            if (!string.IsNullOrEmpty(walkableGroundRootName))
            {
                GameObject existingRoot = GameObject.Find(walkableGroundRootName);
                if (existingRoot != null)
                {
                    return existingRoot.transform;
                }
            }

            return null;
        }

        private bool ShouldPlayIntroOnStart()
        {
            return autoPlayIntro
                && CurrentSaveData != null
                && !CurrentSaveData.introPlayed
                && introController != null;
        }
    }
}
