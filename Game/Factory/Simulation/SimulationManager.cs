using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Tewi.Game.Console;
using Tewi.Game.Factory.Core;
using Tewi.Game.Network;
using Tewi.Helpers;
using Tewi.Helpers.Extensions;

namespace Tewi.Game.Factory.Simulation
{
    public class SimulationManager : NetworkBehaviour, ICleanable
    {
        public NetworkGameManager networkGameManager;

        public int tps;

        public event Action OnSimulationCompletedInterval;
        public event Action<NativeArray<NodeState>.ReadOnly, NativeHashMap<int, int>.ReadOnly> OnSimulationStart;
        public NativeArray<NodeState>.ReadOnly NodesSnapshot => _nodesSnapshot.AsReadOnly();
        public NativeHashMap<int, int>.ReadOnly IdToIndex => _idToIndex.AsReadOnly();
        public int Priority => -100;
        public bool IsSimulationPaused => IsServerFreeze || pausedSimulation.Value || _syncing;
        public bool IsServerFreeze { get; private set; }

        public readonly SyncVar<bool> pausedSimulation = new();
        public int maxTickLead = 5;
        public int maxCatchUpPerFrame = 5;

        private int _tpsCounterTickCount;
        private uint _simulatedTickCount = 0;
        private float _windowTimer;
        private JobHandle _jobHandle;

        private bool _syncing = false;

        private NativeList<NodeState> _nodes;
        private NativeArray<NodeState> _nodesSnapshot;
        private NativeHashMap<int, int> _idToIndex;
        private int nextNodeId = 1;

        private Queue<NodeState> _pendingAdds = new();
        private Queue<int> _pendingRemoves = new();
        private Queue<RecipeChangeRequest> _pendingRecipeChanges = new();
        private Queue<ChangeNodeSlotResourceRequest> _pendingChangeNodeSlots = new();

        public int GenerateNodeId() => nextNodeId++;

        public int AddNode()
        {
            int id = GenerateNodeId();
            AddNode(id);
            return id;
        }

        public void AddNode(int nodeId)
        {
            _pendingAdds.Enqueue(new() { id = nodeId });
        }

        public void RemoveNode(int id)
        {
            _pendingRemoves.Enqueue(id);
        }

        public void ChangeRecipe(int nodeId, int newRecipeId)
        {
            _pendingRecipeChanges.Enqueue(new RecipeChangeRequest { nodeId = nodeId, newRecipeId = newRecipeId });
        }

        public void ChangeResource(int nodeId, SlotType slot, ushort resourceId, ushort amount)
        {
            _pendingChangeNodeSlots.Enqueue(new ChangeNodeSlotResourceRequest
            {
                nodeId = nodeId,
                slot = slot,
                resourceId = resourceId,
                amount = amount
            });
        }

        private void ApplyPendingStructuralChanges()
        {
            // Removes
            while (_pendingRemoves.Count > 0)
            {
                int idToRemove = _pendingRemoves.Dequeue();

                if (!_idToIndex.TryGetValue(idToRemove, out int targetIndex)) continue;

                int lastIndex = _nodes.Length - 1;

                if (targetIndex != lastIndex)
                {
                    // SwapBack
                    NodeState lastNode = _nodes[lastIndex];
                    _nodes[targetIndex] = lastNode;
                    _idToIndex[lastNode.id] = targetIndex;
                }

                // 移除最后一位
                _idToIndex.Remove(idToRemove);
                _nodes.RemoveAtSwapBack(lastIndex);
            }

            // Adds
            while (_pendingAdds.Count > 0)
            {
                NodeState newNode = _pendingAdds.Dequeue();

                int newIndex = _nodes.Length;
                _nodes.Add(newNode);
                _idToIndex.Add(newNode.id, newIndex);
            }

            // Recipe Changes
            while (_pendingRecipeChanges.Count > 0)
            {
                var request = _pendingRecipeChanges.Dequeue();

                if (_idToIndex.TryGetValue(request.nodeId, out int index))
                {
                    NodeState state = _nodes[index];
                    state.recipeId = request.newRecipeId;
                    state.progressTicks = 0;
                    ClearResourceStacks(ref state);

                    _nodes[index] = state;
                }
            }

            // State Changes
            while (_pendingChangeNodeSlots.Count > 0)
            {
                var request = _pendingChangeNodeSlots.Dequeue();

                if (_idToIndex.TryGetValue(request.nodeId, out int index))
                {
                    NodeState state = _nodes[index];
                    ResourceStack stack = new() { id = request.resourceId, amount = request.amount };

                    switch (request.slot)
                    {
                        case SlotType.In1: state.in1 = stack; break;
                        case SlotType.In2: state.in2 = stack; break;
                        case SlotType.In3: state.in3 = stack; break;
                        case SlotType.In4: state.in4 = stack; break;
                        case SlotType.Out1: state.out1 = stack; break;
                        case SlotType.Out2: state.out2 = stack; break;
                        case SlotType.Out3: state.out3 = stack; break;
                        case SlotType.Out4: state.out4 = stack; break;
                    }

                    _nodes[index] = state;
                }
            }
        }

