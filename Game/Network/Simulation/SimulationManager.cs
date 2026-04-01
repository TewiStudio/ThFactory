using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using FishNet;
using FishNet.Object;
using Tewi.Game.Network.Core;
using Tewi.Game.Network.Server;

namespace Tewi.Game.Network.Simulation
{
    public class SimulationManager : NetworkBehaviour
    {
        public NetworkGameManager NetworkGameManagerInstance => InstanceFinder.GetInstance<NetworkGameManager>();

        public float _tickTimer;
        public float tickInterval = 0.1f; // 每 0.1 秒
        public int nextNodeId = 1;
        private JobHandle _jobHandle;

        private NativeList<NodeState> _nodes;
        private NativeArray<NodeState> _nodesSnapshot;
        private NativeHashMap<int, int> _idToIndex;

        public NativeList<NodeState> Nodes => _nodes;
        public NativeHashMap<int, int> IdToIndex => _idToIndex;

        private Queue<NodeState> _pendingAdds = new ();
        private Queue<int> _pendingRemoves = new();

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _nodes = new(1000, Allocator.Persistent);
            _idToIndex = new(1000, Allocator.Persistent);

            TimeManager.OnTick -= Tick;
            TimeManager.OnTick += Tick;
            TimeManager.OnPostTick -= TimeManager_OnPostTick;
            TimeManager.OnPostTick += TimeManager_OnPostTick;
            
            for (int i = 0; i < 10000; i++)
            {
                AddNode(1, 2);
            }

            Debug.Log("Created nodes list.");
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            if (_nodes.IsCreated) _nodes.Dispose();
            if (_idToIndex.IsCreated) _idToIndex.Dispose();
            
            TimeManager.OnTick -= Tick;
            TimeManager.OnPostTick -= TimeManager_OnPostTick;
            
            Debug.Log("Disposed nodes list.");
        }

        public void AddNode(ushort nodeType, ushort recipeId)
        {
            _pendingAdds.Enqueue(new() { id = nextNodeId++, nodeType = nodeType, recipeId = recipeId, in1 = new ResourceStack { id = 1, amount = 100 }, in2 = new ResourceStack { id = 2, amount = 100 } });
        }

        public void RemoveNode(int id)
        {
            _pendingRemoves.Enqueue(id);
        }

        private void ApplyPendingStructuralChanges()
        {
            while (_pendingRemoves.Count > 0)
            {
                int idToRemove = _pendingRemoves.Dequeue();
                
                // 通过映射表找到它当前的物理索引
                if (!_idToIndex.TryGetValue(idToRemove, out int targetIndex)) return;

                int lastIndex = _nodes.Length - 1;

                // 直接删除最后一个
                if (targetIndex == lastIndex)
                {
                    _idToIndex.Remove(idToRemove);
                    _nodes.RemoveAtSwapBack(targetIndex);
                    return;
                }

                // SwapBack
                NodeState lastNode = _nodes[lastIndex];

                // 更新节点的内部索引
                lastNode.internalIndex = targetIndex;
                _nodes[targetIndex] = lastNode;

                // 更新映射表，让换位置的机器 ID 重新指向它的新物理位置
                _idToIndex[lastNode.id] = targetIndex;

                // 清理被删除机器的信息
                _idToIndex.Remove(idToRemove);
                _nodes.RemoveAtSwapBack(lastIndex);
            }

            // 处理新增
            while (_pendingAdds.Count > 0)
            {
                NodeState newNode = _pendingAdds.Dequeue();

                int newIndex = _nodes.Length;
                newNode.internalIndex = newIndex;
                _nodes.Add(newNode);
                _idToIndex.Add(newNode.id, newIndex);
            }
        }

        private void Tick()
        {
            if (!_nodes.IsCreated) return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= tickInterval)
            {
                _tickTimer -= tickInterval;

                _jobHandle.Complete();
                ApplyPendingStructuralChanges();
                CopyToSnapshot();

                var tickJob = new SimulationTickJob
                {
                    Nodes = _nodes.AsArray(),
                    RecipeTable = NetworkGameManagerInstance.resourcesDatabase.recipeTable
                };
                _jobHandle = tickJob.Schedule(_nodes.Length, 64);

                NetworkGameManagerInstance.presentationManager.NotifyNodeSimulationCompleted(_nodesSnapshot);
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

        private void TimeManager_OnPostTick()
        {
        }
    }
}
