using UnityEngine;

namespace ZhuozhengYuan
{
    public class WaterDirectionInteractable : MonoBehaviour, IInteractable
    {
        public Chapter01Director director;
        public string interactionLabel = "调水流向";

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
                Debug.LogWarning(name + " 未绑定 Chapter01Director。");
                return;
            }

            director.HandleFlowSelectorInteraction();
        }
    }
}
