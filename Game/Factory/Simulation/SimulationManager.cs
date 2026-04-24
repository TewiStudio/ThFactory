using FishNet.Object;
using NaughtyAttributes;
using System.Collections.Generic;
using Tewi.Game.Factory.Core;
using Tewi.Game.Network;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor.ShaderGraph.Internal;
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

        public NativeList<NodeState> Nodes => _nodes;
        public NativeHashMap<int, int> IdToIndex => _idToIndex;

        private Queue<NodeState> _pendingAdds = new();
        private Queue<int> _pendingRemoves = new();
        private Queue<RecipeChangeRequest> _pendingRecipeChanges = new();
        private Queue<ChangeNodeSlotResourceRequest> _pendingChangeNodeSlots = new();

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            nextNodeId = 1;
            _nodes = new(1000, Allocator.Persistent);
            _idToIndex = new(1000, Allocator.Persistent);

            TimeManager.OnTick -= Tick;
            TimeManager.OnTick += Tick;
            TimeManager.OnPostTick -= TimeManager_OnPostTick;
            TimeManager.OnPostTick += TimeManager_OnPostTick;

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

                // 尝试通过最新的映射表找到索引
                if (_idToIndex.TryGetValue(request.nodeId, out int index))
                {
                    // 获取当前状态
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
                    // 1. 拷贝结构体
                    NodeState state = _nodes[index];

                    // 2. 构造资源包
                    ResourceStack stack = new() { id = request.itemId, amount = request.amount };

                    // 3. 根据类型修改对应的插槽
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

                    // 4. 写回 NativeArray
                    _nodes[index] = state;

                    Debug.Log($"[Debug] 已强行填充 Node {request.nodeId} 的 {request.slot} 插槽：Item {request.itemId} x{request.amount}");
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

        [Header("Debug Controls")]
        public int debugNodeId;
        public SlotType debugSlot;
        public ushort debugItemId;
        public ushort debugAmount;

        [Button("Execute Force Add Item")]
        public void Debug_ExecuteAdd()
        {
            if (!Application.isPlaying) return;
            Debug_SetResource(debugNodeId, debugSlot, debugItemId, debugAmount);
        }
        public enum SlotType { In1, In2, In3, In4, Out1, Out2, Out3, Out4 }

        public void Debug_SetResource(int nodeId, SlotType slot, ushort itemId, ushort amount)
        {
            _pendingChangeNodeSlots.Enqueue(new ChangeNodeSlotResourceRequest
            {
                nodeId = nodeId,
                slot = slot,
                itemId = itemId,
                amount = amount
            });
        }
    }

    struct RecipeChangeRequest
    {
        public int nodeId;
        public int newRecipeId;
    }

    struct ChangeNodeSlotResourceRequest
    {
        public int nodeId;
        public SimulationManager.SlotType slot;
        public ushort itemId;
        public ushort amount;
    }
}
