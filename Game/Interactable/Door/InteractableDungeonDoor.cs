using UnityEngine;
using Tewi.Game.Player;
using Tewi.Helpers.Extensions;

namespace Tewi.Game.Interactable.Door
{
    public class InteractableDungeonDoor : InteractableItem
    {
        public InteractableDungeonDoor otherDoor;
        public Vector3 teleportPlayerOffset = Vector3.forward.SetY(-.5f);
        public Vector3 TeleportPlayerPosition => transform.position + transform.rotation * teleportPlayerOffset;

        public override void OnInteract(PlayerManager player)
        {
            base.OnInteract(player);
            player.character.TeleportPosition(otherDoor.TeleportPlayerPosition);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(TeleportPlayerPosition, Vector3.one * 0.1f);
        }
    }
}
