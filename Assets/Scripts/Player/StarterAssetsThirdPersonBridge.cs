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
        public FirstPersonPlayerController playerController;
        public PlayerInteractor playerInteractor;
        public Transform cameraPivot;
        public Camera gameplayCamera;
        public Transform playerVisualRoot;
        public ThirdPersonController thirdPersonController;
        public StarterAssetsInputs starterAssetsInputs;
        public PlayerInput playerInput;
        public Transform cinemachineCameraTarget;
        public CinemachineVirtualCamera followVirtualCamera;
        public bool startInThirdPerson = true;
        public bool forcePresentationCameraRig = true;
        public Vector3 presentationShoulderOffset = new Vector3(0f, 0.35f, 0f);
        public float presentationVerticalArmLength = 0.45f;
        public float presentationCameraSide = 0.5f;
        public float presentationCameraDistance = 6.5f;
        public float presentationCameraRadius = 0.18f;
        public Vector3 presentationDamping = new Vector3(0.15f, 0.2f, 0.18f);

        private bool _movementLocked;
        private bool _controlLocked;
        private Transform _originalCameraParent;
        private Vector3 _originalCameraLocalPosition;
        private Quaternion _originalCameraLocalRotation = Quaternion.identity;
        private Vector3 _originalCameraLocalScale = Vector3.one;

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
                if (thirdPersonController != null)
                {
                    return thirdPersonController.GetComponent<CharacterController>();
                }

                return playerController != null ? playerController.characterController : GetComponent<CharacterController>();
            }
        }

        private void Awake()
        {
            AutoResolveReferences();
            if (playerController == null)
            {
                playerController = GetComponent<FirstPersonPlayerController>();
            }

            if (playerInteractor == null)
            {
                playerInteractor = GetComponent<PlayerInteractor>();
            }

            if (cameraPivot == null && playerController != null)
            {
                cameraPivot = playerController.cameraPivot;
            }

            if (gameplayCamera == null && cameraPivot != null)
            {
                gameplayCamera = cameraPivot.GetComponentInChildren<Camera>(true);
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

            CacheOriginalCameraPose();
            EnsureVirtualCameraTargets();
            SetThirdPersonComponentsEnabled(false);
        }

        private void Update()
        {
            AutoResolveReferences();
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
            EnsureVirtualCameraTargets();

            if (!IsConfigured)
            {
                SetThirdPersonComponentsEnabled(false);

                return;
            }

            SetThirdPersonComponentsEnabled(active);

            if (gameplayCamera == null)
            {
                return;
            }

            if (active)
            {
                EnsureCinemachineBrain();
                SyncThirdPersonTargetToCurrentView();

                if (gameplayCamera.transform.parent != null)
                {
                    gameplayCamera.transform.SetParent(null, true);
                }

                if (immediate && cinemachineCameraTarget != null)
                {
                    gameplayCamera.transform.position = cinemachineCameraTarget.position;
                    gameplayCamera.transform.rotation = cinemachineCameraTarget.rotation;
                }
            }
            else
            {
                SyncFirstPersonViewToThirdPersonTarget();

                if (cameraPivot != null)
                {
                    gameplayCamera.transform.SetParent(cameraPivot, false);
                }
                else if (_originalCameraParent != null)
                {
                    gameplayCamera.transform.SetParent(_originalCameraParent, false);
                }

                gameplayCamera.transform.localPosition = _originalCameraLocalPosition;
                gameplayCamera.transform.localRotation = _originalCameraLocalRotation;
                gameplayCamera.transform.localScale = _originalCameraLocalScale;
            }
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

        private void CacheOriginalCameraPose()
        {
            if (gameplayCamera == null)
            {
                return;
            }

            _originalCameraParent = gameplayCamera.transform.parent;
            if (cameraPivot != null && gameplayCamera.transform.parent != cameraPivot)
            {
                _originalCameraLocalPosition = Vector3.zero;
                _originalCameraLocalRotation = Quaternion.identity;
                _originalCameraLocalScale = Vector3.one;
                return;
            }

            _originalCameraLocalPosition = gameplayCamera.transform.localPosition;
            _originalCameraLocalRotation = gameplayCamera.transform.localRotation;
            _originalCameraLocalScale = gameplayCamera.transform.localScale;
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
            if (playerController == null)
            {
                playerController = GetComponent<FirstPersonPlayerController>();
            }

            if (playerInteractor == null)
            {
                playerInteractor = GetComponent<PlayerInteractor>();
            }

            if (cameraPivot == null && playerController != null)
            {
                cameraPivot = playerController.cameraPivot;
            }

            if (gameplayCamera == null && cameraPivot != null)
            {
                gameplayCamera = cameraPivot.GetComponentInChildren<Camera>(true);
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

            if (!forcePresentationCameraRig)
            {
                return;
            }

            Cinemachine3rdPersonFollow thirdPersonFollow = followVirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            if (thirdPersonFollow == null)
            {
                return;
            }

            thirdPersonFollow.ShoulderOffset = presentationShoulderOffset;
            thirdPersonFollow.VerticalArmLength = presentationVerticalArmLength;
            thirdPersonFollow.CameraSide = presentationCameraSide;
            thirdPersonFollow.CameraDistance = presentationCameraDistance;
            thirdPersonFollow.CameraRadius = presentationCameraRadius;
            thirdPersonFollow.Damping = presentationDamping;
        }

        private void SyncThirdPersonTargetToCurrentView()
        {
            if (cinemachineCameraTarget == null)
            {
                return;
            }

            if (cameraPivot != null)
            {
                cinemachineCameraTarget.rotation = cameraPivot.rotation;
                return;
            }

            cinemachineCameraTarget.rotation = transform.rotation;
        }

        private void SyncFirstPersonViewToThirdPersonTarget()
        {
            if (playerController == null || cinemachineCameraTarget == null)
            {
                return;
            }

            playerController.SnapToPose(transform.position, cinemachineCameraTarget.rotation);
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
