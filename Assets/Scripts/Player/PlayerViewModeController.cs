using System.Collections.Generic;
using UnityEngine;

namespace ZhuozhengYuan
{
    public enum ViewMode
    {
        FirstPerson = 0,
        ThirdPerson = 1
    }

    [DisallowMultipleComponent]
    public class PlayerViewModeController : MonoBehaviour
    {
        public FirstPersonPlayerController playerController;
        public StarterAssetsThirdPersonBridge thirdPersonBridge;
        public PlayerInteractor playerInteractor;
        public Transform cameraPivot;
        public Camera gameplayCamera;
        public Transform playerVisualRoot;
        public KeyCode toggleViewKey = KeyCode.V;
        public ViewMode initialViewMode = ViewMode.FirstPerson;
        public Vector3 shoulderOffset = new Vector3(0.55f, 0.15f, 0f);
        public float followDistance = 3.6f;
        public float followHeight = 0.15f;
        public float orbitSensitivity = 1f;
        public float collisionRadius = 0.22f;
        public float cameraMoveSharpness = 14f;
        public LayerMask cameraCollisionLayers = Physics.DefaultRaycastLayers;

        public ViewMode CurrentMode { get; private set; }

        public Camera ActiveCamera
        {
            get
            {
                if (CurrentMode == ViewMode.ThirdPerson
                    && thirdPersonBridge != null
                    && thirdPersonBridge.ActiveCamera != null)
                {
                    return thirdPersonBridge.ActiveCamera;
                }

                return gameplayCamera;
            }
        }

        public CharacterController ActiveCharacterController
        {
            get
            {
                if (thirdPersonBridge != null && thirdPersonBridge.ActiveCharacterController != null)
                {
                    return thirdPersonBridge.ActiveCharacterController;
                }

                return playerController != null ? playerController.characterController : null;
            }
        }

        private readonly List<Renderer> _playerVisualRenderers = new List<Renderer>();
        private Vector3 _firstPersonLocalPosition;
        private Quaternion _firstPersonLocalRotation = Quaternion.identity;
        private float _baseMouseSensitivity = 2.2f;

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = GetComponent<FirstPersonPlayerController>();
            }

            if (playerInteractor == null)
            {
                playerInteractor = GetComponent<PlayerInteractor>();
            }

            if (thirdPersonBridge == null)
            {
                thirdPersonBridge = GetComponent<StarterAssetsThirdPersonBridge>();
            }

            if (cameraPivot == null && playerController != null)
            {
                cameraPivot = playerController.cameraPivot;
            }

            if (gameplayCamera == null && cameraPivot != null)
            {
                gameplayCamera = cameraPivot.GetComponentInChildren<Camera>(true);
            }

            if (playerVisualRoot == null)
            {
                Transform playerVisual = transform.Find("PlayerVisual");
                if (playerVisual != null)
                {
                    playerVisualRoot = playerVisual;
                }
            }

            CachePlayerVisuals();
            CacheAndRepairFirstPersonCameraPose();

            if (playerController != null)
            {
                _baseMouseSensitivity = playerController.mouseSensitivity;
            }

            if (playerInteractor != null)
            {
                playerInteractor.viewModeController = this;
                if (playerInteractor.playerCamera == null && gameplayCamera != null)
                {
                    playerInteractor.playerCamera = gameplayCamera;
                }
            }

            if (thirdPersonBridge != null)
            {
                thirdPersonBridge.playerController = playerController;
                thirdPersonBridge.playerInteractor = playerInteractor;
                thirdPersonBridge.cameraPivot = cameraPivot;
                thirdPersonBridge.gameplayCamera = gameplayCamera;
                thirdPersonBridge.playerVisualRoot = playerVisualRoot;
            }

