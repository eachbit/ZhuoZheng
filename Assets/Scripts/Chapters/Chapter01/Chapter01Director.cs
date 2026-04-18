using System;
using UnityEngine;

namespace ZhuozhengYuan
{
    public class Chapter01Director : MonoBehaviour
    {
        public GardenGameManager manager;
        public Chapter01EnvironmentController environmentController;
        public GateInteractable leftGate;
        public GateInteractable rightGate;
        public WaterDirectionInteractable flowSelector;
        public PagePickupInteractable pagePickup;
        public string correctDirection = Chapter01FlowDirection.Center;
        public string[] directionOptions = { "\u897f\u6e20", "\u5357\u6e20", "\u4e2d\u6c60" };

        public string objectiveOpenGates = "\u524d\u5f80\u8fdc\u9999\u5802\uff0c\u5f00\u542f\u5de6\u53f3\u4e24\u5904\u6697\u95f8\u3002";
        public string objectiveChooseFlow = "\u70b9\u51fb\u6c34\u95f8\u5e76\u9009\u62e9\u6c34\u6d41\u65b9\u5411\uff0c\u627e\u51fa\u80fd\u771f\u6b63\u5524\u9192\u6c34\u8109\u7684\u53bb\u5411\u3002";
        public string objectiveChooseFlowAfterWestRejected = "\u897f\u6e20\u5c3d\u5934\u53ea\u5269\u7a7a\u54cd\uff0c\u518d\u8bd5\u5357\u6e20\u6216\u4e2d\u6c60\u3002";
        public string objectiveChooseFlowAfterSouthRejected = "\u5357\u6e20\u867d\u6709\u52a8\u9759\uff0c\u5374\u6ca1\u6709\u5524\u9192\u6c34\u8109\uff0c\u518d\u8bd5\u897f\u6e20\u6216\u4e2d\u6c60\u3002";
        public string objectiveChooseFlowAfterAllRejected = "\u897f\u6e20\u3001\u5357\u6e20\u90fd\u4e0d\u901a\uff0c\u771f\u6b63\u7684\u6d3b\u6c34\u5e94\u56de\u5230\u4e2d\u6c60\u3002";
        public string objectiveCollectPage = "\u5728\u77f3\u7f1d\u4e2d\u62fe\u53d6\u300a\u957f\u7269\u5fd7\u300b\u6b8b\u9875\u3002";
        public string objectiveCompleted = "\u7b2c\u4e00\u7ae0\u5df2\u5b8c\u6210\uff0c\u53ef\u7ee7\u7eed\u524d\u884c\u3002";
        public string flowHintWestRejected = "\u897f\u6e20\u5c3d\u5934\u4f20\u6765\u7a7a\u54cd\uff0c\u50cf\u662f\u65ad\u5728\u6b7b\u8def\u91cc\u3002";
        public string flowHintSouthRejected = "\u5357\u6e20\u6709\u6c34\u58f0\u5374\u6ca1\u6709\u805a\u5230\u4e2d\u6c60\uff0c\u8fd9\u6761\u8def\u4e0d\u5bf9\u3002";
        public string flowHintAllRejected = "\u4e24\u6761\u65c1\u8def\u90fd\u4e0d\u901a\uff0c\u771f\u6b63\u7684\u6d3b\u6c34\u5e94\u56de\u5230\u4e2d\u6c60\u3002";
        public string flowHintCorrect = "\u4e2d\u6c60\u6df1\u5904\u4f20\u6765\u56de\u54cd\uff0c\u6c34\u8109\u88ab\u63a5\u901a\u4e86\u3002";
        public string chapter02RouteGuideObjectName = "Chapter01ToChapter02RouteGuide";
        public string chapter02RouteGuideRootName = "Chapter01ToChapter02GuidePath";
        public float chapter02RouteGuideReachedRadius = 4f;
        public int chapter02RouteGuideMaxDecorations = 6;
        public int chapter02RouteGuideAutoPointCount = 4;

