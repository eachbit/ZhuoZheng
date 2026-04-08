using UnityEngine;

namespace ZhuozhengYuan
{
    public class PagePickupInteractable : MonoBehaviour, IInteractable
    {
        public Chapter01Director director;
        public string pageDisplayName = "《长物志》残页";

        [SerializeField]
        private bool isAvailable;

        public bool CanInteract(PlayerInteractor interactor)
        {
            return director != null && isAvailable;
        }

        public string GetInteractionPrompt(PlayerInteractor interactor)
        {
            if (!isAvailable || director == null)
            {
                return string.Empty;
            }

            return "按 E 拾取" + pageDisplayName;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (director == null)
            {
                Debug.LogWarning(name + " 未绑定 Chapter01Director。");
                return;
            }

            director.HandlePagePickup(this);
        }

        public void SetAvailability(bool available)
        {
            isAvailable = available;
            gameObject.SetActive(available);
        }
    }
}
