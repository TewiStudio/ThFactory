using FishNet;
using FishNet.Object;
using System.Collections.Generic;
using Tewi.Game.Console;
using Tewi.Game.Factory.Core;
using Tewi.Game.Interactable.Nodes;
using Tewi.Game.Network;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Pool;

namespace Tewi.Game.Factory.Presentation
{
    public class PresentationManager : MonoBehaviour
    {
        public delegate void OnNodeSimulationCompleted(in NativeArray<NodeState>.ReadOnly nodes, in NativeHashMap<int, int>.ReadOnly idToIndex);
        public event OnNodeSimulationCompleted NodeSimulationCompletedEvent;

        public Dictionary<int, List<INodeStatePushed>> ActiveObservers => _activeObservers;
        public NetworkGameManager gameManager;

        [SerializeField] private Node nodePrefab;
        private IObjectPool<Node> _nodePool;
        private readonly Dictionary<int, List<INodeStatePushed>> _activeObservers = new();

        private void Awake()
        {
            _nodePool = new ObjectPool<Node>(
                createFunc: () => Instantiate(nodePrefab),
                actionOnGet: (node) =>
                {
                    node.gameObject.SetActive(true);
                },
                actionOnRelease: (node) =>
                {
                    node.nodeId = 0;
                    node.Deinit();
                    node.gameObject.SetActive(false);
                },
                actionOnDestroy: (node) =>
                {
                    node.Deinit();
                    Destroy(node.gameObject);
                },
#if UNITY_EDITOR
                collectionCheck: true,
#else
                collectionCheck: false,
#endif
                defaultCapacity: 200,
                maxSize: 3000
            );
        }

        public void AddObserver(int nodeId, ushort nodeType, Vector3 position, Quaternion rotation)
        {
            Node node = _nodePool.Get();
            node.gameManager = gameManager;
            node.nodeId = nodeId;
            node.transform.position = position;
            node.transform.rotation = rotation;
            node.Init();
        }

        public void RemoveObserver(int nodeId)
        {
            if (_activeObservers.TryGetValue(nodeId, out var list))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var observer = list[i];
                    if (observer is Node node)
                    {
                        _nodePool.Release(node);
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

                    // push to observers
                    List<INodeStatePushed> uiList = kvp.Value;
                    for (int i = 0; i < uiList.Count; i++)
                    {
                        uiList[i].OnNodeStatePushed(in *statePtr);
                    }
                }
            }
        }

        private void Start()
        {

        }

        [ConsoleCommand("get_observer_count", "Prints the total number of observers in the presentation.")]
        public int DebugGetActiveObserverCount()
        {
            return _activeObservers.Count;
        }
    }
}
