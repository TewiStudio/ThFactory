using Tewi.Game.Player;
using Tewi.Game.Player.UI.Styles;
using Tewi.Game.Network;
using Tewi.Game.Factory.Core;

namespace Tewi.Game.Interactable.Nodes
{
    public class Node : InteractableItem, INodeStatePushed
    {
        public NetworkGameManager gameManager;
        public int currentRecipeID = 0;
        public int nodeId { get; set; }

        protected NodeState lastState;

        public override void OnStartClient()
        {
            base.OnStartClient();
           gameManager.presentationManager.Subscribe(this);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            gameManager.presentationManager.Unsubscribe(this);
        }

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
