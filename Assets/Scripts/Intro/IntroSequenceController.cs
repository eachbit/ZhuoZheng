using System.Collections;
using UnityEngine;

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
    }
}
