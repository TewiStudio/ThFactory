using FishNet.Connection;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Tewi.Game.Console;
using Tewi.Game.Factory.Core;
using Tewi.Game.Factory.Presentation;
using Tewi.Game.Network;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Tewi.Game.Factory.Simulation
{
    public class SimulationManager : NetworkBehaviour
    {
        public NetworkGameManager networkGameManager;

        public int tps;
        private int _tickCount;
        private float _windowTimer;

        private JobHandle _jobHandle;

        private NativeList<NodeState> _nodes;
        private NativeArray<NodeState> _nodesSnapshot;
        private NativeHashMap<int, int> _idToIndex;
        private int nextNodeId = 1;

        public NativeArray<NodeState>.ReadOnly NodesSnapshot => _nodesSnapshot.AsReadOnly();
        public NativeHashMap<int, int>.ReadOnly IdToIndex => _idToIndex.AsReadOnly();

        private Queue<NodeState> _pendingAdds = new();
        private Queue<int> _pendingRemoves = new();
        private Queue<RecipeChangeRequest> _pendingRecipeChanges = new();
        private Queue<ChangeNodeSlotResourceRequest> _pendingChangeNodeSlots = new();

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            if (IsServerStarted)
            {
                nextNodeId = 1;
                _nodes = new(1000, Allocator.Persistent);
                _idToIndex = new(1000, Allocator.Persistent);

                TimeManager.OnTick -= Tick;
                TimeManager.OnTick += Tick;
                TimeManager.OnPostTick -= TimeManager_OnPostTick;
                TimeManager.OnPostTick += TimeManager_OnPostTick;
            }

            Debug.Log("Created nodes list.");
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            DisposeNative();
            TimeManager.OnTick -= Tick;
            TimeManager.OnPostTick -= TimeManager_OnPostTick;

            Debug.Log("Disposed nodes list.");
        }

        private void OnDestroy()
        {
            DisposeNative();
        }

        private void DisposeNative()
        {
            _jobHandle.Complete();
            if (_nodes.IsCreated) _nodes.Dispose();
            if (_nodesSnapshot.IsCreated) _nodesSnapshot.Dispose();
            if (_idToIndex.IsCreated) _idToIndex.Dispose();

        }

        public int AddNode()
        {
            var id = nextNodeId++;
            _pendingAdds.Enqueue(new() { id = id, recipeId = 0 });
            return id;
        }

        internal void AddNode(ushort nodeType, int recipeId, ushort in1 = 0, ushort amount1 = 0, ushort in2 = 0, ushort amount2 = 0)
        {
            _pendingAdds.Enqueue(
                new()
                {
                    id = nextNodeId++,
                    recipeId = recipeId,
                    in1 = new ResourceStack { id = in1, amount = amount1 },
                    in2 = new ResourceStack { id = in2, amount = amount2 }
                });
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
                    lastNode.internalIndex = targetIndex;
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
                newNode.internalIndex = newIndex;
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

        private void Tick()
        {
            _tickCount++;
            if (!_nodes.IsCreated) return;

            _jobHandle.Complete();
            ApplyPendingStructuralChanges();
            CopyToSnapshot();

            var tickJob = new SimulationTickJob
            {
                Nodes = _nodes.AsArray(),
                RecipeTable = networkGameManager.resourcesDatabase.recipeTable,
                ResourceTable = networkGameManager.resourcesDatabase.resourceTable
            };
            _jobHandle = tickJob.Schedule(_nodes.Length, 64);

            networkGameManager.presentationManager.NotifyNodeSimulationCompleted(_nodesSnapshot.AsReadOnly(), _idToIndex.AsReadOnly());
        }

        private void TimeManager_OnPostTick()
        {
        }

        private void FixedUpdate()
        {
            _windowTimer += Time.fixedDeltaTime;
            if (_windowTimer >= 1f)
            {
                tps = _tickCount;
                _tickCount = 0;
                _windowTimer -= 1f;
            }
        }

        [Server]
        public void SendFullSync(NetworkConnection conn)
        {
            if (_nodes.Length == 0) return;

            // 获取原始内存数据
            int nodeCount = _nodes.Length;
            int stride = Marshal.SizeOf<NodeState>();
            int totalBytes = nodeCount * stride;

            // 将 NativeList 转换为 byte[]
            byte[] allData = new byte[totalBytes];
            unsafe
            {
                fixed (void* dest = allData)
                {
                    void* src = _nodes.GetUnsafePtr();
                    UnsafeUtility.MemCpy(dest, src, totalBytes);
                }
            }

            // 开始分片发送
            int chunkSize = 1200; // 避开 MTU 限制
            for (int i = 0; i < totalBytes; i += chunkSize)
            {
                int currentChunkSize = Mathf.Min(chunkSize, totalBytes - i);
                byte[] chunk = new byte[currentChunkSize];
                System.Buffer.BlockCopy(allData, i, chunk, 0, currentChunkSize);

                // 调用 RPC
                TargetReceiveChunk(conn, chunk, i, totalBytes, nodeCount);
            }
        }

        // 客户端用于暂存数据的缓冲区
        private byte[] _syncBuffer;
        private int _receivedBytes = 0;
        private int _expectedNodeCount = 0;

        [TargetRpc] // 大规模数据同步必须用可靠通道
        public void TargetReceiveChunk(NetworkConnection conn, byte[] chunk, int offset, int totalBytes, int nodeCount)
        {
            // 1. 初始化缓冲区
            if (_syncBuffer == null || _syncBuffer.Length != totalBytes)
            {
                _syncBuffer = new byte[totalBytes];
                _receivedBytes = 0;
                _expectedNodeCount = nodeCount;
                // 同步期间可以考虑暂停本地模拟 Job
                //IsSimulationPaused = true;
            }

            // 2. 拷贝分片到缓冲区
            System.Buffer.BlockCopy(chunk, offset, _syncBuffer, offset, chunk.Length);
            _receivedBytes += chunk.Length;

            // 3. 检查是否接收完成
            if (_receivedBytes >= totalBytes)
            {
                FinalizeFullSync();
            }
        }

        private unsafe void FinalizeFullSync()
        {
            try
            {
                // 1. 清理本地现有数据
                _nodes.Clear();
                _idToIndex.Clear();

                // 2. 将 byte[] 还原为 NodeState 并填充 NativeList
                fixed (byte* ptr = _syncBuffer)
                {
                    // 使用 Reinterpret 视角的技巧直接读取内存
                    int stride = sizeof(NodeState);
                    for (int i = 0; i < _expectedNodeCount; i++)
                    {
                        // 逐个从缓冲区读取结构体
                        NodeState* nodePtr = (NodeState*)(ptr + (i * stride));
                        _nodes.Add(*nodePtr);

                        // 3. 重建 ID 映射表 (这是 O(N) 操作，但在主线程完成很快)
                        _idToIndex.Add(nodePtr->id, i);
                    }
                }

                Debug.Log($"[Sync] 成功还原 {_nodes.Length} 个节点，同步完成。");
            }
            finally
            {
                // 4. 释放临时缓冲区，恢复模拟
                _syncBuffer = null;
                //IsSimulationPaused = false;

                // 触发一次表示层的全量刷新
                //presentationManager.RebuildAllObservers();
            }
        }

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
    }

    public enum SlotType { In1, In2, In3, In4, Out1, Out2, Out3, Out4 }
}