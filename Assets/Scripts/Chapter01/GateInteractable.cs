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

        [SerializeField]
        private bool isOpened;

        public bool IsOpened
        {
            get { return isOpened; }
        }

        public bool CanInteract(PlayerInteractor interactor)
        {
            return director != null && !isOpened;
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

            return "按 E 启动" + gateDisplayName;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (director == null)
            {
                Debug.LogWarning(name + " 未绑定 Chapter01Director。");
                return;
            }

            director.HandleGateInteraction(this);
        }

        public void ApplyOpenedState(bool opened)
        {
            isOpened = opened;
            SetObjectsActive(closedStateObjects, !opened);
            SetObjectsActive(openedStateObjects, opened);
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
