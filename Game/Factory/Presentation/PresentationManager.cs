using FishNet.Object;
using Unity.Collections;
using Tewi.Game.Factory.Core;

namespace Tewi.Game.Factory.Presentation
{
    public class PresentationManager : NetworkBehaviour
    {
        public delegate void OnNodeSimulationCompleted(NativeArray<NodeState>.ReadOnly nodes);
        public event OnNodeSimulationCompleted NodeSimulationCompletedEvent;

        public void NotifyNodeSimulationCompleted(NativeArray<NodeState>.ReadOnly nodes)
        {
            NodeSimulationCompletedEvent?.Invoke(nodes);
        }
    }
}
