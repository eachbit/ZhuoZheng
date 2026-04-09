using UnityEngine;

namespace ZhuozhengYuan
{
    public class WaterDirectionInteractable : MonoBehaviour, IInteractable
    {
        public Chapter01Director director;
        public string interactionLabel = "\u6c34\u6d41\u65b9\u5411";

        public bool CanInteract(PlayerInteractor interactor)
        {
            return director != null;
        }

        public string GetInteractionPrompt(PlayerInteractor interactor)
        {
            if (director == null)
            {
                return string.Empty;
            }

            return director.GetFlowInteractionPrompt(interactionLabel);
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (director == null)
            {
                Debug.LogWarning(name + " is missing a Chapter01Director reference.");
                return;
            }

            director.HandleFlowSelectorInteraction();
        }
    }
}
