using FishNet.Object;
using Unity.Collections;
using Tewi.Game.Network.Core;

namespace Tewi.Game.Network.Presentation
{
    public class PresentationManager : NetworkBehaviour
    {
        public delegate void OnNodeSimulationCompleted(NativeArray<NodeState> nodes);
        public event OnNodeSimulationCompleted NodeSimulationCompletedEvent;

        public void NotifyNodeSimulationCompleted(NativeArray<NodeState> nodes)
        {
            NodeSimulationCompletedEvent?.Invoke(nodes);
        }
    }
}
