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
        public string[] directionOptions = { "东", "西", "南" };

        public string objectiveOpenGates = "前往远香堂，开启两处暗闸。";
        public string objectiveChooseFlow = "至水畔调定水流方向。";
        public string objectiveCollectPage = "在石隙间寻得《长物志》残页。";
        public string objectiveCompleted = "第一章已完成，继续前行。";

        public Chapter01State CurrentState { get; private set; }

        private bool _leftGateOpened;
        private bool _rightGateOpened;
        private bool _pageCollected;
        private string _selectedDirection = string.Empty;

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

            if (gateInteractable.gateId == GateId.Left)
            {
                _leftGateOpened = true;
            }
            else
            {
                _rightGateOpened = true;
            }

            gateInteractable.ApplyOpenedState(true);
            CurrentState = _leftGateOpened && _rightGateOpened
                ? Chapter01State.NeedChooseFlow
                : Chapter01State.NeedOpenGates;

            manager.ShowToast(gateInteractable.gateDisplayName + "已启。");
            ApplyRuntimeState();
            WriteBackSaveState();
            manager.SaveProgress();
        }

        public string GetFlowInteractionPrompt(string label)
        {
            if (CurrentState == Chapter01State.NeedOpenGates)
            {
                return "需先开启两处暗闸";
            }

            if (CurrentState == Chapter01State.Completed)
            {
                return "水势已成";
            }

            if (CurrentState == Chapter01State.PageAvailable)
            {
                return "残页已现于石隙";
            }

            return "按 E " + label;
        }

        public void HandleFlowSelectorInteraction()
        {
            if (manager == null)
            {
                return;
            }

            if (CurrentState == Chapter01State.NeedOpenGates)
            {
                manager.ShowToast("需先开启左右两处暗闸。");
                return;
            }

            if (CurrentState == Chapter01State.Completed || CurrentState == Chapter01State.PageAvailable)
            {
                manager.ShowToast("水势已成，石隙间可见残页。");
                return;
            }

            manager.ShowDirectionChoice(directionOptions, OnDirectionSelected);
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
            manager.ShowToast("得《长物志》残页一页。");
            ApplyRuntimeState();
            WriteBackSaveState();
            manager.SaveProgress();
        }

        private void OnDirectionSelected(string direction)
        {
            if (manager == null)
            {
                return;
            }

            _selectedDirection = direction ?? string.Empty;

            if (string.Equals(_selectedDirection, correctDirection, StringComparison.Ordinal))
            {
                CurrentState = Chapter01State.FlowSolved;

                if (environmentController != null)
                {
                    environmentController.SetFlowing();
                }

                CurrentState = Chapter01State.PageAvailable;
                if (pagePickup != null)
                {
                    pagePickup.SetAvailability(true);
                }

                if (environmentController != null)
                {
                    environmentController.ShowPage();
                }

                manager.ShowToast("水波始动，荷影摇曳，游鱼倏忽。");
            }
            else
            {
                CurrentState = Chapter01State.NeedChooseFlow;

                if (environmentController != null)
                {
                    environmentController.SetDormant();
                }

                manager.ShowToast("水势未成，请再试。");
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

            bool isFlowing = CurrentState == Chapter01State.FlowSolved
                || CurrentState == Chapter01State.PageAvailable
                || CurrentState == Chapter01State.Completed;

            if (environmentController != null)
            {
                if (isFlowing)
                {
                    environmentController.SetFlowing();
                }
                else
                {
                    environmentController.SetDormant();
                }

                if (CurrentState == Chapter01State.PageAvailable)
                {
                    environmentController.ShowPage();
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
    }
}
