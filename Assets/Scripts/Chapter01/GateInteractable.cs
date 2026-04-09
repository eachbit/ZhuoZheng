using UnityEngine;

namespace ZhuozhengYuan
{
    public enum GateId
    {
        Left = 0,
        Right = 1
    }

    public class GateInteractable : MonoBehaviour, IInteractable
    {
        public Chapter01Director director;
        public GateId gateId;
        public string gateDisplayName = "暗闸";
        public GameObject[] closedStateObjects;
        public GameObject[] openedStateObjects;
        public Transform dialTransform;
        public Vector3 dialRotationAxis = Vector3.up;
        public bool useSuggestedTargetAngle = true;
        public float targetAngle = 0f;
        public float validAngleTolerance = 9f;
        public float rotateSpeed = 90f;
        public bool snapSolvedAngle = true;
        public GameObject[] puzzleModeObjects;
        public GameObject[] solvedFeedbackObjects;

        [SerializeField]
        private bool isOpened;

        [SerializeField]
        private float currentAngle;

        private bool _isPuzzleModeActive;
        private Quaternion _dialBaseLocalRotation = Quaternion.identity;

        public bool IsOpened
        {
            get { return isOpened; }
        }

        public bool IsPuzzleModeActive
        {
            get { return _isPuzzleModeActive; }
        }

        public float CurrentAngle
        {
            get { return currentAngle; }
        }

        public float ResolvedTargetAngle
        {
            get { return useSuggestedTargetAngle ? GetSuggestedTargetAngle() : targetAngle; }
        }

        private void Awake()
        {
            if (dialTransform == null)
            {
                dialTransform = transform;
            }

            _dialBaseLocalRotation = dialTransform.localRotation;
            ApplyCurrentAngleToDial();
            ApplyOpenedState(isOpened);
        }

        public bool CanInteract(PlayerInteractor interactor)
        {
            return director != null && !isOpened && !_isPuzzleModeActive;
        }

        public string GetInteractionPrompt(PlayerInteractor interactor)
        {
            if (director == null)
            {
                return string.Empty;
            }

            if (isOpened)
            {
                return gateDisplayName + "已开启";
            }

            if (_isPuzzleModeActive)
            {
                return "A/D 校准" + gateDisplayName + "，按 E 确认";
            }

            return "按 E 操作" + gateDisplayName;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (director == null)
            {
                Debug.LogWarning(name + " is missing a Chapter01Director reference.");
                return;
            }

            director.HandleGateInteraction(this);
        }

        public void BeginPuzzleMode()
        {
            _isPuzzleModeActive = true;
            SetObjectsActive(puzzleModeObjects, true);
        }

        public void EndPuzzleMode()
        {
            _isPuzzleModeActive = false;
            SetObjectsActive(puzzleModeObjects, false);
        }

        public void AdjustCalibration(float delta)
        {
            if (isOpened)
            {
                return;
            }

            currentAngle += delta;
            currentAngle = NormalizeAngle(currentAngle);
            ApplyCurrentAngleToDial();
        }

        public bool IsWithinCalibrationTolerance()
        {
            float delta = Mathf.Abs(Mathf.DeltaAngle(currentAngle, ResolvedTargetAngle));
            return delta <= Mathf.Max(0.1f, validAngleTolerance);
        }

        public void ApplyOpenedState(bool opened)
        {
            isOpened = opened;
            if (opened && snapSolvedAngle)
            {
                currentAngle = NormalizeAngle(ResolvedTargetAngle);
            }

            ApplyCurrentAngleToDial();
            EndPuzzleMode();
            SetObjectsActive(closedStateObjects, !opened);
            SetObjectsActive(openedStateObjects, opened);
            SetObjectsActive(solvedFeedbackObjects, opened);
        }

        private void ApplyCurrentAngleToDial()
        {
            if (dialTransform == null)
            {
                return;
            }

            Vector3 axis = dialRotationAxis.sqrMagnitude <= 0.0001f ? Vector3.up : dialRotationAxis.normalized;
            dialTransform.localRotation = _dialBaseLocalRotation * Quaternion.AngleAxis(currentAngle, axis);
        }

        private float GetSuggestedTargetAngle()
        {
            return gateId == GateId.Left ? 55f : -70f;
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

        private static void SetObjectsActive(GameObject[] gameObjects, bool active)
        {
            if (gameObjects == null)
            {
                return;
            }

            for (int index = 0; index < gameObjects.Length; index++)
            {
                if (gameObjects[index] != null)
                {
                    gameObjects[index].SetActive(active);
                }
            }
        }
    }
}