        public KeyCode gateRotateNegativeKey = KeyCode.A;
        public KeyCode gateRotatePositiveKey = KeyCode.D;
        public KeyCode gateConfirmKey = KeyCode.E;
        public KeyCode gateCancelKey = KeyCode.Escape;

        public Chapter01State CurrentState { get; private set; }

        private const int WestRejectedMask = 1 << 0;
        private const int SouthRejectedMask = 1 << 1;

        public bool HasActiveGatePuzzle
        {
            get { return _activeGatePuzzle != null; }
        }

        private bool _leftGateOpened;
        private bool _rightGateOpened;
        private bool _pageCollected;
        private string _selectedDirection = string.Empty;
        private int _directionCycleIndex = -1;
        private int _rejectedDirectionMask;
        private GateInteractable _activeGatePuzzle;
        private Transform _chapter02RouteGuideRoot;

        private void Update()
        {
            if (_activeGatePuzzle == null)
            {
                return;
            }

            HandleGatePuzzleInput();
        }

        public void Initialize(GardenGameManager gameManager, SaveData saveData)
        {
            manager = gameManager;
            correctDirection = Chapter01FlowDirection.Center;
            directionOptions = Chapter01FlowDirection.CreateOptionLabels();

            if (leftGate != null)
            {
                leftGate.director = this;
            }

            if (rightGate != null)
            {
                rightGate.director = this;
            }

            if (flowSelector != null)
            {
                flowSelector.director = this;
            }

            if (pagePickup != null)
            {
                pagePickup.director = this;
            }

            ApplySaveState(saveData);
        }

        public bool IsGateSolved(GateId gateId)
        {
            return gateId == GateId.Left ? _leftGateOpened : _rightGateOpened;
        }

        public void ApplySaveState(SaveData saveData)
        {
            if (saveData == null)
            {
                saveData = SaveData.CreateDefault();
            }

            _leftGateOpened = saveData.leftGateOpened;
            _rightGateOpened = saveData.rightGateOpened;
            _pageCollected = saveData.chapter01PageCollected;
            _selectedDirection = NormalizeDirection(saveData.selectedFlowDirection);
            _directionCycleIndex = ResolveDirectionIndex(_selectedDirection);
            _rejectedDirectionMask = saveData.chapter01RejectedFlowDirections;
            CancelGatePuzzle();

            if (_pageCollected)
            {
                CurrentState = Chapter01State.Completed;
            }
            else if (!_leftGateOpened || !_rightGateOpened)
            {
                CurrentState = Chapter01State.NeedOpenGates;
            }
            else if (string.Equals(_selectedDirection, correctDirection, StringComparison.Ordinal))
            {
                CurrentState = Chapter01State.PageAvailable;
            }
            else
            {
                CurrentState = Chapter01State.NeedChooseFlow;
            }

            if (saveData.chapter01State > CurrentState)
            {
                CurrentState = saveData.chapter01State;
            }

            ApplyRuntimeState();
            WriteBackSaveState();
        }

        public void OnIntroFinished()
        {
            UpdateObjectiveText();
        }

        public void HandleGateInteraction(GateInteractable gateInteractable)
        {
            if (gateInteractable == null || manager == null || gateInteractable.IsOpened)
            {
                return;
            }

            if (_activeGatePuzzle != null && _activeGatePuzzle != gateInteractable)
            {
                CancelGatePuzzle();
            }

            _activeGatePuzzle = gateInteractable;
            _activeGatePuzzle.BeginPuzzleMode();

            if (manager.playerViewModeController != null)
            {
                manager.playerViewModeController.SetMovementLocked(true);
            }
            else if (manager.playerController != null)
            {
                manager.playerController.SetMovementLocked(true);
            }

            if (environmentController != null)
            {
                environmentController.OnGateCalibrationStarted(gateInteractable.gateId);
            }

            UpdateGateCalibrationUI();
        }

