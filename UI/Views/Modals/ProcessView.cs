using Tewi.Factory.Core;
using Tewi.Game.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tewi.Game.UI.Views
{
    public struct ProcessData
    {
        public int nodeID;
    }

    public sealed class ProcessView : ViewUI<VisualElement, ProcessData>, INodeStatePushed
    {
        public int NodeId { get; set; }
        public override string ElementName => "ProcessContainer";

        public override void SetData(ProcessData data)
        {
            NodeId = data.nodeID;
        }

        protected override void OnOpening()
        {
            Debug.Log($"ProcessView: Opened with nodeID: {NodeId}");
            GameManager.FactoryManager.presentationManager.Subscribe(this);
        }

        protected override void OnClosed()
        {
            Debug.Log($"ProcessView: Closed with nodeID: {NodeId}");
            GameManager.FactoryManager.presentationManager.Unsubscribe(this);
        }

        public void OnSubscribe()
        {
            Debug.Log($"ProcessView: Subscribed with nodeID: {NodeId}");
        }

        public void OnUnsubscribe()
        {
            Debug.Log($"ProcessView: Unsubscribed with nodeID: {NodeId}");
            NodeId = 0;
        }

        public void OnNodeStatePushed(in NodeState state)
        {
            Debug.Log($"ProcessView: NodeState pushed for nodeID: {NodeId}, currentStatus: {state.currentStatus}, progressTicks: {state.progressTicks}");
        }
    }
}
