using FishNet.Object;
using Tewi.Game.Factory.Presentation;
using Tewi.Game.Factory.Simulation;
using UnityEngine;

namespace Tewi.Game.Factory
{
    public class NodeCoordinator : NetworkBehaviour
    {
        [SerializeField] internal SimulationManager simulationManager;
        [SerializeField] internal PresentationManager presentationManager;
        
        [Server]
        public void CreateNode(int nodeType, Vector3 position, Quaternion rotation)
        {
            int newNodeID = simulationManager.AddNode();
            presentationManager.AddObserver(newNodeID, position, rotation);
        }

        [Server]
        public void DestroyNode(int nodeID)
        {
            simulationManager.RemoveNode(nodeID);
            presentationManager.RemoveObserver(nodeID);
        }
    }
}