        public string GetFlowInteractionPrompt(string label)
        {
            if (CurrentState == Chapter01State.NeedOpenGates)
            {
                return "\u9700\u5148\u5b8c\u6210\u5de6\u53f3\u6697\u95f8\u6821\u51c6\u3002";
            }

            if (CurrentState == Chapter01State.Completed || CurrentState == Chapter01State.PageAvailable)
            {
                return "\u6c34\u8109\u5df2\u88ab\u5524\u9192";
            }

            string currentDirection = string.IsNullOrEmpty(_selectedDirection)
                ? "\u672a\u9009\u62e9"
                : Chapter01FlowDirection.GetLabel(_selectedDirection);
            string promptHint = GetDirectionPromptHint();
            if (string.IsNullOrEmpty(promptHint))
            {
                return "\u6309 E \u8c03\u8282" + label + "\uff08\u5f53\u524d\uff1a" + currentDirection + "\uff09";
            }

            return "\u6309 E \u8c03\u8282" + label + "\uff08\u5f53\u524d\uff1a" + currentDirection + "\uff1b" + promptHint + "\uff09";
        }

        public void HandleFlowSelectorInteraction()
        {
            if (manager == null)
            {
                return;
            }

            if (CurrentState == Chapter01State.NeedOpenGates)
            {
                return;
            }

            if (CurrentState == Chapter01State.PageAvailable || CurrentState == Chapter01State.Completed)
            {
                return;
            }

            manager.ShowDirectionChoice(GetDirectionOptionsForUi(), OnDirectionOptionSelected);
        }

        public void HandlePagePickup(PagePickupInteractable pickupInteractable)
        {
            if (manager == null || pickupInteractable == null || CurrentState != Chapter01State.PageAvailable)
            {
                return;
            }

            Vector3 pagePickupPosition = pickupInteractable.transform.position;

            _pageCollected = true;
            CurrentState = Chapter01State.Completed;
            pickupInteractable.SetAvailability(false);

            if (environmentController != null)
            {
                environmentController.HidePage();
            }

            manager.AddCollectedPages(1);
            ApplyRuntimeState();
            WriteBackSaveState();
            ShowChapter02RouteGuide(pagePickupPosition);
            manager.SaveProgress();
        }

        public static bool TryResolveChapter02GuideTarget(Chapter02Director chapter02Director, out Vector3 targetPosition)
        {
            targetPosition = Vector3.zero;
            if (chapter02Director == null)
            {
                return false;
            }

            Collider triggerCollider = chapter02Director.GetComponent<Collider>();
            targetPosition = triggerCollider != null
                ? triggerCollider.bounds.center
                : chapter02Director.transform.position;
            return true;
        }

        private void HandleGatePuzzleInput()
        {
            float rotationInput = 0f;
            if (Input.GetKey(gateRotateNegativeKey))
            {
                rotationInput -= 1f;
            }

            if (Input.GetKey(gateRotatePositiveKey))
            {
                rotationInput += 1f;
            }

            if (Mathf.Abs(rotationInput) > 0.001f)
            {
                float delta = rotationInput * Mathf.Max(5f, _activeGatePuzzle.rotateSpeed) * Time.deltaTime;
                _activeGatePuzzle.AdjustCalibration(delta);
                UpdateGateCalibrationUI();
            }

            if (Input.GetKeyDown(gateCancelKey))
            {
                CancelGatePuzzle();
                return;
            }

            if (Input.GetKeyDown(gateConfirmKey) && _activeGatePuzzle.IsWithinCalibrationTolerance())
            {
                SolveGatePuzzle(_activeGatePuzzle);
                return;
            }

            if (_activeGatePuzzle != null)
            {
                UpdateGateCalibrationUI();
            }
        }

