using Cinemachine;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ZhuozhengYuan
{
    [DisallowMultipleComponent]
    public class StarterAssetsThirdPersonBridge : MonoBehaviour
    {
        public PlayerInteractor playerInteractor;
        public Camera gameplayCamera;
        public Transform playerVisualRoot;
        public ThirdPersonController thirdPersonController;
        public StarterAssetsInputs starterAssetsInputs;
        public PlayerInput playerInput;
        public Transform cinemachineCameraTarget;
        public CinemachineVirtualCamera followVirtualCamera;
        public bool startInThirdPerson = true;
        private bool _movementLocked;
        private bool _controlLocked;

        public bool IsConfigured
        {
            get
            {
                return thirdPersonController != null
                    && starterAssetsInputs != null
                    && playerInput != null
                    && cinemachineCameraTarget != null
                    && followVirtualCamera != null
                    && gameplayCamera != null;
            }
        }

        public Camera ActiveCamera
        {
            get { return gameplayCamera; }
        }

        public CharacterController ActiveCharacterController
        {
            get
            {
                return thirdPersonController != null
                    ? thirdPersonController.GetComponent<CharacterController>()
                    : GetComponent<CharacterController>();
            }
        }

        private void Awake()
        {
            AutoResolveReferences();
            EnsureCinemachineBrain();
            EnsureVirtualCameraTargets();
            SetThirdPersonComponentsEnabled(startInThirdPerson);
        }

        private void Update()
        {
            AutoResolveReferences();
            EnsureCinemachineBrain();
            EnsureVirtualCameraTargets();

            if (!IsConfigured)
            {
                return;
            }

            if (_movementLocked || _controlLocked)
            {
                starterAssetsInputs.MoveInput(Vector2.zero);
                starterAssetsInputs.SprintInput(false);
                starterAssetsInputs.JumpInput(false);
            }

            if (_controlLocked)
            {
                starterAssetsInputs.LookInput(Vector2.zero);
            }
        }

        public void SetThirdPersonActive(bool active, bool immediate)
        {
            AutoResolveReferences();
            EnsureCinemachineBrain();
            EnsureVirtualCameraTargets();

            if (!IsConfigured)
            {
                SetThirdPersonComponentsEnabled(false);

                return;
            }

            SetThirdPersonComponentsEnabled(active);
        }

        public void SetMovementLocked(bool locked)
        {
            _movementLocked = locked;
        }

        public void SetControlLocked(bool locked)
        {
            _controlLocked = locked;
        }

        public void SetCursorForGameplay(bool gameplayActive)
        {
            if (starterAssetsInputs != null)
            {
                starterAssetsInputs.cursorLocked = gameplayActive;
                starterAssetsInputs.cursorInputForLook = gameplayActive && !_controlLocked;
            }

            Cursor.visible = !gameplayActive;
            Cursor.lockState = gameplayActive ? CursorLockMode.Locked : CursorLockMode.None;
        }

        public void SnapToPose(Vector3 worldPosition, Quaternion worldRotation)
        {
            CharacterController controller = ActiveCharacterController;
            bool previousEnabled = controller != null && controller.enabled;

            if (controller != null)
            {
                controller.enabled = false;
            }

            transform.position = worldPosition;
            transform.rotation = Quaternion.Euler(0f, worldRotation.eulerAngles.y, 0f);

            if (cinemachineCameraTarget != null)
            {
                cinemachineCameraTarget.rotation = worldRotation;
            }

            if (controller != null)
            {
                controller.enabled = previousEnabled;
            }
        }

        public void SnapToPose(Transform pose)
        {
            if (pose == null)
            {
                return;
            }

            SnapToPose(pose.position, pose.rotation);
        }

        public void SnapViewToPose(Transform pose)
        {
            if (pose == null)
            {
                return;
            }

            SnapToPose(pose.position, pose.rotation);
        }

        private void EnsureCinemachineBrain()
        {
            if (gameplayCamera == null)
            {
                return;
            }

            if (gameplayCamera.GetComponent<CinemachineBrain>() == null)
            {
                gameplayCamera.gameObject.AddComponent<CinemachineBrain>();
            }
        }

        private void AutoResolveReferences()
        {
            if (playerInteractor == null)
            {
                playerInteractor = GetComponent<PlayerInteractor>();
            }

            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            if (thirdPersonController == null)
            {
                thirdPersonController = GetComponent<ThirdPersonController>();
            }

            if (starterAssetsInputs == null)
            {
                starterAssetsInputs = GetComponent<StarterAssetsInputs>();
            }

            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInput>();
            }

            if (cinemachineCameraTarget == null)
            {
                Transform target = transform.Find("CinemachineCameraTarget");
                if (target != null)
                {
                    cinemachineCameraTarget = target;
                }
            }

            if (followVirtualCamera == null)
            {
                GameObject followCameraObject = GameObject.Find("PlayerFollowCamera");
                if (followCameraObject != null)
                {
                    followVirtualCamera = followCameraObject.GetComponent<CinemachineVirtualCamera>();
                }
            }

            if (playerVisualRoot == null)
            {
                Transform visual = transform.Find("PlayerVisual");
                if (visual != null)
                {
                    playerVisualRoot = visual;
                }
            }
        }

        private void EnsureVirtualCameraTargets()
        {
            if (followVirtualCamera == null || cinemachineCameraTarget == null)
            {
                return;
            }

            if (followVirtualCamera.Follow != cinemachineCameraTarget)
            {
                followVirtualCamera.Follow = cinemachineCameraTarget;
            }

            if (followVirtualCamera.LookAt != cinemachineCameraTarget)
            {
                followVirtualCamera.LookAt = cinemachineCameraTarget;
            }

        }

        private void SetThirdPersonComponentsEnabled(bool active)
        {
            if (thirdPersonController != null)
            {
                thirdPersonController.enabled = active;
            }

            if (starterAssetsInputs != null)
            {
                starterAssetsInputs.enabled = active;
            }

            if (playerInput != null)
            {
                playerInput.enabled = active;
            }

            if (followVirtualCamera != null)
            {
                followVirtualCamera.gameObject.SetActive(active);
            }
        }
    }
}
