using UnityEngine;

namespace ZhuozhengYuan
{
    public class PlayerInteractor : MonoBehaviour
    {
        private static readonly RaycastHit[] InteractionHitBuffer = new RaycastHit[32];
        private static readonly Collider[] NearbyInteractableBuffer = new Collider[32];

        public GardenGameManager gameManager;
        public Camera playerCamera;
        public PlayerViewModeController viewModeController;
        public float interactDistance = 6f;
        public float interactSphereRadius = 0.75f;
        public bool enableNearbyFallback = true;
        public float nearbyFallbackRadius = 3f;
        public float nearbyFallbackVerticalRange = 80f;
        public LayerMask interactLayers = Physics.DefaultRaycastLayers;
        public KeyCode interactKey = KeyCode.E;
        public QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide;

        private IInteractable _currentInteractable;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GardenGameManager.Instance;
            }

            if (viewModeController == null)
            {
                viewModeController = GetComponent<PlayerViewModeController>();
            }

            if (playerCamera == null)
            {
                if (viewModeController != null && viewModeController.ActiveCamera != null)
                {
                    playerCamera = viewModeController.ActiveCamera;
                }
                else
                {
                    playerCamera = GetComponentInChildren<Camera>(true);
                }
            }
        }

        private void Update()
        {
            if (gameManager == null || !gameManager.CanPlayerInteract)
            {
                ClearPrompt();
                return;
            }

            UpdateCurrentInteractable();

            if (_currentInteractable != null && Input.GetKeyDown(interactKey))
            {
                _currentInteractable.Interact(this);
            }
        }

        private void UpdateCurrentInteractable()
        {
            _currentInteractable = null;

            Camera interactionCamera = ResolveInteractionCamera();

            if (interactionCamera == null)
            {
                ClearPrompt();
                return;
            }

            if (TryFindInteractableByView(interactionCamera, out IInteractable interactable))
            {
                string prompt = interactable.GetInteractionPrompt(this);
                if (!string.IsNullOrEmpty(prompt))
                {
                    _currentInteractable = interactable.CanInteract(this) ? interactable : null;
                    gameManager.SetInteractionPrompt(prompt);
                    return;
                }
            }

            if (enableNearbyFallback && TryFindNearbyInteractable(interactionCamera, out IInteractable nearbyInteractable))
            {
                string prompt = nearbyInteractable.GetInteractionPrompt(this);
                if (!string.IsNullOrEmpty(prompt))
                {
                    _currentInteractable = nearbyInteractable.CanInteract(this) ? nearbyInteractable : null;
                    gameManager.SetInteractionPrompt(prompt);
                    return;
                }
            }

            ClearPrompt();
        }

        private bool TryFindInteractableByView(Camera interactionCamera, out IInteractable interactable)
        {
            interactable = null;

            Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int hitCount = Physics.SphereCastNonAlloc(
                ray,
                Mathf.Max(0.05f, interactSphereRadius),
                InteractionHitBuffer,
                interactDistance,
                interactLayers,
                queryTriggerInteraction);

            if (hitCount <= 0)
            {
                return false;
            }

            SortHitsByDistance(InteractionHitBuffer, hitCount);
            for (int index = 0; index < hitCount; index++)
            {
                interactable = FindInteractable(InteractionHitBuffer[index].collider);
                if (interactable != null)
                {
                    return true;
                }
            }

            interactable = null;
            return false;
        }

        private bool TryFindNearbyInteractable(Camera interactionCamera, out IInteractable interactable)
        {
            interactable = null;

            Vector3 capsuleTop = interactionCamera.transform.position + (Vector3.up * 0.1f);
            Vector3 capsuleBottom = interactionCamera.transform.position - (Vector3.up * Mathf.Max(2f, nearbyFallbackVerticalRange));
            float radius = Mathf.Max(0.5f, nearbyFallbackRadius);
            int overlapCount = Physics.OverlapCapsuleNonAlloc(
                capsuleTop,
                capsuleBottom,
                radius,
                NearbyInteractableBuffer,
                interactLayers,
                queryTriggerInteraction);

            float bestScore = float.PositiveInfinity;
            for (int index = 0; index < overlapCount; index++)
            {
                Collider collider = NearbyInteractableBuffer[index];
                IInteractable candidate = FindInteractable(collider);
                if (candidate == null)
                {
                    continue;
                }

                Vector3 nearestPoint = collider != null ? collider.ClosestPoint(interactionCamera.transform.position) : interactionCamera.transform.position;
                Vector3 horizontalOffset = nearestPoint - interactionCamera.transform.position;
                horizontalOffset.y = 0f;
                float horizontalDistance = horizontalOffset.magnitude;
                if (horizontalDistance > radius)
                {
                    continue;
                }

                float verticalDistance = Mathf.Abs(nearestPoint.y - interactionCamera.transform.position.y);
                float score = horizontalDistance + (verticalDistance * 0.01f);
                if (score < bestScore)
                {
                    bestScore = score;
                    interactable = candidate;
                }
            }

            return interactable != null;
        }

        private void ClearPrompt()
        {
            if (gameManager != null)
            {
                gameManager.SetInteractionPrompt(string.Empty);
            }
        }

        private Camera ResolveInteractionCamera()
        {
            if (viewModeController != null && viewModeController.ActiveCamera != null)
            {
                return viewModeController.ActiveCamera;
            }

            if (playerCamera != null)
            {
                return playerCamera;
            }

            return Camera.main;
        }

        private static IInteractable FindInteractable(Collider collider)
        {
            if (collider == null)
            {
                return null;
            }

            MonoBehaviour[] behaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IInteractable interactable)
                {
                    return interactable;
                }
            }

            return null;
        }

        private static void SortHitsByDistance(RaycastHit[] hits, int hitCount)
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
    }
}