        private void SolveGatePuzzle(GateInteractable gateInteractable)
        {
            if (gateInteractable == null)
            {
                return;
            }

            if (gateInteractable.gateId == GateId.Left)
            {
                _leftGateOpened = true;
            }
            else
            {
                _rightGateOpened = true;
            }

            gateInteractable.ApplyOpenedState(true);
            _activeGatePuzzle = null;
            manager?.HideGateCalibration();

            if (manager != null && manager.playerViewModeController != null)
            {
                manager.playerViewModeController.SetMovementLocked(false);
            }
            else if (manager != null && manager.playerController != null)
            {
                manager.playerController.SetMovementLocked(false);
            }

            if (environmentController != null)
            {
                environmentController.OnGateSolved(gateInteractable.gateId);
            }

            CurrentState = _leftGateOpened && _rightGateOpened
                ? Chapter01State.NeedChooseFlow
                : Chapter01State.NeedOpenGates;

            ApplyRuntimeState();
            WriteBackSaveState();
            manager.SaveProgress();
        }

        private void CancelGatePuzzle()
        {
            if (_activeGatePuzzle != null)
            {
                _activeGatePuzzle.EndPuzzleMode();
                _activeGatePuzzle = null;
            }

            manager?.HideGateCalibration();

            if (manager != null && manager.playerViewModeController != null)
            {
                manager.playerViewModeController.SetMovementLocked(false);
            }
            else if (manager != null && manager.playerController != null)
            {
                manager.playerController.SetMovementLocked(false);
            }

            ApplyRuntimeState();
        }

        private void OnDirectionOptionSelected(string optionLabel)
        {
            string directionId = Chapter01FlowDirection.GetIdFromOptionLabel(optionLabel);
            OnDirectionSelected(directionId);
        }

        private void OnDirectionSelected(string direction)
        {
            if (manager == null)
            {
                return;
            }

            _selectedDirection = NormalizeDirection(direction);
            _directionCycleIndex = ResolveDirectionIndex(_selectedDirection);
            bool isCorrectDirection = string.Equals(_selectedDirection, correctDirection, StringComparison.Ordinal);

            if (isCorrectDirection)
            {
                CurrentState = Chapter01State.FlowSolved;

                if (environmentController != null)
                {
                    environmentController.SetFlowingSolved();
                }

                CurrentState = Chapter01State.PageAvailable;
                if (pagePickup != null)
                {
                    pagePickup.SetAvailability(true);
                }

                if (environmentController != null)
                {
                    environmentController.OnPageRevealed();
                }
            }
            else
            {
                MarkRejectedDirection(_selectedDirection);
                CurrentState = Chapter01State.NeedChooseFlow;

                if (environmentController != null)
                {
                    environmentController.SetDirectionPreview(_selectedDirection);
                }

                if (pagePickup != null)
                {
                    pagePickup.SetAvailability(false);
                }
            }

            ApplyRuntimeState();

            if (environmentController != null)
            {
                environmentController.PlayDirectionSelectionFeedback(
                    _selectedDirection,
                    isCorrectDirection,
                    GetFlowFeedbackSourcePosition(),
                    GetFlowFeedbackTargetPosition());
            }

            manager.ShowToast(isCorrectDirection ? flowHintCorrect : GetRejectedDirectionToast(_selectedDirection), 2.8f);
            ShowDirectionResultBanner(_selectedDirection, isCorrectDirection);
            WriteBackSaveState();
            manager.SaveProgress();
        }

