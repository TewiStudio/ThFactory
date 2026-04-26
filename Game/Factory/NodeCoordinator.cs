using UnityEngine;
using FishNet.Object;
using Tewi.Game.Console;
using Tewi.Game.Factory.Presentation;
using Tewi.Game.Factory.Simulation;

namespace Tewi.Game.Factory
{
    public class NodeCoordinator : NetworkBehaviour
    {
        [SerializeField] internal SimulationManager simulationManager;
        [SerializeField] internal SpatialManager spatialManager;
        [SerializeField] internal PresentationManager presentationManager;

        [Server]
        public void CreateNode(ushort nodeType, Vector3 position, Quaternion rotation)
        {
            int newNodeID = simulationManager.AddNode();
            spatialManager.AddNode(newNodeID, nodeType, position, rotation);
        }

        [Server]
        public void DestroyNode(int nodeID)
        {
            simulationManager.RemoveNode(nodeID);
            spatialManager.RemoveNode(nodeID);
        }

        [Server]
        public void RemoveAll()
        {
            foreach (var item in simulationManager.NodesSnapshot)
            {
                DestroyNode(item.id);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ServerRequestCreateNode(ushort nodeType, Vector3 position, Quaternion rotation)
        {
            CreateNode(nodeType, position, rotation);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ServerRequestRemoveNode(int nodeID)
        {
            DestroyNode(nodeID);
        }

        [ConsoleCommand("remove_node_all", "Remove all nodes")]
        public string DebugRemoveAllNodes()
        {
            if (!IsServerStarted)
                return "Only the server can execute this command.";
            RemoveAll();
            return "Removed all nodes.";
        }

        [ConsoleCommand("remove_node_range", "Remove a range of nodes")]
        public string DebugRemoveNodeRange(int startID, int endID)
        {
            if (!IsServerStarted)
                return "Only the server can execute this command.";
            for (int id = startID; id <= endID; id++)
            {
                DestroyNode(id);
            }
            return $"Removed nodes from {startID} to {endID}.";
        }

        [ConsoleCommand("remove_node", "Remove a specific node")]
        public string DebugRemoveNode(int nodeID)
        {
            if (!IsServerStarted)
                return "Only the server can execute this command.";
            DestroyNode(nodeID);
            return $"Node {nodeID} has been removed.";
        }

        [ConsoleCommand("spawn_node", "Spawn a node")]
        public string DebugCreateNode(ushort nodeType, Vector3 position, Quaternion rotation)
        {
            if (!IsServerStarted)
                return "Only the server can execute this command.";
            CreateNode(nodeType, position, rotation);
            return $"Node {nodeType} has been created at position {position}.";
        }

        [ConsoleCommand("spawn_node_rect", "Spawn a rectangular array of nodes")]
        public string DebugCreateRectangleArray(int rows, int cols, float spacing, ushort type)
        {
            float offsetX = (rows - 1) * spacing / 2f;
            float offsetZ = (cols - 1) * spacing / 2f;

            Vector3 startPos = transform.position;

            int count = 0;
            for (int x = 0; x < rows; x++)
            {
                for (int z = 0; z < cols; z++)
                {
                    Vector3 spawnPos = startPos + new Vector3(
                        x * spacing - offsetX,
                        0,
                        z * spacing - offsetZ
                    );

                    CreateNode(type, spawnPos, Quaternion.identity);
                    count++;
                }
            }

            return $"Node rectangle array {rows}x{cols} has been created with {count} nodes.";
        }

    }
}
