using FishNet.Object;
using System.Collections.Generic;
using Tewi.Game.Console;
using Tewi.Game.Factory.Presentation;
using Tewi.Game.Factory.Simulation;
using UnityEngine;

namespace Tewi.Game.Factory
{
    public class NodeCoordinator : NetworkBehaviour
    {
        [SerializeField] internal SimulationManager simulationManager;
        [SerializeField] internal SpatialManager spatialManager;
        [SerializeField] internal PresentationManager presentationManager;

        private List<CreateNodeCommand> _pendingCreates = new();
        private List<DestroyNodeCommand> _pendingDestroys = new();
        
        public uint tickBuffer = 5;

        [ServerRpc(RequireOwnership = false)]
        public void ServerRequestCreateNode(ushort nodeType, Vector3 position, Quaternion rotation)
        {
            uint executionTick = TimeManager.Tick + tickBuffer;
            int newNodeId = simulationManager.GenerateNodeId();

            RpcBroadcastCreateNode(new CreateNodeCommand
            {
                TargetTick = executionTick,
                NodeId = newNodeId,
                NodeType = nodeType,
                Position = position,
                Rotation = rotation
            });
        }

        [ServerRpc(RequireOwnership = false)]
        public void ServerRequestRemoveNode(int nodeId)
        {
            uint executionTick = TimeManager.Tick + tickBuffer;
            RpcBroadcastDestroyNode(new DestroyNodeCommand
            {
                TargetTick = executionTick,
                NodeId = nodeId
            });
        }

        [ObserversRpc(RunLocally = true)]
        private void RpcBroadcastCreateNode(CreateNodeCommand cmd)
        {
            _pendingCreates.Add(cmd);
        }

        [ObserversRpc(RunLocally = true)]
        private void RpcBroadcastDestroyNode(DestroyNodeCommand cmd)
        {
            _pendingDestroys.Add(cmd);
        }

        private void ExecuteLocalCreate(CreateNodeCommand cmd)
        {
            simulationManager.AddNode(cmd.NodeId);
            spatialManager.AddNode(cmd.NodeId, cmd.NodeType, cmd.Position, cmd.Rotation);
        }

        private void ExecuteLocalDestroy(DestroyNodeCommand cmd)
        {
            simulationManager.RemoveNode(cmd.NodeId);
            spatialManager.RemoveNode(cmd.NodeId);
        }

        private void ProcessTickCommands()
        {
            uint currentTick = TimeManager.Tick;

            // creates
            for (int i = _pendingCreates.Count - 1; i >= 0; i--)
            {
                var cmd = _pendingCreates[i];
                if (currentTick >= cmd.TargetTick)
                {
                    ExecuteLocalCreate(cmd);
                    _pendingCreates.RemoveAt(i);
                }
            }

            // destroys
            for (int i = _pendingDestroys.Count - 1; i >= 0; i--)
            {
                var cmd = _pendingDestroys[i];
                if (currentTick >= cmd.TargetTick)
                {
                    ExecuteLocalDestroy(cmd);
                    _pendingDestroys.RemoveAt(i);
                }
            }
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            simulationManager.OnSimulationCompletedInterval += SimulationManager_OnSimulationCompletedInterval;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            simulationManager.OnSimulationCompletedInterval -= SimulationManager_OnSimulationCompletedInterval;
        }

        private void SimulationManager_OnSimulationCompletedInterval()
        {
            ProcessTickCommands();
        }

        #region console commands
        [ConsoleCommand("remove_node_all", "Remove all nodes")]
        public string DebugRemoveAllNodes()
        {
            if (!IsServerStarted)
                return "Only the server can execute this command.";
            
            return "Removed all nodes.";
        }

        [ConsoleCommand("remove_node_range", "Remove a range of nodes")]
        public string DebugRemoveNodeRange(int startID, int endID)
        {
            if (!IsServerStarted)
                return "Only the server can execute this command.";
            for (int id = startID; id <= endID; id++)
            {
                ServerRequestRemoveNode(id);
            }
            return $"Removed nodes from {startID} to {endID}.";
        }

        [ConsoleCommand("remove_node", "Remove a specific node")]
        public string DebugRemoveNode(int nodeID)
        {
            if (!IsServerStarted)
                return "Only the server can execute this command.";
            ServerRequestRemoveNode(nodeID);
            return $"Node {nodeID} has been removed.";
        }

        [ConsoleCommand("spawn_node", "Spawn a node")]
        public string DebugCreateNode(ushort nodeType, Vector3 position, Quaternion rotation)
        {
            if (!IsServerStarted)
                return "Only the server can execute this command.";
            ServerRequestCreateNode(nodeType, position, rotation);
            return $"Node {nodeType} has been created at position {position}.";
        }

        [ConsoleCommand("spawn_node_rect", "Spawn a rectangular array of nodes")]
        public string DebugCreateRectangleArray(int rows, int cols, float spacing, ushort type, Vector3 startPos)
        {
            float offsetX = (rows - 1) * spacing / 2f;
            float offsetZ = (cols - 1) * spacing / 2f;

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

                    ServerRequestCreateNode(type, spawnPos, Quaternion.identity);
                    count++;
                }
            }

            return $"Node rectangle array {rows}x{cols} has been created with {count} nodes.";
        }
        #endregion
    }
    public struct CreateNodeCommand
    {
        public uint TargetTick; // 该指令执行的逻辑时刻
        public int NodeId;      // 由服务器统一分配的 ID
        public ushort NodeType;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    public struct DestroyNodeCommand
    {
        public uint TargetTick;
        public int NodeId;
    }
}