        private void ApplyRuntimeState()
        {
            if (leftGate != null)
            {
                leftGate.ApplyOpenedState(_leftGateOpened);
            }

            if (rightGate != null)
            {
                rightGate.ApplyOpenedState(_rightGateOpened);
            }

            if (environmentController != null)
            {
                if (_leftGateOpened)
                {
                    environmentController.OnGateSolved(GateId.Left);
                }

                if (_rightGateOpened)
                {
                    environmentController.OnGateSolved(GateId.Right);
                }

                if (CurrentState == Chapter01State.PageAvailable || CurrentState == Chapter01State.Completed || CurrentState == Chapter01State.FlowSolved)
                {
                    environmentController.SetFlowingSolved();
                }
                else if (CurrentState == Chapter01State.NeedChooseFlow && !string.IsNullOrEmpty(_selectedDirection))
                {
                    environmentController.SetDirectionPreview(_selectedDirection);
                }
                else
                {
                    environmentController.SetDormant();
                }

                if (CurrentState == Chapter01State.PageAvailable)
                {
                    environmentController.OnPageRevealed();
                }
                else
                {
                    environmentController.HidePage();
                }
            }

            if (pagePickup != null)
            {
                pagePickup.SetAvailability(CurrentState == Chapter01State.PageAvailable);
            }

            UpdateObjectiveText();
        }

        private void UpdateObjectiveText()
        {
            if (manager == null)
            {
                return;
            }

            if (ShouldHideObjectiveAtStart())
            {
                manager.SetObjective(string.Empty);
                return;
            }

            switch (CurrentState)
            {
                case Chapter01State.NeedOpenGates:
                    manager.SetObjective(objectiveOpenGates);
                    break;
                case Chapter01State.NeedChooseFlow:
                    manager.SetObjective(GetChooseFlowObjective());
                    break;
                case Chapter01State.FlowSolved:
                case Chapter01State.PageAvailable:
                    manager.SetObjective(objectiveCollectPage);
                    break;
                case Chapter01State.Completed:
                    manager.SetObjective(objectiveCompleted);
                    break;
                default:
                    manager.SetObjective(objectiveOpenGates);
                    break;
            }
        }

        private bool ShouldHideObjectiveAtStart()
        {
            return CurrentState == Chapter01State.NeedOpenGates
                && !_leftGateOpened
                && !_rightGateOpened
                && !_pageCollected
                && string.IsNullOrEmpty(_selectedDirection);
        }

        private void WriteBackSaveState()
        {
            if (manager == null || manager.CurrentSaveData == null)
            {
                return;
            }

            manager.CurrentSaveData.chapter01State = CurrentState;
            manager.CurrentSaveData.leftGateOpened = _leftGateOpened;
            manager.CurrentSaveData.rightGateOpened = _rightGateOpened;
            manager.CurrentSaveData.selectedFlowDirection = _selectedDirection;
            manager.CurrentSaveData.chapter01RejectedFlowDirections = _rejectedDirectionMask;
            manager.CurrentSaveData.chapter01PageCollected = _pageCollected;
        }

        private void ShowChapter02RouteGuide(Vector3 pagePickupPosition)
        {
            if (manager == null || manager.CurrentSaveData == null || manager.CurrentSaveData.chapter02State == Chapter02State.Completed)
            {
                return;
            }

            Chapter02Director chapter02Director = manager.chapter02Director != null
                ? manager.chapter02Director
                : FindObjectOfType<Chapter02Director>();
            if (!TryResolveChapter02GuideTarget(chapter02Director, out Vector3 chapter02TargetPosition))
            {
                return;
            }

            DestroyChapter02RouteGuide();

            GameObject routeGuideObject = new GameObject(string.IsNullOrWhiteSpace(chapter02RouteGuideObjectName)
                ? "Chapter01ToChapter02RouteGuide"
                : chapter02RouteGuideObjectName);
            _chapter02RouteGuideRoot = routeGuideObject.transform;
            _chapter02RouteGuideRoot.SetParent(transform, false);

            Transform startMarker = CreateRouteGuideMarker("PagePickupStart", _chapter02RouteGuideRoot, pagePickupPosition);
            Transform targetMarker = CreateRouteGuideMarker("Chapter02QuizTarget", _chapter02RouteGuideRoot, chapter02TargetPosition);
            Transform[] routeMarkers = ResolveChapter02RouteMarkers(pagePickupPosition, chapter02TargetPosition, _chapter02RouteGuideRoot);

            Chapter01AuthoredRouteGuide routeGuide = routeGuideObject.AddComponent<Chapter01AuthoredRouteGuide>();
            routeGuide.manager = manager;
            routeGuide.director = null;
            routeGuide.introController = null;
            routeGuide.playerStartPose = startMarker;
            routeGuide.targetGate = targetMarker;
            routeGuide.authoredRouteRootName = chapter02RouteGuideRootName;
            routeGuide.routePoints = routeMarkers;
            routeGuide.showGuideOnStart = true;
            routeGuide.useResolvedRouteFallback = false;
            routeGuide.smoothControlPoints = true;
            routeGuide.reachedRadius = Mathf.Max(1.5f, chapter02RouteGuideReachedRadius);
            routeGuide.maxDecorationMarkers = Mathf.Max(1, chapter02RouteGuideMaxDecorations);
            routeGuide.RebuildGuide();
        }

