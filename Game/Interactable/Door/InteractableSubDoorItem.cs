using Tewi.Game.Player;

namespace Tewi.Game.Interactable.Door
{
    public class InteractableSubDoorItem : NetworkInteractableItem
    {
        public InteractableDoorItem interactableParent;

        public override void OnInteract(PlayerManager player)
        {
            base.OnInteract(player);
            if (!interactableParent.isOpen)
            {
                interactTime = interactableParent.InteractTime;
                defaultPlayerLookText = interactableParent.DefaultPlayerLookText;
            }
            else
                interactableParent.OnInteract(player);
        }

        public override void OnPlayerLookAt(PlayerManager player)
        {
            interactTime = interactableParent.InteractTime;
            defaultPlayerLookText = interactableParent.DefaultPlayerLookText;
            base.OnPlayerLookAt(player);
        }
    }
}