using PrimeTween;
using Tewi.Factory.Core;
using UnityEngine;

namespace Tewi.Game.UI.Styles
{
    public struct NodeUIContext
    {
        public NodeState nodeState;
    }

    public class NodeUI : UIBase<NodeUIContext>, INodeStatePushed
    {
        public int NodeId { get; set; }

        internal override void OnOpen(NodeUIContext context)
        {
            base.OnOpen(context);
            if (context.nodeState.id != 0)
            {
                NodeId = context.nodeState.id;
                uiManager.playerManager.gameManager.FactoryManager.presentationManager.Subscribe(this);
            }
        }

        internal override void OnClose()
        {
            base.OnClose();
            uiManager.playerManager.gameManager.FactoryManager.presentationManager.Unsubscribe(this);
            NodeId = 0;
        }

        public virtual void OnNodeStatePushed(in NodeState state)
        {

        }

        public void OnSubscribe()
        {

        }

        public void OnUnsubscribe()
        {

        }
    }
}