        private void CopyToSnapshot()
        {
            // 深拷贝内存
            if (!_nodesSnapshot.IsCreated || _nodesSnapshot.Length != _nodes.Length)
            {
                if (_nodesSnapshot.IsCreated) _nodesSnapshot.Dispose();
                _nodesSnapshot = new NativeArray<NodeState>(_nodes.AsArray(), Allocator.Persistent);
            }
            else
            {
                // 快速内存拷贝
                NativeArray<NodeState>.Copy(_nodes.AsArray(), _nodesSnapshot);
            }

        }

        private void ClearResourceStacks(ref NodeState state)
        {
            state.in1 = default; state.in2 = default; state.in3 = default; state.in4 = default;
            state.out1 = default; state.out2 = default; state.out3 = default; state.out4 = default;
        }

        private void RunSimulationJob()
        {
            _simulatedTickCount++;
            _jobHandle.Complete();
            OnSimulationCompletedInterval?.Invoke();
            ApplyPendingStructuralChanges();
            CopyToSnapshot();

            _tpsCounterTickCount++;
            var tickJob = new SimulationTickJob
            {
                Nodes = _nodes.AsArray(),
                RecipeTable = networkGameManager.resourcesDatabase.recipeTable,
                ResourceTable = networkGameManager.resourcesDatabase.resourceTable
            };
            _jobHandle = tickJob.Schedule(_nodes.Length, 64);
            OnSimulationStart?.Invoke(_nodesSnapshot.AsReadOnly(), _idToIndex.AsReadOnly());
        }

        #region lifecycle
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            networkGameManager.RegisterCleanable(this);

            nextNodeId = 1;
            _nodes = new(1000, Allocator.Persistent);
            _idToIndex = new(1000, Allocator.Persistent);

            if (IsServerInitialized)
            {
                TimeManager.OnTick += TickServer;
            }

            Debug.Log("Created nodes list.");
        }

        public void CleanUp()
        {
            TimeManager.OnTick -= TickServer;
            TimeManager.OnTick -= TickClient;
            DisposeNative();
        }

        private void OnDestroy()
        {
            DisposeNative();
        }

        public void DisposeNative()
        {
            _jobHandle.Complete();
            if (_nodes.IsCreated) _nodes.Dispose();
            if (_nodesSnapshot.IsCreated) _nodesSnapshot.Dispose();
            if (_idToIndex.IsCreated) _idToIndex.Dispose();

            Debug.Log("Disposed nodes list.");

        }

        private void TickServer()
        {
            if (!_nodes.IsCreated) return;
            if (IsSimulationPaused) return;
            RunSimulationJob();
        }