        private Transform[] ResolveChapter02RouteMarkers(Vector3 startPosition, Vector3 targetPosition, Transform fallbackParent)
        {
            Transform authoredRoot = FindChapter02RouteRoot();
            if (authoredRoot != null)
            {
                Transform[] authoredMarkers = new Transform[authoredRoot.childCount];
                for (int index = 0; index < authoredRoot.childCount; index++)
                {
                    authoredMarkers[index] = authoredRoot.GetChild(index);
                }

                Array.Sort(authoredMarkers, (left, right) => string.CompareOrdinal(left.name, right.name));
                if (authoredMarkers.Length > 0)
                {
                    return authoredMarkers;
                }
            }

            return CreateChapter02FallbackRouteMarkers(startPosition, targetPosition, fallbackParent);
        }

        private Transform[] CreateChapter02FallbackRouteMarkers(Vector3 startPosition, Vector3 targetPosition, Transform fallbackParent)
        {
            int pointCount = Mathf.Max(4, chapter02RouteGuideAutoPointCount);
            Transform[] markers = new Transform[pointCount];
            Vector3 flatDelta = targetPosition - startPosition;
            flatDelta.y = 0f;

            Vector3 forward = flatDelta.sqrMagnitude > 0.01f
                ? flatDelta.normalized
                : Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            if (side.sqrMagnitude < 0.001f)
            {
                side = Vector3.right;
            }
            side.Normalize();

            float routeLength = flatDelta.magnitude;
            float bendOffset = Mathf.Clamp(routeLength * 0.12f, 1.2f, 6f);

            for (int index = 0; index < pointCount; index++)
            {
                float t = (index + 1f) / (pointCount + 1f);
                float centeredT = (t - 0.5f) * 2f;
                float sideSign = index < pointCount * 0.5f ? 1f : -1f;
                float offsetStrength = 0.55f + (1f - Mathf.Abs(centeredT)) * 0.45f;
                Vector3 markerPosition = Vector3.Lerp(startPosition, targetPosition, t) + side * sideSign * bendOffset * offsetStrength;
                markers[index] = CreateRouteGuideMarker("AutoRoutePoint_" + index.ToString("00"), fallbackParent, markerPosition);
            }

            return markers;
        }

        private Transform FindChapter02RouteRoot()
        {
            if (string.IsNullOrWhiteSpace(chapter02RouteGuideRootName))
            {
                return null;
            }

            Transform localRoot = transform.Find(chapter02RouteGuideRootName);
            if (localRoot != null)
            {
                return localRoot;
            }

            GameObject rootObject = GameObject.Find(chapter02RouteGuideRootName);
            return rootObject != null ? rootObject.transform : null;
        }

