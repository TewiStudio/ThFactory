using PrimeTween;
using Tewi.Game.Factory.Core;
using UnityEngine;

namespace Tewi.Game.Player.UI.Styles
{
    public struct NodeUIContext
    {
        public NodeState nodeState;
    }

    public class NodeUI : UIBase<NodeUIContext>, INodeStatePushed
    {
        public int nodeId { get; set; }

        internal override void OnOpen(NodeUIContext context)
        {
            base.OnOpen(context);
            if (context.nodeState.id != 0)
            {
                nodeId = context.nodeState.id;
                uiManager.playerManager.gameManager.presentationManager.Subscribe(this);
            }
        }

        internal override void OnClose()
        {
            base.OnClose();
            uiManager.playerManager.gameManager.presentationManager.Unsubscribe(this);
            nodeId = 0;
        }

        public virtual void OnNodeStatePushed(in NodeState state)
        {

        }
    }
}
