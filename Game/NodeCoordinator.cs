using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using Tewi.Console;
using Tewi.Game.Network;
using Tewi.Factory.Simulation;
using Tewi.Factory.Presentation;

namespace Tewi.Game
{
    public class NodeCoordinator : NetworkBehaviour
    {
        [SerializeField] private NetworkGameManager gameManager;
        private SimulationManager simulationManager => gameManager.SimulationManager;
        private SpatialManager spatialManager => gameManager.SpatialManager;
        private PresentationManager presentationManager => gameManager.PresentationManager;

        private List<CreateNodeCommand> _pendingCreates = new();
        private List<DestroyNodeCommand> _pendingDestroys = new();
        private List<ChangeRecipeCommand> _pendingChangeRecipes = new();
        private List<ChangeResourceCommand> _pendingChangeResources = new();
        
        public uint tickBuffer = 3;

        [ServerRpc(RequireOwnership = false)]
        public void ServerRequestCreateNode(ushort nodeType, Vector3 position, Quaternion rotation)
        {
            uint executionTick = simulationManager.CurrentTick + tickBuffer;
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
            uint executionTick = simulationManager.CurrentTick + tickBuffer;
            RpcBroadcastDestroyNode(new DestroyNodeCommand
            {
                TargetTick = executionTick,
                NodeId = nodeId
            });
        }

        [ServerRpc(RequireOwnership = false)]
        public void ServerRequestChangeRecipe(int nodeId, ushort newRecipeId)
        {
            uint executionTick = simulationManager.CurrentTick + tickBuffer;
            RpcBroadcastChangeRecipe(new ChangeRecipeCommand
            {
                TargetTick = executionTick,
                NodeId = nodeId,
                NewRecipeId = newRecipeId
            });
        }

        [ServerRpc(RequireOwnership = false)]
        public void ServerRequestChangeResource(int nodeId, SlotType slot, ushort newResourceId, ushort amount)
        {
            uint executionTick = simulationManager.CurrentTick + tickBuffer;
            RpcBroadcastChangeResource(new ChangeResourceCommand
            {
                TargetTick = executionTick,
                NodeId = nodeId,
                NewResourceId = newResourceId,
                SlotType = slot,
                Amount = amount
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

        [ObserversRpc(RunLocally = true)]
        private void RpcBroadcastChangeRecipe(ChangeRecipeCommand cmd)
        {
            _pendingChangeRecipes.Add(cmd);
        }

        [ObserversRpc(RunLocally = true)]
        private void RpcBroadcastChangeResource(ChangeResourceCommand cmd)
        {
            _pendingChangeResources.Add(cmd);
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
            uint currentTick = simulationManager.CurrentTick;

            // creates
            for (int i = _pendingCreates.Count - 1; i >= 0; i--)
            {
                var cmd = _pendingCreates[i];
                if (currentTick == cmd.TargetTick)
                {
                    ExecuteLocalCreate(cmd);
                    _pendingCreates.RemoveAt(i);
                }
                else if (currentTick > cmd.TargetTick)
                {
                    Debug.LogWarning($"Create node command {cmd.NodeId} is delayed and failed.");
                    _pendingCreates.RemoveAt(i);
                }
            }

            // destroys
            for (int i = _pendingDestroys.Count - 1; i >= 0; i--)
            {
                var cmd = _pendingDestroys[i];
                if (currentTick == cmd.TargetTick)
                {
                    ExecuteLocalDestroy(cmd);
                    _pendingDestroys.RemoveAt(i);
                }
                else if (currentTick > cmd.TargetTick)
                {
                    Debug.LogWarning($"Destroy node command {cmd.NodeId} is delayed and failed.");
                    _pendingCreates.RemoveAt(i);
                }
            }

            // change recipes
            for (int i = _pendingChangeRecipes.Count - 1; i >= 0; i--)
            {
                var cmd = _pendingChangeRecipes[i];
                if (currentTick == cmd.TargetTick)
                {
                    simulationManager.ChangeRecipe(cmd.NodeId, cmd.NewRecipeId);
                    _pendingChangeRecipes.RemoveAt(i);
                }
                else if (currentTick > cmd.TargetTick)
                {
                    Debug.LogWarning($"Change recipe command {cmd.NodeId} is delayed and failed.");
                    _pendingCreates.RemoveAt(i);
                }
            }

            // change resources
            for (int i = _pendingChangeResources.Count - 1; i >= 0; i--)
            {
                var cmd = _pendingChangeResources[i];
                if (currentTick == cmd.TargetTick)
                {
                    simulationManager.ChangeResource(cmd.NodeId, cmd.SlotType, cmd.NewResourceId, cmd.Amount);
                    _pendingChangeResources.RemoveAt(i);
                }
                else if (currentTick > cmd.TargetTick)
                {
                    Debug.LogWarning($"Change resource command {cmd.NodeId} is delayed and failed.");
                    _pendingCreates.RemoveAt(i);
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

        [ConsoleCommand("set_node_recipe", "Changes the recipe of a node.")]
        public string DebugSetNodeRecipe(int nodeId, int newRecipeId)
        {
            ServerRequestChangeRecipe(nodeId, (ushort)newRecipeId);
            return $"Requested recipe change for Node {nodeId} to Recipe {newRecipeId}.";
        }

        [ConsoleCommand("set_node_recipe_range", "Changes the recipe of a range of nodes.")]
        public string DebugSetNodeRecipeFromRange(int startNodeId, int endNodeId, int newRecipeId)
        {
            for (int nodeId = startNodeId; nodeId <= endNodeId; nodeId++)
            {
                ServerRequestChangeRecipe(nodeId, (ushort)newRecipeId);
            }
            return $"Requested recipe change for Nodes {startNodeId} to {endNodeId} to Recipe {newRecipeId}.";
        }

        [ConsoleCommand("set_node_res", "Changes the resource in a specific slot of a node.")]
        public string DebugSetNodeResource(int nodeId, SlotType slot, ushort resourceId, ushort amount)
        {
            ServerRequestChangeResource(nodeId, slot, resourceId, amount);
            return $"Requested resource change for Node {nodeId} at {slot} to Resource {resourceId} x{amount}.";
        }

        [ConsoleCommand("set_node_res_range", "Changes the resource in a specific slot of a range of nodes.")]
        public string DebugSetNodeResourceFromRange(int startNodeId, int endNodeId, SlotType slot, ushort resourceId, ushort amount)
        {
            for (int nodeId = startNodeId; nodeId <= endNodeId; nodeId++)
            {
                ServerRequestChangeResource(nodeId, slot, resourceId, amount);
            }
            return $"Requested resource change for Nodes {startNodeId} to {endNodeId} at {slot} to Resource {resourceId} x{amount}.";
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

    public struct ChangeRecipeCommand
    {
        public uint TargetTick;
        public int NodeId;
        public ushort NewRecipeId;
    }

    public struct ChangeResourceCommand
    {
        public uint TargetTick;
        public int NodeId;
        public ushort NewResourceId;
        public SlotType SlotType;
        public ushort Amount;
    }
}