        private static Transform CreateRouteGuideMarker(string markerName, Transform parent, Vector3 position)
        {
            GameObject markerObject = new GameObject(markerName);
            Transform marker = markerObject.transform;
            marker.SetParent(parent, false);
            marker.position = position;
            return marker;
        }

        private void DestroyChapter02RouteGuide()
        {
            if (_chapter02RouteGuideRoot == null)
            {
                Transform existingRoot = transform.Find(string.IsNullOrWhiteSpace(chapter02RouteGuideObjectName)
                    ? "Chapter01ToChapter02RouteGuide"
                    : chapter02RouteGuideObjectName);
                if (existingRoot != null)
                {
                    _chapter02RouteGuideRoot = existingRoot;
                }
            }

            if (_chapter02RouteGuideRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_chapter02RouteGuideRoot.gameObject);
            }
            else
            {
                DestroyImmediate(_chapter02RouteGuideRoot.gameObject);
            }

            _chapter02RouteGuideRoot = null;
        }

        private string SelectNextDirection()
        {
            _directionCycleIndex++;
            if (_directionCycleIndex >= directionOptions.Length)
            {
                _directionCycleIndex = 0;
            }

            return Chapter01FlowDirection.GetIdByIndex(_directionCycleIndex);
        }

        private int ResolveDirectionIndex(string direction)
        {
            return Chapter01FlowDirection.GetIndex(direction);
        }

        private string NormalizeDirection(string direction)
        {
            return Chapter01FlowDirection.Normalize(direction);
        }

        private string[] GetDirectionOptionsForUi()
        {
            string[] options = Chapter01FlowDirection.CreateOptionLabels();

            if (HasRejectedDirection(Chapter01FlowDirection.West))
            {
                options[0] += "\uff08\u7a7a\u54cd\uff09";
            }

            if (HasRejectedDirection(Chapter01FlowDirection.South))
            {
                options[1] += "\uff08\u672a\u901a\u4e2d\u6c60\uff09";
            }

            if (HasRejectedDirection(Chapter01FlowDirection.West) && HasRejectedDirection(Chapter01FlowDirection.South))
            {
                options[2] += "\uff08\u50cf\u662f\u6b63\u89e3\uff09";
            }

            directionOptions = options;
            return options;
        }

        private string GetChooseFlowObjective()
        {
            bool westRejected = HasRejectedDirection(Chapter01FlowDirection.West);
            bool southRejected = HasRejectedDirection(Chapter01FlowDirection.South);

            if (westRejected && southRejected)
            {
                return objectiveChooseFlowAfterAllRejected;
            }

            if (westRejected)
            {
                return objectiveChooseFlowAfterWestRejected;
            }

            if (southRejected)
            {
                return objectiveChooseFlowAfterSouthRejected;
            }

            return objectiveChooseFlow;
        }

        private string GetDirectionPromptHint()
        {
            bool westRejected = HasRejectedDirection(Chapter01FlowDirection.West);
            bool southRejected = HasRejectedDirection(Chapter01FlowDirection.South);

            if (westRejected && southRejected)
            {
                return "\u7ebf\u7d22\uff1a\u5e94\u56de\u4e2d\u6c60";
            }

            if (westRejected)
            {
                return "\u5df2\u6392\u9664\uff1a\u897f\u6e20";
            }

            if (southRejected)
            {
                return "\u5df2\u6392\u9664\uff1a\u5357\u6e20";
            }

            return string.Empty;
        }

        private string GetRejectedDirectionToast(string direction)
        {
            bool westRejected = HasRejectedDirection(Chapter01FlowDirection.West);
            bool southRejected = HasRejectedDirection(Chapter01FlowDirection.South);
            if (westRejected && southRejected)
            {
                return flowHintAllRejected;
            }

            switch (NormalizeDirection(direction))
            {
                case Chapter01FlowDirection.West:
                    return flowHintWestRejected;
                case Chapter01FlowDirection.South:
                    return flowHintSouthRejected;
                default:
                    return objectiveChooseFlow;
            }
        }

