using Tewi.Game.Player;
using Tewi.Game.UI.Styles;
using Tewi.Game.UI.Views;

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
            player.uiToolkitManager.Open<InteractLabelView, InteractLabelViewContext>(new() { text = item.DefaultPlayerLookText });
        }

        public static void HandleNotLooking(this IInteractable item, PlayerManager player)
        {
            player.uiToolkitManager.Close<InteractLabelView>();
        }

        public static void HandleUpdatePlayerShowsTextUI(this IInteractable item, PlayerManager player)
        {
            player.uiToolkitManager.Open<InteractLabelView, InteractLabelViewContext>(new() { text = item.DefaultPlayerLookText });
        }
    }
}
