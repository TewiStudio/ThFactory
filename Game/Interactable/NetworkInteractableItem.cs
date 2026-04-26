using FishNet.Object;
using FishNet.Object.Synchronizing;
using Tewi.Game.Player;

namespace Tewi.Game.Interactable
{
    public class NetworkInteractableItem : NetworkBehaviour, IInteractable
    {
        public string itemName = "NetworkInteractableItem";
        public float interactTime = 0f;
        public string defaultPlayerLookText = "交互";

        public readonly SyncVar<bool> interactable = new(true);

        public string ItemName => itemName;
        public float InteractTime => interactTime;
        public bool IsInteractable => interactable.Value;
        public string DefaultPlayerLookText => defaultPlayerLookText;

        public virtual void OnInteract(PlayerManager player) { }

        public virtual void OnPlayerLookAt(PlayerManager player) => this.HandleLookAt(player);

        public virtual void OnPlayerNotLooking(PlayerManager player) => this.HandleNotLooking(player);

        public virtual void UpdatePlayerShowsTextUI(PlayerManager player) => this.HandleUpdatePlayerShowsTextUI(player);
    }
}