        private void MarkRejectedDirection(string direction)
        {
            _rejectedDirectionMask |= GetRejectedDirectionMask(direction);
        }

        private bool HasRejectedDirection(string direction)
        {
            int directionMask = GetRejectedDirectionMask(direction);
            return directionMask != 0 && (_rejectedDirectionMask & directionMask) != 0;
        }

        private int GetRejectedDirectionMask(string direction)
        {
            switch (NormalizeDirection(direction))
            {
                case Chapter01FlowDirection.West:
                    return WestRejectedMask;
                case Chapter01FlowDirection.South:
                    return SouthRejectedMask;
                default:
                    return 0;
            }
        }

        private Vector3 GetFlowFeedbackSourcePosition()
        {
            if (flowSelector != null)
            {
                return flowSelector.transform.position;
            }

            return transform.position;
        }

        private Vector3 GetFlowFeedbackTargetPosition()
        {
            if (pagePickup != null)
            {
                return pagePickup.transform.position;
            }

            return GetFlowFeedbackSourcePosition() + (Vector3.up * 1.2f);
        }

        private void ShowDirectionResultBanner(string direction, bool isCorrectDirection)
        {
            if (manager == null)
            {
                return;
            }

            string normalizedDirection = NormalizeDirection(direction);
            string title;
            string message;
            Color accentColor;

            if (isCorrectDirection)
            {
                title = "\u4e2d\u6c60\u5df2\u8d2f\u901a";
                message = flowHintCorrect;
                accentColor = new Color(0.45f, 0.93f, 1f, 1f);
            }
            else if (string.Equals(normalizedDirection, Chapter01FlowDirection.West, StringComparison.Ordinal))
            {
                title = "\u897f\u6e20\u53ea\u6709\u7a7a\u54cd";
                message = flowHintWestRejected;
                accentColor = new Color(1f, 0.78f, 0.35f, 1f);
            }
            else
            {
                title = "\u5357\u6e20\u672a\u80fd\u805a\u6c34";
                message = flowHintSouthRejected;
                accentColor = new Color(0.56f, 0.92f, 0.66f, 1f);
            }

            manager.ShowDirectionResult(title, message, accentColor, 2.8f);
        }

        private void UpdateGateCalibrationUI()
        {
            if (manager == null || _activeGatePuzzle == null)
            {
                return;
            }

            manager.ShowGateCalibration(new Chapter01GateCalibrationViewData
            {
                gateName = string.IsNullOrWhiteSpace(_activeGatePuzzle.gateDisplayName) ? "暗闸" : _activeGatePuzzle.gateDisplayName,
                currentAngle = _activeGatePuzzle.CurrentAngle,
                targetAngle = _activeGatePuzzle.ResolvedTargetAngle,
                validAngleTolerance = _activeGatePuzzle.validAngleTolerance,
                canConfirm = _activeGatePuzzle.IsWithinCalibrationTolerance(),
                rotationHint = BuildGateRotationHint(_activeGatePuzzle),
                negativeKey = gateRotateNegativeKey,
                positiveKey = gateRotatePositiveKey,
                confirmKey = gateConfirmKey,
                cancelKey = gateCancelKey
            });
        }

        private string BuildGateRotationHint(GateInteractable gateInteractable)
        {
            if (gateInteractable == null)
            {
                return string.Empty;
            }

            float delta = Mathf.DeltaAngle(gateInteractable.CurrentAngle, gateInteractable.ResolvedTargetAngle);
            float tolerance = Mathf.Max(0.1f, gateInteractable.validAngleTolerance);

            if (Mathf.Abs(delta) <= tolerance)
            {
                return "已经进入正确位置，请按 E 完成校准";
            }

            if (delta > 0f)
            {
                return "请继续向右旋转，靠近正确位置";
            }

            return "请继续向左旋转，靠近正确位置";
        }
    }
}
