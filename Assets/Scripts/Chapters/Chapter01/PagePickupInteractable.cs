using UnityEngine;

namespace ZhuozhengYuan
{
    public class PagePickupInteractable : MonoBehaviour, IInteractable
    {
        public Chapter01Director director;
        public string pageDisplayName = "\u300a\u957f\u7269\u5fd7\u300b\u6b8b\u9875";

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

            return "\u6309 E \u62fe\u53d6" + pageDisplayName;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (director == null)
            {
                Debug.LogWarning(name + " is missing a Chapter01Director reference.");
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
