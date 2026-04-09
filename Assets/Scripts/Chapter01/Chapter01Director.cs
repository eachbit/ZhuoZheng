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
        public string correctDirection = "东";
        public string[] directionOptions = { "西", "南", "东" };

        public string objectiveOpenGates = "前往远香堂，开启两处暗闸。";
        public string objectiveChooseFlow = "尝试调转水流，找出真正能唤醒水脉的方向。";
        public string objectiveCollectPage = "在石缝中拾取《长物志》残页。";
        public string objectiveCompleted = "第一章已完成，可继续前行。";

        public KeyCode gateRotateNegativeKey = KeyCode.A;
        public KeyCode gateRotatePositiveKey = KeyCode.D;
        public KeyCode gateConfirmKey = KeyCode.E;
        public KeyCode gateCancelKey = KeyCode.Escape;

        public Chapter01State CurrentState { get; private set; }

        public bool HasActiveGatePuzzle
        {
            get { return _activeGatePuzzle != null; }
        }

        private bool _leftGateOpened;
        private bool _rightGateOpened;
        private bool _pageCollected;
        private string _selectedDirection = string.Empty;
        private int _directionCycleIndex = -1;
        private GateInteractable _activeGatePuzzle;

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
            _selectedDirection = saveData.selectedFlowDirection ?? string.Empty;
            _directionCycleIndex = ResolveDirectionIndex(_selectedDirection);
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
        }

        public string GetFlowInteractionPrompt(string label)
        {
            if (CurrentState == Chapter01State.NeedOpenGates)
            {
                return "需先完成左右暗闸校准";
            }

            if (CurrentState == Chapter01State.Completed || CurrentState == Chapter01State.PageAvailable)
            {
                return "水脉已被唤醒";
            }

            string currentDirection = string.IsNullOrEmpty(_selectedDirection) ? "未试调" : _selectedDirection;
            return "按 E 试调" + label + "（当前：" + currentDirection + "）";
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

            string nextDirection = SelectNextDirection();
            OnDirectionSelected(nextDirection);
        }

        public void HandlePagePickup(PagePickupInteractable pickupInteractable)
        {
            if (manager == null || pickupInteractable == null || CurrentState != Chapter01State.PageAvailable)
            {
                return;
            }

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
            manager.SaveProgress();
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
            }

            if (Input.GetKeyDown(gateCancelKey))
            {
                CancelGatePuzzle();
                return;
            }

            if (Input.GetKeyDown(gateConfirmKey) && _activeGatePuzzle.IsWithinCalibrationTolerance())
            {
                SolveGatePuzzle(_activeGatePuzzle);
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

        private void OnDirectionSelected(string direction)
        {
            if (manager == null)
            {
                return;
            }

            _selectedDirection = direction ?? string.Empty;
            _directionCycleIndex = ResolveDirectionIndex(_selectedDirection);

            if (string.Equals(_selectedDirection, correctDirection, StringComparison.Ordinal))
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

            switch (CurrentState)
            {
                case Chapter01State.NeedOpenGates:
                    manager.SetObjective(objectiveOpenGates);
                    break;
                case Chapter01State.NeedChooseFlow:
                    manager.SetObjective(objectiveChooseFlow);
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
            manager.CurrentSaveData.chapter01PageCollected = _pageCollected;
        }

        private string SelectNextDirection()
        {
            if (directionOptions == null || directionOptions.Length == 0)
            {
                return string.Empty;
            }

            _directionCycleIndex++;
            if (_directionCycleIndex >= directionOptions.Length)
            {
                _directionCycleIndex = 0;
            }

            return directionOptions[_directionCycleIndex];
        }

        private int ResolveDirectionIndex(string direction)
        {
            if (string.IsNullOrEmpty(direction) || directionOptions == null)
            {
                return -1;
            }

            for (int index = 0; index < directionOptions.Length; index++)
            {
                if (string.Equals(directionOptions[index], direction, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