        private void TickClient()
        {
            if (!_nodes.IsCreated) return;

            uint localTick = TimeManager.Tick;
            uint lastServerTick = TimeManager.LastPacketTick.RemoteTick;
            int tickGap = (int)(localTick - lastServerTick);
            IsServerFreeze = tickGap > maxTickLead;

            if (IsSimulationPaused) return;

            int catchUpCount = 0;
            while (_simulatedTickCount < TimeManager.LastPacketTick.RemoteTick && catchUpCount < maxCatchUpPerFrame)
            {
                RunSimulationJob();
                catchUpCount++;
            }
        }
        private void FixedUpdate()
        {
            _windowTimer += Time.fixedDeltaTime;
            if (_windowTimer >= 1f)
            {
                tps = _tpsCounterTickCount;
                _tpsCounterTickCount = 0;
                _windowTimer -= 1f;
            }
        }
        #endregion

        #region sync
        [Server]
        public void SendFullSync(NetworkConnection conn)
        {
            if (_nodesSnapshot.Length == 0)
            {
                TargetSetSynced(conn);
                return;
            }

            // 获取原始内存数据
            int nodeCount = _nodesSnapshot.Length;
            int stride = Marshal.SizeOf<NodeState>();
            int totalBytes = nodeCount * stride;

            // 将 NativeList 转换为 byte[]
            byte[] allData = new byte[totalBytes];
            unsafe
            {
                fixed (void* dest = allData)
                {
                    void* src = _nodesSnapshot.GetUnsafePtr();
                    UnsafeUtility.MemCpy(dest, src, totalBytes);
                }
            }

            Debug.Log($"[Server][SimulationManager] Send chunk {ExtemsionMethods.FormatBytes(totalBytes)}");
            // 开始分片发送
            int chunkSize = 1200;
            for (int i = 0; i < totalBytes; i += chunkSize)
            {
                int currentChunkSize = Mathf.Min(chunkSize, totalBytes - i);
                byte[] chunk = new byte[currentChunkSize];
                Buffer.BlockCopy(allData, i, chunk, 0, currentChunkSize);

                TargetReceiveChunk(conn, chunk, i, totalBytes, nodeCount);
            }
        }

        private byte[] _syncBuffer;
        private int _receivedBytes = 0;
        private int _expectedNodeCount = 0;

        [TargetRpc]
        public void TargetReceiveChunk(NetworkConnection conn, byte[] chunk, int offset, int totalBytes, int nodeCount)
        {
            _simulatedTickCount = TimeManager.LastPacketTick.RemoteTick;
            if (_syncBuffer == null || _syncBuffer.Length != totalBytes)
            {
                _syncBuffer = new byte[totalBytes];
                _receivedBytes = 0;
                _expectedNodeCount = nodeCount;
                _syncing = true;
            }

            Buffer.BlockCopy(chunk, 0, _syncBuffer, offset, chunk.Length);
            _receivedBytes += chunk.Length;

            // 检查是否接收完成
            if (_receivedBytes >= totalBytes)
            {
                Debug.Log($"[Client][SimulationManager] Received {ExtemsionMethods.FormatBytes(_receivedBytes)}.");
                FinalizeFullSync();
            }
        }

        /// <summary>
        /// 当 Server 中 _nodesSnapshot 为空时触发
        /// </summary>
        /// <param name="conn"></param>
        [TargetRpc]
        public void TargetSetSynced(NetworkConnection conn)
        {
            _simulatedTickCount = TimeManager.LastPacketTick.RemoteTick;
            // start client ticking
            TimeManager.OnTick += TickClient;
        }

        private unsafe void FinalizeFullSync()
        {
            try
            {
                // 清理本地现有数据
                _nodes.Clear();
                _idToIndex.Clear();

                // 将 byte[] 还原为 NodeState 并填充 NativeList
                fixed (byte* ptr = _syncBuffer)
                {
                    int stride = sizeof(NodeState);
                    for (int i = 0; i < _expectedNodeCount; i++)
                    {
                        NodeState* nodePtr = (NodeState*)(ptr + (i * stride));
                        _nodes.Add(*nodePtr);

                        // 重建 ID 映射表
                        _idToIndex.Add(nodePtr->id, i);
                    }
                }

                Debug.Log($"[Client][SimulationManager] 成功还原 {_nodes.Length} 个节点，同步完成。");
            }
            finally
            {
                _syncBuffer = null;
                _syncing = false;

                // start client ticking
                TimeManager.OnTick += TickClient;
            }
        }
        #endregion

