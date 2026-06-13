using System.Collections.Generic;
using UnityEngine.Pool;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using FishNet.Object;
using Tewi.Game.Network;
using Tewi.Game.Console;
using Tewi.Game.Factory.Core;
using Tewi.Game.Interactable.Nodes;
using Tewi.Helpers;

namespace Tewi.Game.Factory.Presentation
{
    public class PresentationManager : NetworkBehaviour, ICleanable
    {
        public delegate void OnNodeSimulationCompleted(in NativeArray<NodeState>.ReadOnly nodes, in NativeHashMap<int, int>.ReadOnly idToIndex);
        public event OnNodeSimulationCompleted NodeSimulationCompletedEvent;

        public Dictionary<int, List<INodeStatePushed>> ActiveObservers => _activeObservers;

        public int Priority => -99;

        public NetworkGameManager gameManager;

        [SerializeField] private Node nodePrefab;
        private IObjectPool<Node> _nodePool;
        private readonly Dictionary<int, List<INodeStatePushed>> _activeObservers = new();

        public override void OnStartClient()
        {
            base.OnStartClient();
            gameManager.simulationManager.OnSimulationStart -= NotifyNodeSimulationCompleted;
            gameManager.simulationManager.OnSimulationStart += NotifyNodeSimulationCompleted;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            DestroyNative();
            Debug.Log("PresentationManager stopped on client, cleaned up observers and unsubscribed from simulation events.");
        }

        public void CleanUp()
        {
            DestroyNative();
        }
         
        private void OnDestroy()
        {
            DestroyNative();
        }

        private void DestroyNative()
        {
            gameManager.simulationManager.OnSimulationStart -= NotifyNodeSimulationCompleted;
            ReleaseAllObservers();
            _activeObservers.Clear();
        }

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

        private void ReleaseAllObservers()
        {
            foreach (var pair in _activeObservers)
            {
                var list = pair.Value;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] is Node node)
                    {
                        _nodePool.Release(node);
                    }
                }

                list.Clear();
            }

            _activeObservers.Clear();
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

        private void NotifyNodeSimulationCompleted(NativeArray<NodeState>.ReadOnly nodes, NativeHashMap<int, int>.ReadOnly idToIndex)
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

        [ConsoleCommand("get_observer_count", "Prints the total number of observers in the presentation.")]
        public int DebugGetActiveObserverCount()
        {
            return _activeObservers.Count;
        }
    }
}
