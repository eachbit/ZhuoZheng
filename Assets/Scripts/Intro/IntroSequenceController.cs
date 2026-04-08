using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ZhuozhengYuan
{
    public class IntroSequenceController : MonoBehaviour
    {
        public GardenGameManager manager;
        public Transform playerIntroPose;
        public Transform playerPostIntroPose;
        public Transform oldGardenerActor;
        public Transform oldGardenerEntrancePoint;
        public Transform oldGardenerDialoguePoint;
        public Transform oldGardenerExitPoint;
        public float oldGardenerWalkSpeed = 1.1f;
        public float entrancePause = 0.4f;
        public float exitPause = 0.2f;
        public DialogueLine[] dialogueLines;
        public bool usePoseAsViewpoint = false;

        [Header("Old Gardener Visual")]
        public GameObject oldGardenerVisualPrefab;
        public string oldGardenerVisualEditorAssetPath = string.Empty;
        public Vector3 oldGardenerVisualLocalPosition = Vector3.zero;
        public Vector3 oldGardenerVisualLocalEulerAngles = Vector3.zero;
        public Vector3 oldGardenerVisualLocalScale = Vector3.one;
        public bool autoFitSpawnedVisualHeight = true;
        public float spawnedVisualTargetHeight = 1.72f;
        public bool autoGroundSpawnedVisual = true;
        public bool hidePlaceholderRenderers = true;
        public bool hidePlaceholderColliders = true;

        [Header("Runtime Safety Support")]
        public bool createRuntimeSupportPlatforms = true;
        public Vector2 introSupportSize = new Vector2(40f, 40f);
        public Vector2 postSupportSize = new Vector2(40f, 40f);
        public float supportThickness = 2f;
        public float supportTopOffset = -0.05f;
        public bool onlyCreateSupportWhenNoNearbyGround = true;
        public float supportGroundCheckHeight = 1.5f;
        public float supportGroundCheckDistance = 6f;
        public string ignoredGroundObjectName = "GlobalInvisibleGround";

        private Transform _runtimeSupportRoot;
        private GameObject _introSupportPlatform;
        private GameObject _postSupportPlatform;
        private GameObject _spawnedOldGardenerVisual;

        public void PrepareRuntimeSupports()
        {
            DestroyRuntimeSupports();
        }

        public void SnapPlayerToPostPoseIfAvailable()
        {
            if (manager == null || manager.playerController == null || playerPostIntroPose == null)
            {
                return;
            }

            PrepareRuntimeSupports();
            SnapPlayerToConfiguredPose(playerPostIntroPose);
        }

        public IEnumerator PlayIntroSequence()
        {
            if (manager == null)
            {
                yield break;
            }

            if (playerIntroPose != null && manager.playerController != null)
            {
                SnapPlayerToConfiguredPose(playerIntroPose);
            }

            EnsureOldGardenerVisualReady();

            if (oldGardenerActor != null)
            {
                oldGardenerActor.gameObject.SetActive(true);
            }

            if (oldGardenerActor != null && oldGardenerEntrancePoint != null)
            {
                oldGardenerActor.position = oldGardenerEntrancePoint.position;
                oldGardenerActor.rotation = oldGardenerEntrancePoint.rotation;
            }

            if (manager.runtimeUI != null)
            {
                yield return manager.runtimeUI.Fade(1f, 0f, 1f);
            }

            if (oldGardenerActor != null && oldGardenerDialoguePoint != null)
            {
                yield return MoveActor(oldGardenerActor, oldGardenerDialoguePoint.position);
            }

            if (entrancePause > 0f)
            {
                yield return new WaitForSeconds(entrancePause);
            }

            bool dialogueCompleted = false;
            manager.ShowDialogue(GetDialogueLines(), delegate { dialogueCompleted = true; });
            while (!dialogueCompleted)
            {
                yield return null;
            }

            if (exitPause > 0f)
            {
                yield return new WaitForSeconds(exitPause);
            }

            if (oldGardenerActor != null && oldGardenerExitPoint != null)
            {
                yield return MoveActor(oldGardenerActor, oldGardenerExitPoint.position);
            }

            if (oldGardenerActor != null)
            {
                oldGardenerActor.gameObject.SetActive(false);
            }

            if (playerPostIntroPose != null && manager.playerController != null)
            {
                SnapPlayerToConfiguredPose(playerPostIntroPose);
            }

            manager.MarkIntroPlayed();
            manager.CompleteIntro();
        }

        private DialogueLine[] GetDialogueLines()
        {
            if (dialogueLines != null && dialogueLines.Length > 0)
            {
                return dialogueLines;
            }

            return new[]
            {
                new DialogueLine { speaker = "\u8001\u56ed\u5320", text = "\u5b50\u6765\u77e3\u3002" },
                new DialogueLine { speaker = "\u8001\u56ed\u5320", text = "\u6b64\u56ed\u5931\u5176\u9b42\u9b44\u4e45\u77e3\u3002\u6c34\u4e0d\u6d41\uff0c\u5f71\u4e0d\u771f\uff0c\u58f0\u4e0d\u4f20\uff0c\u5c71\u4e0d\u7eed\u3002" },
                new DialogueLine { speaker = "\u8001\u56ed\u5320", text = "\u5b50\u867d\u672a\u8c19\u8425\u9020\u4e4b\u672f\uff0c\u7136\u624b\u4e2d\u6b8b\u5377\uff0c\u7eb5\u4f7f\u7247\u695a\u96f6\u7f23\uff0c\u4ea6\u8db3\u4ee5\u5524\u5176\u751f\u673a\u3002" },
                new DialogueLine { speaker = "\u8001\u56ed\u5320", text = "\u53bb\u89c5\u6563\u4f5a\u4e4b\u7bc7\uff0c\u590d\u6b64\u56ed\u65e7\u89c2\u3002\u8001\u673d\u4e8e\u7ec8\u9014\u76f8\u5019\u3002" }
            };
        }

        private void EnsureSupportPlatform(ref GameObject platformObject, string platformName, Transform pose, Vector2 platformSize)
        {
            DestroyRuntimeSupports();
        }

        private IEnumerator MoveActor(Transform actor, Vector3 targetPosition)
        {
            if (actor == null)
            {
                yield break;
            }

            while (Vector3.Distance(actor.position, targetPosition) > 0.03f)
            {
                Vector3 nextPosition = Vector3.MoveTowards(actor.position, targetPosition, oldGardenerWalkSpeed * Time.deltaTime);
                Vector3 lookDirection = targetPosition - actor.position;
                lookDirection.y = 0f;
                if (lookDirection.sqrMagnitude > 0.0001f)
                {
                    actor.rotation = Quaternion.Slerp(
                        actor.rotation,
                        Quaternion.LookRotation(lookDirection.normalized, Vector3.up),
                        Time.deltaTime * 8f);
                }

                actor.position = nextPosition;
                yield return null;
            }
        }

        private bool HasNearbyUsableGround(Transform pose)
        {
            return true;
        }

        private static void SetPlatformActive(GameObject platformObject, bool active)
        {
            if (platformObject != null && platformObject.activeSelf != active)
            {
                platformObject.SetActive(active);
            }
        }

        private void DestroyRuntimeSupports()
        {
            if (_introSupportPlatform != null)
            {
                Destroy(_introSupportPlatform);
                _introSupportPlatform = null;
            }

            if (_postSupportPlatform != null)
            {
                Destroy(_postSupportPlatform);
                _postSupportPlatform = null;
            }

            if (_runtimeSupportRoot != null)
            {
                Destroy(_runtimeSupportRoot.gameObject);
                _runtimeSupportRoot = null;
            }
        }

        private void SnapPlayerToConfiguredPose(Transform pose)
        {
            if (manager == null || manager.playerController == null || pose == null)
            {
                return;
            }

            if (usePoseAsViewpoint)
            {
                manager.playerController.SnapViewToPose(pose);
                return;
            }

            manager.playerController.SnapToPose(pose);
        }

        private void EnsureOldGardenerVisualReady()
        {
            if (oldGardenerActor == null)
            {
                return;
            }

            bool hasAuthoredVisual = HasAuthoredVisualChild(oldGardenerActor);
            if (!hasAuthoredVisual && _spawnedOldGardenerVisual == null)
            {
                GameObject visualPrefab = ResolveOldGardenerVisualPrefab();
                if (visualPrefab != null)
                {
                    _spawnedOldGardenerVisual = Instantiate(visualPrefab, oldGardenerActor, false);
                    _spawnedOldGardenerVisual.name = "OldGardenerVisual";
                    ApplySpawnedVisualTransform(_spawnedOldGardenerVisual.transform);
                }
            }

            bool hasAnyVisual = hasAuthoredVisual || _spawnedOldGardenerVisual != null;
            if (hasAnyVisual)
            {
                SetPlaceholderPresentation(false);
            }
        }

        private GameObject ResolveOldGardenerVisualPrefab()
        {
            if (oldGardenerVisualPrefab != null)
            {
                return oldGardenerVisualPrefab;
            }

#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(oldGardenerVisualEditorAssetPath))
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(oldGardenerVisualEditorAssetPath);
            }

            string[] modelGuids = AssetDatabase.FindAssets("laoren t:Model", new[] { "Assets/Figure" });
            if (modelGuids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(modelGuids[0]);
                return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
#endif

            return null;
        }

        private bool HasAuthoredVisualChild(Transform root)
        {
            if (root == null)
            {
                return false;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                if (child == null || child.gameObject == _spawnedOldGardenerVisual)
                {
                    continue;
                }

                if (child.GetComponentInChildren<Renderer>(true) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplySpawnedVisualTransform(Transform visualTransform)
        {
            if (visualTransform == null || oldGardenerActor == null)
            {
                return;
            }

            visualTransform.localPosition = oldGardenerVisualLocalPosition;
            visualTransform.localRotation = Quaternion.Euler(oldGardenerVisualLocalEulerAngles);
            visualTransform.localScale = oldGardenerVisualLocalScale;

            if (autoFitSpawnedVisualHeight && spawnedVisualTargetHeight > 0.01f)
            {
                if (TryGetCombinedBounds(visualTransform.gameObject, out Bounds initialBounds) && initialBounds.size.y > 0.01f)
                {
                    float scaleFactor = spawnedVisualTargetHeight / initialBounds.size.y;
                    visualTransform.localScale = oldGardenerVisualLocalScale * scaleFactor;
                }
            }

            if (autoGroundSpawnedVisual && TryGetCombinedBounds(visualTransform.gameObject, out Bounds fittedBounds))
            {
                float yOffset = oldGardenerActor.position.y - fittedBounds.min.y;
                visualTransform.position += Vector3.up * yOffset;
            }
        }

        private void SetPlaceholderPresentation(bool visible)
        {
            if (oldGardenerActor == null)
            {
                return;
            }

            if (hidePlaceholderRenderers)
            {
                Renderer[] renderers = oldGardenerActor.GetComponents<Renderer>();
                for (int index = 0; index < renderers.Length; index++)
                {
                    renderers[index].enabled = visible;
                }
            }

            if (hidePlaceholderColliders)
            {
                Collider[] colliders = oldGardenerActor.GetComponents<Collider>();
                for (int index = 0; index < colliders.Length; index++)
                {
                    colliders[index].enabled = visible;
                }
            }
        }

        private static bool TryGetCombinedBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return true;
        }
    }
}
