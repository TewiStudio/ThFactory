using Tewi.Game.Player;
using Tewi.Game.UI.Styles;

namespace Tewi.Game.Interactable
{
    public interface IInteractable
    {
        string DefaultPlayerLookText { get; }
        string ItemName { get; }
        float InteractTime { get; }
        bool IsInteractable { get; }

        void OnInteract(PlayerManager player);

        public void OnPlayerLookAt(PlayerManager player);

        public void OnPlayerNotLooking(PlayerManager player);

        public void UpdatePlayerShowsTextUI(PlayerManager player);
    }

    public static class InteractableExtensions
    {
        public static void HandleLookAt(this IInteractable item, PlayerManager player)
        {
            player.uiManager.Open<InteractUI, InteractUIContext>(new() { text = item.DefaultPlayerLookText });
        }

        public static void HandleNotLooking(this IInteractable item, PlayerManager player)
        {
            player.uiManager.Close<InteractUI>();
        }

        public static void HandleUpdatePlayerShowsTextUI(this IInteractable item, PlayerManager player)
        {
            player.uiManager.Open<InteractUI, InteractUIContext>(new() { text = item.DefaultPlayerLookText });
        }
    }
}