            SetViewMode(initialViewMode, true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleViewKey))
            {
                SwitchViewMode();
            }
        }

        private void LateUpdate()
        {
            if (cameraPivot == null || gameplayCamera == null)
            {
                return;
            }

            if (CurrentMode == ViewMode.ThirdPerson && thirdPersonBridge != null && thirdPersonBridge.IsConfigured)
            {
                return;
            }

            if (CurrentMode == ViewMode.FirstPerson)
            {
                gameplayCamera.transform.localPosition = _firstPersonLocalPosition;
                gameplayCamera.transform.localRotation = _firstPersonLocalRotation;
                return;
            }

            Vector3 desiredLocalOffset = new Vector3(
                shoulderOffset.x,
                shoulderOffset.y + followHeight,
                -Mathf.Max(0.8f, followDistance));

            Vector3 pivotWorldPosition = cameraPivot.position;
            Vector3 desiredWorldPosition = cameraPivot.TransformPoint(desiredLocalOffset);
            Vector3 direction = desiredWorldPosition - pivotWorldPosition;
            float desiredDistance = direction.magnitude;

            if (desiredDistance > 0.001f)
            {
                Vector3 normalizedDirection = direction / desiredDistance;
                if (Physics.SphereCast(
                    pivotWorldPosition,
                    Mathf.Max(0.05f, collisionRadius),
                    normalizedDirection,
                    out RaycastHit hit,
                    desiredDistance,
                    cameraCollisionLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    desiredWorldPosition = pivotWorldPosition + (normalizedDirection * Mathf.Max(0.2f, hit.distance - collisionRadius));
                }
            }

            gameplayCamera.transform.position = Vector3.Lerp(
                gameplayCamera.transform.position,
                desiredWorldPosition,
                1f - Mathf.Exp(-Mathf.Max(0.01f, cameraMoveSharpness) * Time.deltaTime));
            gameplayCamera.transform.rotation = cameraPivot.rotation;
        }

        public void SwitchViewMode()
        {
            SetViewMode(CurrentMode == ViewMode.FirstPerson ? ViewMode.ThirdPerson : ViewMode.FirstPerson);
        }

        public void SetViewMode(ViewMode mode)
        {
            SetViewMode(mode, false);
        }

        public void SetViewMode(ViewMode mode, bool immediate)
        {
            CurrentMode = mode;

            bool useStarterAssetsThirdPerson = CurrentMode == ViewMode.ThirdPerson
                && thirdPersonBridge != null
                && thirdPersonBridge.IsConfigured;

            if (playerController != null)
            {
                float multiplier = CurrentMode == ViewMode.ThirdPerson ? Mathf.Max(0.1f, orbitSensitivity) : 1f;
                playerController.mouseSensitivity = _baseMouseSensitivity * multiplier;
                playerController.enabled = !useStarterAssetsThirdPerson;
            }

            if (thirdPersonBridge != null)
            {
                thirdPersonBridge.SetThirdPersonActive(useStarterAssetsThirdPerson, immediate);
            }

            bool showVisual = CurrentMode == ViewMode.ThirdPerson;
            for (int index = 0; index < _playerVisualRenderers.Count; index++)
            {
                if (_playerVisualRenderers[index] != null)
                {
                    _playerVisualRenderers[index].enabled = showVisual;
                }
            }

            if (gameplayCamera == null)
            {
                return;
            }

            if (CurrentMode == ViewMode.FirstPerson)
            {
                gameplayCamera.transform.localPosition = _firstPersonLocalPosition;
                gameplayCamera.transform.localRotation = _firstPersonLocalRotation;
                return;
            }

            if (useStarterAssetsThirdPerson)
            {
                return;
            }

            if (immediate && cameraPivot != null)
            {
                Vector3 desiredWorldPosition = cameraPivot.TransformPoint(
                    new Vector3(
                        shoulderOffset.x,
                        shoulderOffset.y + followHeight,
                        -Mathf.Max(0.8f, followDistance)));
                gameplayCamera.transform.position = desiredWorldPosition;
                gameplayCamera.transform.rotation = cameraPivot.rotation;
            }
        }

        public void SetMovementLocked(bool locked)
        {
            if (playerController != null)
            {
                playerController.SetMovementLocked(locked);
            }

            if (thirdPersonBridge != null)
            {
                thirdPersonBridge.SetMovementLocked(locked);
            }
        }

        public void SetControlLocked(bool locked)
        {
            if (playerController != null)
            {
                playerController.SetControlLocked(locked);
            }

            if (thirdPersonBridge != null)
            {
                thirdPersonBridge.SetControlLocked(locked);
            }
        }

        public void SetCursorForGameplay(bool gameplayActive)
        {
            if (thirdPersonBridge != null && CurrentMode == ViewMode.ThirdPerson && thirdPersonBridge.IsConfigured)
            {
                thirdPersonBridge.SetCursorForGameplay(gameplayActive);
                return;
            }

            if (playerController != null)
            {
                playerController.SetCursorForGameplay(gameplayActive);
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

            if (CurrentMode == ViewMode.ThirdPerson && thirdPersonBridge != null && thirdPersonBridge.IsConfigured)
            {
                thirdPersonBridge.SnapToPose(pose.position, pose.rotation);
                return;
            }

            if (playerController != null)
            {
                playerController.SnapViewToPose(pose);
            }
        }

        public void SnapToPose(Vector3 worldPosition, Quaternion worldRotation)
        {
            if (CurrentMode == ViewMode.ThirdPerson && thirdPersonBridge != null && thirdPersonBridge.IsConfigured)
            {
                thirdPersonBridge.SnapToPose(worldPosition, worldRotation);
                return;
            }

            if (playerController != null)
            {
                playerController.SnapToPose(worldPosition, worldRotation);
            }
        }

        private void CachePlayerVisuals()
        {
            _playerVisualRenderers.Clear();

            if (playerVisualRoot == null)
            {
                return;
            }

            Renderer[] renderers = playerVisualRoot.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null && !_playerVisualRenderers.Contains(renderers[index]))
                {
                    _playerVisualRenderers.Add(renderers[index]);
                }
            }
        }

        private void CacheAndRepairFirstPersonCameraPose()
        {
            _firstPersonLocalPosition = Vector3.zero;
            _firstPersonLocalRotation = Quaternion.identity;

            if (gameplayCamera == null)
            {
                return;
            }

            if (cameraPivot != null)
            {
                if (gameplayCamera.transform.parent != cameraPivot)
                {
                    gameplayCamera.transform.SetParent(cameraPivot, false);
                }

                bool shouldResetPosition = gameplayCamera.transform.localPosition.sqrMagnitude > 0.0001f;
                bool shouldResetRotation = Quaternion.Angle(gameplayCamera.transform.localRotation, Quaternion.identity) > 0.01f;
                bool shouldResetScale = (gameplayCamera.transform.localScale - Vector3.one).sqrMagnitude > 0.0001f;

                if (shouldResetPosition || shouldResetRotation || shouldResetScale)
                {
                    gameplayCamera.transform.localPosition = Vector3.zero;
                    gameplayCamera.transform.localRotation = Quaternion.identity;
                    gameplayCamera.transform.localScale = Vector3.one;
                }
            }

            _firstPersonLocalPosition = gameplayCamera.transform.localPosition;
            _firstPersonLocalRotation = gameplayCamera.transform.localRotation;
        }
    }
}
