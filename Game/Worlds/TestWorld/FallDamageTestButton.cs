using UnityEngine;
using Tewi.Game.Player;
using Tewi.Game.Interactable;

namespace Tewi.Game.Worlds.TestWorld
{
    public class FallDamageTestButton : InteractableItem
    {
        public Transform teleportPosition;

        public override void OnInteract(PlayerManager player)
        {
            base.OnInteract(player);
            player.character.TeleportPosition(teleportPosition.position);
        }
    }
}