using FishNet.Object;
using FishNet.Object.Synchronizing;
using Tewi.Game.Player;
using Tewi.Game.Player.UI.Styles;

namespace Tewi.Game.Interactable
{
    public class InteractableItem : NetworkBehaviour
    {
        public string defaultPlayerLookText = "交互";
        public string itemName = "Interactable Item";
        public float interactTime = 0f;

        public readonly SyncVar<bool> interactable = new(true);

        public virtual void OnInteract(PlayerManager player)
        {

        }

        public virtual void OnPlayerLookAt(PlayerManager player)
        {
            player.uiManager.Open<InteractUI, InteractUIContext>(new() { text = defaultPlayerLookText });
            //Debug.Log($"Look at {transform.name}");
        }

        public virtual void OnPlayerNotLooking(PlayerManager player)
        {
            player.uiManager.Close<InteractUI>();
            //Debug.Log($"Not looking {transform.name}");
        }

        public virtual void UpdatePlayerShowsTextUI(PlayerManager player)
        {
            player.uiManager.Open<InteractUI, InteractUIContext>(new() { text = defaultPlayerLookText });
        }
    }
}