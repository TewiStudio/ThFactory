using FishNet.Object;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Tewi.Game.Factory.Core;
using Tewi.Game.Interactable.Nodes;

namespace Tewi.Game.Factory.Presentation
{
    public class PresentationManager : NetworkBehaviour
    {
        public delegate void OnNodeSimulationCompleted(in NativeArray<NodeState>.ReadOnly nodes, in NativeHashMap<int, int>.ReadOnly idToIndex);
        public event OnNodeSimulationCompleted NodeSimulationCompletedEvent;

        [SerializeField] private NetworkObject _nodePrefab;
        private readonly Dictionary<int, List<INodeStatePushed>> _activeObservers = new();
        
        public void NotifyNodeSimulationCompleted(NativeArray<NodeState>.ReadOnly nodes, NativeHashMap<int, int>.ReadOnly idToIndex)
        {
            NodeSimulationCompletedEvent?.Invoke(in nodes, in idToIndex);
            UpdateObservers(in nodes, in idToIndex);
            
        }

        private unsafe void UpdateObservers(in NativeArray<NodeState>.ReadOnly nodes, in NativeHashMap<int, int>.ReadOnly idToIndex)
        {
            NodeState* basePtr = (NodeState*)nodes.GetUnsafeReadOnlyPtr();

            foreach (var kvp in _activeObservers)
            {
                int nodeId = kvp.Key;

                if (idToIndex.TryGetValue(nodeId, out int index))
                {
                    NodeState* statePtr = basePtr + index;

                    // 将数据直接推送到 Observers
                    List<INodeStatePushed> uiList = kvp.Value;
                    for (int i = 0; i < uiList.Count; i++)
                    {
                        uiList[i].OnNodeStatePushed(in *statePtr);
                    }
                }
            }
        }

        public void AddObserver(int nodeId, Vector3 position, Quaternion rotation)
        {
            var obj = Instantiate(_nodePrefab, position, rotation);
            Spawn(obj);
            if (obj.GetComponent<Node>() is Node node)
            {
                node.nodeId = nodeId;
            }
        }

        public void RemoveObserver(int nodeId)
        {
            if (_activeObservers.TryGetValue(nodeId, out var list))
            {
                foreach (var observer in list)
                {
                    if (observer is Node node)
                    {
                        Despawn(node);
                    }
                }
                _activeObservers.Remove(nodeId);
            }
        }

        public void Subscribe(INodeStatePushed pushed)
        {
            if (!_activeObservers.ContainsKey(pushed.nodeId))
                _activeObservers[pushed.nodeId] = new List<INodeStatePushed>();

            _activeObservers[pushed.nodeId].Add(pushed);
        }

        public void Unsubscribe(INodeStatePushed pushed)
        {
            if (_activeObservers.TryGetValue(pushed.nodeId, out var list))
            {
                list.Remove(pushed);
                if (list.Count == 0) _activeObservers.Remove(pushed.nodeId);
            }
        }
    }
}
