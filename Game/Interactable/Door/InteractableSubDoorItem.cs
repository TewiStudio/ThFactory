using Tewi.Game.Player;

namespace Tewi.Game.Interactable.Door
{
    public class InteractableSubDoorItem : InteractableItem
    {
        public InteractableDoorItem interactableParent;

        public override void OnInteract(PlayerManager player)
        {
            base.OnInteract(player);
            if (!interactableParent.isOpen)
            {
                interactTime = interactableParent.interactTime;
                defaultPlayerLookText = interactableParent.defaultPlayerLookText;
            }
            else
                interactableParent.OnInteract(player);
        }

        public override void OnPlayerLookAt(PlayerManager player)
        {
            interactTime = interactableParent.interactTime;
            defaultPlayerLookText = interactableParent.defaultPlayerLookText;
            base.OnPlayerLookAt(player);
        }
    }
}