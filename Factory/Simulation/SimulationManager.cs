using System;
using System.Collections.Generic;
using Tewi.Console;
using Tewi.Factory.Core;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Tewi.Factory.Simulation
{
    public class SimulationManager : IDisposable
    {
        public event Action OnSimulationCompletedInterval;
        public event Action<NativeArray<NodeState>.ReadOnly, NativeHashMap<int, int>.ReadOnly> OnSimulationStart;

        public NativeArray<NodeState>.ReadOnly NodesSnapshot => _nodesSnapshot.AsReadOnly();
        public NativeHashMap<int, int>.ReadOnly IdToIndex => _idToIndex.AsReadOnly();
        public bool IsSimulationPaused => PausedSimulation;
        public ulong SimulationHash { get; private set; }
        public bool PausedSimulation { get; set; } = false;
        public uint TargetTick { get; set; }
        public uint CurrentTick { get; set; }

        public ResourcesDatabase resourcesDatabase;

        public int maxCatchUpPerTick = 3;

        private JobHandle _jobHandle;
        private NativeList<NodeState> _nodes;
        private NativeArray<NodeState> _nodesSnapshot;
        private NativeHashMap<int, int> _idToIndex;
        private int nextNodeId = 1;

        private Queue<NodeState> _pendingAdds = new();
        private Queue<int> _pendingRemoves = new();
        private Queue<RecipeChangeRequest> _pendingRecipeChanges = new();
        private Queue<ChangeNodeSlotResourceRequest> _pendingChangeNodeSlots = new();

        public SimulationManager(ResourcesDatabase resourcesDatabase)
        {
            this.resourcesDatabase = resourcesDatabase;
            nextNodeId = 1;
            _nodes = new(1000, Allocator.Persistent);
            _idToIndex = new(1000, Allocator.Persistent);
            Debug.Log("Created nodes list.");
        }

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

        public unsafe void* NodesSnapshotGetUnsafePtr()
        {
            return _nodesSnapshot.GetUnsafePtr();
        }

        private void ClearResourceStacks(ref NodeState state)
        {
            state.in1 = default; state.in2 = default; state.in3 = default; state.in4 = default;
            state.out1 = default; state.out2 = default; state.out3 = default; state.out4 = default;
        }

        private void RunSimulationJob()
        {
            _jobHandle.Complete();

            ApplyPendingStructuralChanges();

            CurrentTick++;
            OnSimulationCompletedInterval?.Invoke();
            CopyToSnapshot();

            if (CurrentTick % 30 == 0)
            {
                var hash = CalculateHash();
                SimulationHash = hash;
                //Debug.Log($"[SimulationManager] Tick {CurrentTick}: Simulation hash: {hash:X16}");
            }

            CreateJob();
            OnSimulationStart?.Invoke(_nodesSnapshot.AsReadOnly(), _idToIndex.AsReadOnly());
        }

        private void CreateJob()
        {
            var tickJob = new SimulationTickJob
            {
                Nodes = _nodes.AsArray(),
                RecipeTable = resourcesDatabase.recipeTable,
                ResourceTable = resourcesDatabase.resourceTable
            };
            _jobHandle = tickJob.Schedule(_nodes.Length, 64);
        }

        #region lifecycle
        public int Tick()
        {
            if (!_nodes.IsCreated) return 0;
            if (IsSimulationPaused) return 0;
            int catchUpCount = 0;
            while (CurrentTick < TargetTick && catchUpCount < maxCatchUpPerTick)
            {
                RunSimulationJob();
                catchUpCount++;
/*
                if (NodesSnapshot.Length > 0)
                {
                    Debug.Log($"node 1 progress local: {NodesSnapshot[0].progressTicks}");
                }*/
            }
            return catchUpCount;
        }

        public unsafe void FinalizeFullSync(byte[] syncBuffer, int nodeCount, uint syncStartTick, ulong hash)
        {
            if (syncBuffer != null)
            {
                // 清理本地现有数据
                _nodes.Clear();
                _idToIndex.Clear();

                // 将 byte[] 还原为 NodeState 并填充 NativeList
                fixed (byte* ptr = syncBuffer)
                {
                    int stride = sizeof(NodeState);
                    for (int i = 0; i < nodeCount; i++)
                    {
                        NodeState* nodePtr = (NodeState*)(ptr + (i * stride));
                        _nodes.Add(*nodePtr);

                        // 重建 ID 映射表
                        _idToIndex.Add(nodePtr->id, i);
                    }
                }
            }

            CurrentTick = syncStartTick;
            CopyToSnapshot();
            var localHash = CalculateHash();
            if (localHash != hash)
                Debug.LogError($"Simulation hash mismatch after full sync! Local: {localHash:X16}, Server: {hash:X16}");
            else
                Debug.Log($"成功还原 {_nodes.Length} 个节点，同步 tick {syncStartTick} 完成。Local: {localHash:X16}, Server: {hash:X16}");
            
            CreateJob(); // 同步后，立刻创建新的 SimulationTickJob，因为此时的 Job 实际为空，而服务器的 Job 为当前同步的数据。
        }

        public void DisposeNative()
        {
            CurrentTick = 0;
            _jobHandle.Complete();
            if (_nodes.IsCreated) _nodes.Dispose();
            if (_nodesSnapshot.IsCreated) _nodesSnapshot.Dispose();
            if (_idToIndex.IsCreated) _idToIndex.Dispose();

        }

        public void Dispose()
        {
            DisposeNative();
        }
/*
        private void TickServer()
        {
            if (!_nodes.IsCreated) return;
            if (IsSimulationPaused) return;
            RunSimulationJob();
            if (TimeManager.Tick % 2 == 0)
            {
                ObserversApplySimulatedTick(_simulatedTickCount);
            }
        }

        private void TickClient()
        {
            if (!_nodes.IsCreated) return;
            if (IsSimulationPaused) return;

            int catchUpCount = 0;
            while (_simulatedTickCount < _latestServerSimulationTick && catchUpCount < maxCatchUpPerFrame)
            {
                RunSimulationJob();
                catchUpCount++;
            }
        }

        private void SimulationHash_OnChange(ulong prev, ulong next, bool asServer)
        {
            localSimulationHash = CalculateHash();
            if (next != localSimulationHash)
            {
                Debug.LogWarning($"[SimulationManager] Simulation hash mismatch! Local: {localSimulationHash:X16}, Server: {next:X16}");
            }
        }
*/
#endregion

        #region test
        public unsafe ulong CalculateHash()
        {
            const ulong offsetBasis = 14695981039346656037UL;
            ulong hash = offsetBasis;
            NodeState* ptr = (NodeState*)_nodesSnapshot.GetUnsafeReadOnlyPtr();

            for (int i = 0; i < _nodesSnapshot.Length; i++)
            {
                ref readonly NodeState node = ref ptr[i];

                Hash(ref hash, node.id);
                Hash(ref hash, node.recipeId);
                Hash(ref hash, node.progressTicks);

                Hash(ref hash, node.in1.id);
                Hash(ref hash, node.in1.amount);

                Hash(ref hash, node.in2.id);
                Hash(ref hash, node.in2.amount);

                Hash(ref hash, node.in3.id);
                Hash(ref hash, node.in3.amount);

                Hash(ref hash, node.in4.id);
                Hash(ref hash, node.in4.amount);

                Hash(ref hash, node.out1.id);
                Hash(ref hash, node.out1.amount);

                Hash(ref hash, node.out2.id);
                Hash(ref hash, node.out2.amount);

                Hash(ref hash, node.out3.id);
                Hash(ref hash, node.out3.amount);

                Hash(ref hash, node.out4.id);
                Hash(ref hash, node.out4.amount);
            }

            return hash;
        }

        private static void Hash(ref ulong hash, int value)
        {
            hash ^= (uint)value;
            hash *= 1099511628211UL;
        }

        private static void Hash(ref ulong hash, ushort value)
        {
            hash ^= value;
            hash *= 1099511628211UL;
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