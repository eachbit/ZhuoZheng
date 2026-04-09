using UnityEngine;
using UnityEngine.InputSystem;

namespace ZhuozhengYuan
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonPlayerController : MonoBehaviour
    {
        public Transform cameraPivot;
        public CharacterController characterController;
        public float walkSpeed = 4f;
        public float runSpeed = 7f;
        public KeyCode runKey = KeyCode.LeftShift;
        public bool allowRunning = true;
        public bool allowRuntimeSpeedAdjustment = true;
        public KeyCode walkSpeedDownKey = KeyCode.Minus;
        public KeyCode walkSpeedUpKey = KeyCode.Equals;
        public KeyCode runSpeedDownKey = KeyCode.LeftBracket;
        public KeyCode runSpeedUpKey = KeyCode.RightBracket;
        public float speedAdjustmentStep = 0.5f;
        public float minWalkSpeed = 1f;
        public float maxWalkSpeed = 12f;
        public float minRunSpeed = 2f;
        public float maxRunSpeed = 18f;
        public float gravity = -20f;
        public float mouseSensitivity = 2.2f;
        public float maxLookAngle = 80f;
        public bool normalizeAttachedCameraTransform = false;

        private float _verticalVelocity;
        private float _pitch;
        private bool _controlLocked;
        private bool _movementLockedExternally;
        private Transform _attachedCameraTransform;

        public bool IsMovementLockedExternally
        {
            get { return _movementLockedExternally; }
        }

        public Transform CameraPivot
        {
            get { return cameraPivot; }
        }

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            CacheAttachedCamera();
            NormalizeAttachedCameraTransformIfNeeded();

            if (cameraPivot == null)
            {
                Debug.LogWarning("FirstPersonPlayerController is missing a Camera Pivot reference.");
            }
            else
            {
                _pitch = NormalizePitch(cameraPivot.localEulerAngles.x);
                ApplyLook();
            }
        }

        private void Update()
        {
            if (_controlLocked || characterController == null)
            {
                ApplyGravityOnly();
                return;
            }

            HandleRuntimeSpeedAdjustment();
            HandleLook();

            if (_movementLockedExternally)
            {
                ApplyGravityOnly();
                return;
            }

            HandleMove();
        }

        public void SetControlLocked(bool locked)
        {
            _controlLocked = locked;
        }

        public void SetMovementLocked(bool locked)
        {
            _movementLockedExternally = locked;
        }

        public void SetCursorForGameplay(bool gameplayActive)
        {
            Cursor.visible = !gameplayActive;
            Cursor.lockState = gameplayActive ? CursorLockMode.Locked : CursorLockMode.None;
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

            Quaternion desiredRotation = pose.rotation;
            Vector3 rootPosition = pose.position;

            if (cameraPivot != null)
            {
                Quaternion yawRotation = Quaternion.Euler(0f, desiredRotation.eulerAngles.y, 0f);
                rootPosition -= yawRotation * cameraPivot.localPosition;
            }

            SnapToPose(rootPosition, desiredRotation);
        }

        public void SnapToPose(Vector3 worldPosition, Quaternion worldRotation)
        {
            if (characterController != null)
            {
                bool previousEnabled = characterController.enabled;
                characterController.enabled = false;
                transform.position = worldPosition;
                characterController.enabled = previousEnabled;
            }
            else
            {
                transform.position = worldPosition;
            }

            Vector3 euler = worldRotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            _pitch = Mathf.Clamp(NormalizePitch(euler.x), -maxLookAngle, maxLookAngle);
            NormalizeAttachedCameraTransformIfNeeded();
            ApplyLook();
        }

        private void HandleLook()
        {
            if (cameraPivot == null)
            {
                return;
            }

            Vector2 lookInput = ReadLookInput();
            float mouseX = lookInput.x * mouseSensitivity;
            float mouseY = lookInput.y * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);
            _pitch = Mathf.Clamp(_pitch - mouseY, -maxLookAngle, maxLookAngle);
            ApplyLook();
        }

        private void HandleMove()
        {
            Vector2 moveInput = ReadMoveInput();
            Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
            input = Vector3.ClampMagnitude(input, 1f);

            float currentMoveSpeed = walkSpeed;
            if (allowRunning && IsRunPressed())
            {
                currentMoveSpeed = runSpeed;
            }

            Vector3 movement = transform.TransformDirection(input) * Mathf.Max(0f, currentMoveSpeed);

            if (characterController.isGrounded)
            {
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }

            movement.y = _verticalVelocity;
            characterController.Move(movement * Time.deltaTime);
        }

        private void HandleRuntimeSpeedAdjustment()
        {
            if (!allowRuntimeSpeedAdjustment)
            {
                return;
            }

            float step = Mathf.Max(0.1f, speedAdjustmentStep);
            bool changed = false;

            if (Input.GetKeyDown(walkSpeedDownKey))
            {
                walkSpeed = Mathf.Clamp(walkSpeed - step, minWalkSpeed, maxWalkSpeed);
                changed = true;
            }
            else if (Input.GetKeyDown(walkSpeedUpKey))
            {
                walkSpeed = Mathf.Clamp(walkSpeed + step, minWalkSpeed, maxWalkSpeed);
                changed = true;
            }

            if (Input.GetKeyDown(runSpeedDownKey))
            {
                runSpeed = Mathf.Clamp(runSpeed - step, minRunSpeed, maxRunSpeed);
                changed = true;
            }
            else if (Input.GetKeyDown(runSpeedUpKey))
            {
                runSpeed = Mathf.Clamp(runSpeed + step, minRunSpeed, maxRunSpeed);
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            walkSpeed = Mathf.Clamp(walkSpeed, minWalkSpeed, Mathf.Min(maxWalkSpeed, runSpeed));
            runSpeed = Mathf.Clamp(runSpeed, Mathf.Max(minRunSpeed, walkSpeed), maxRunSpeed);

            if (GardenGameManager.Instance != null)
            {
                GardenGameManager.Instance.ShowToast("Speed  Walk " + walkSpeed.ToString("0.0") + "  Run " + runSpeed.ToString("0.0"), 1.8f);
            }
        }

        private void ApplyGravityOnly()
        {
            if (characterController == null)
            {
                return;
            }

            if (characterController.isGrounded)
            {
                _verticalVelocity = -2f;
                return;
            }

            _verticalVelocity += gravity * Time.deltaTime;
            characterController.Move(Vector3.up * (_verticalVelocity * Time.deltaTime));
        }

        private void ApplyLook()
        {
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void CacheAttachedCamera()
        {
            _attachedCameraTransform = null;

            if (cameraPivot == null)
            {
                return;
            }

            Camera attachedCamera = cameraPivot.GetComponentInChildren<Camera>(true);
            if (attachedCamera != null && attachedCamera.transform.parent == cameraPivot)
            {
                _attachedCameraTransform = attachedCamera.transform;
            }
        }

        private void NormalizeAttachedCameraTransformIfNeeded()
        {
            if (_attachedCameraTransform == null)
            {
                return;
            }

            bool shouldResetPosition = _attachedCameraTransform.localPosition.sqrMagnitude > 0.0001f;
            bool shouldResetRotation = Quaternion.Angle(_attachedCameraTransform.localRotation, Quaternion.identity) > 0.01f;
            bool shouldResetScale = (_attachedCameraTransform.localScale - Vector3.one).sqrMagnitude > 0.0001f;

            if (!shouldResetPosition && !shouldResetRotation && !shouldResetScale)
            {
                return;
            }

            Debug.LogWarning("Main Camera local transform was offset. Resetting it to CameraPivot so the first-person view stays playable.");
            _attachedCameraTransform.localPosition = Vector3.zero;
            _attachedCameraTransform.localRotation = Quaternion.identity;
            _attachedCameraTransform.localScale = Vector3.one;
        }

        private static float NormalizePitch(float angle)
        {
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }

        private Vector2 ReadMoveInput()
        {
            Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (move.sqrMagnitude > 0.0001f)
            {
                return Vector2.ClampMagnitude(move, 1f);
            }

            if (Keyboard.current == null)
            {
                return Vector2.zero;
            }

            float horizontal = 0f;
            float vertical = 0f;

            if (Keyboard.current.aKey.isPressed)
            {
                horizontal -= 1f;
            }

            if (Keyboard.current.dKey.isPressed)
            {
                horizontal += 1f;
            }

            if (Keyboard.current.wKey.isPressed)
            {
                vertical += 1f;
            }

            if (Keyboard.current.sKey.isPressed)
            {
                vertical -= 1f;
            }

            return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }

        private Vector2 ReadLookInput()
        {
            Vector2 look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            if (look.sqrMagnitude > 0.000001f)
            {
                return look;
            }

            if (Mouse.current == null)
            {
                return Vector2.zero;
            }

            Vector2 delta = Mouse.current.delta.ReadValue();
            return delta * 0.02f;
        }

        private bool IsRunPressed()
        {
            if (Input.GetKey(runKey))
            {
                return true;
            }

            if (Keyboard.current == null)
            {
                return false;
            }

            switch (runKey)
            {
                case KeyCode.LeftShift:
                    return Keyboard.current.leftShiftKey.isPressed;
                case KeyCode.RightShift:
                    return Keyboard.current.rightShiftKey.isPressed;
                default:
                    return false;
            }
        }
    }
}
