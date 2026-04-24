using Tewi.Game.Factory.Core;
using Tewi.Game.Player;
using Tewi.Game.Player.UI.Styles;

namespace Tewi.Game.Interactable.Nodes
{
    public class Node : InteractableItem, INodeStatePushed
    {
        public int currentRecipeID = 0;
        public int nodeId { get; set; }

        protected NodeState lastState;

        public override void OnInteract(PlayerManager player)
        {
            base.OnInteract(player);
            player.uiManager.Open<ProcessorUI, NodeUIContext>(new() { nodeState = lastState });
        }

        public void OnNodeStatePushed(in NodeState state)
        {
            lastState = state;
        }
    }
}