        #region console commands
        [ConsoleCommand("get_node", "Prints detailed information about a nodestate.")]
        public string DebugGetNodeInfo(int nodeId)
        {
            if (_idToIndex.TryGetValue(nodeId, out int index))
            {
                NodeState state = _nodesSnapshot[index];
                return $"Node {nodeId}: Recipe {state.recipeId}, Progress {state.progressTicks} ticks, In1 ({state.in1.id} x{state.in1.amount}), Out1 ({state.out1.id} x{state.out1.amount})";
            }
            else
            {
                return $"Node {nodeId} not found.";
            }
        }

        [ConsoleCommand("get_node_all", "Prints detailed information about all nodestates.")]
        public string DebugGetAllNodesInfo()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < _nodesSnapshot.Length; i++)
            {
                NodeState state = _nodesSnapshot[i];
                sb.AppendLine($"Node {state.id}: Recipe {state.recipeId}, Progress {state.progressTicks} ticks, In1 ({state.in1.id} x{state.in1.amount}), Out1 ({state.out1.id} x{state.out1.amount})");
            }
            return sb.ToString();
        }

        [ConsoleCommand("get_node_count", "Prints the total number of nodestates in the simulation.")]
        public int DebugGetNodeCount()
        {
            return _nodesSnapshot.Length;
        }

        [ConsoleCommand("set_node_recipe", "Changes the recipe of a node.")]
        public string DebugSetNodeRecipe(int nodeId, int newRecipeId)
        {
            if (_idToIndex.ContainsKey(nodeId))
            {
                ChangeRecipe(nodeId, newRecipeId);
                return $"Requested recipe change for Node {nodeId} to Recipe {newRecipeId}.";
            }
            else
            {
                return $"Node {nodeId} not found.";
            }
        }

        [ConsoleCommand("set_node_recipe_range", "Changes the recipe of a range of nodes.")]
        public string DebugSetNodeRecipeFromRange(int startNodeId, int endNodeId, int newRecipeId)
        {
            for (int nodeId = startNodeId; nodeId <= endNodeId; nodeId++)
            {
                if (_idToIndex.ContainsKey(nodeId))
                {
                    ChangeRecipe(nodeId, newRecipeId);
                }
            }
            return $"Requested recipe change for Nodes {startNodeId} to {endNodeId} to Recipe {newRecipeId}.";
        }

        [ConsoleCommand("set_node_res", "Changes the resource in a specific slot of a node.")]
        public string DebugSetNodeResource(int nodeId, SlotType slot, ushort resourceId, ushort amount)
        {
            if (_idToIndex.ContainsKey(nodeId))
            {
                ChangeResource(nodeId, slot, resourceId, amount);
                return $"Requested resource change for Node {nodeId} at {slot} to Resource {resourceId} x{amount}.";
            }
            else
            {
                return $"Node {nodeId} not found.";
            }
        }

        [ConsoleCommand("set_node_res_range", "Changes the resource in a specific slot of a range of nodes.")]
        public string DebugSetNodeResourceFromRange(int startNodeId, int endNodeId, SlotType slot, ushort resourceId, ushort amount)
        {
            for (int nodeId = startNodeId; nodeId <= endNodeId; nodeId++)
            {
                if (_idToIndex.ContainsKey(nodeId))
                {
                    ChangeResource(nodeId, slot, resourceId, amount);
                }
            }
            return $"Requested resource change for Nodes {startNodeId} to {endNodeId} at {slot} to Resource {resourceId} x{amount}.";
        }
        #endregion

    }

    struct RecipeChangeRequest
    {
        public int nodeId;
        public int newRecipeId;
    }

    struct ChangeNodeSlotResourceRequest
    {
        public int nodeId;
        public SlotType slot;
        public ushort resourceId;
        public ushort amount;
    }

    public enum SlotType { In1, In2, In3, In4, Out1, Out2, Out3, Out4 }
}