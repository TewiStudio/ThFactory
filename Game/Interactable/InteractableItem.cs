using UnityEngine;
using Tewi.Game.Player;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace Tewi.Game.Interactable
{
    public class InteractableItem : NetworkBehaviour
    {
        public string defaultPlayerLookText = "½»»¥";
        public string itemName = "Interactable Item";
        public float interactTime = 0f;

        public readonly SyncVar<bool> interactable = new(true);

        public virtual void OnInteract(PlayerManager player)
        {

        }

        public virtual void OnPlayerLookAt(PlayerManager player)
        {
            player.uiManager.SetInteractActive(true, defaultPlayerLookText);
            //Debug.Log($"Look at {transform.name}");
        }

        public virtual void OnPlayerNotLooking(PlayerManager player)
        {
            player.uiManager.SetInteractActive(false);
            //Debug.Log($"Not looking {transform.name}");
        }

        public virtual void UpdatePlayerShowsTextUI(PlayerManager player)
        {
            player.uiManager.SetInteractActive(true, defaultPlayerLookText);
        }
    }
}