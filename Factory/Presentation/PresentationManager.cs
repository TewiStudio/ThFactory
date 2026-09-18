using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Tewi.Console;
using Tewi.Factory.Core;
using Tewi.Factory.Simulation;

namespace Tewi.Factory.Presentation
{
    public class PresentationManager : IDisposable
    {
        public Action OnPresentationDispose;
        public delegate void OnNodeSimulationCompleted(in NativeArray<NodeState>.ReadOnly nodes, in NativeHashMap<int, int>.ReadOnly idToIndex);
        public event OnNodeSimulationCompleted NodeSimulationCompletedEvent;
        public Dictionary<int, List<INodeStatePushed>> ActiveObservers => _activeObservers;

        public SimulationManager simulationManager { get; private set; }
        private readonly Dictionary<int, List<INodeStatePushed>> _activeObservers = new();

        public PresentationManager(SimulationManager simulationManager)
        {
            this.simulationManager = simulationManager;
            simulationManager.OnSimulationStart += NotifyNodeSimulationCompleted;
        }

        private void DestroyNative()
        {
            simulationManager.OnSimulationStart -= NotifyNodeSimulationCompleted;
            OnPresentationDispose?.Invoke();
            UnsubscribeAll();
            _activeObservers.Clear();
            Debug.Log("PresentationManager stopped, cleaned up observers and unsubscribed from simulation events.");
        }

        public void Subscribe(INodeStatePushed pushed)
        {
            if (!_activeObservers.ContainsKey(pushed.NodeId))
                _activeObservers[pushed.NodeId] = new List<INodeStatePushed>();

            _activeObservers[pushed.NodeId].Add(pushed);
            pushed.OnSubscribe();
        }

        public void Unsubscribe(INodeStatePushed pushed)
        {
            if (_activeObservers.TryGetValue(pushed.NodeId, out var list))
            {
                list.Remove(pushed);
                if (list.Count == 0) _activeObservers.Remove(pushed.NodeId);
                pushed.OnUnsubscribe();
            }
        }

        public void UnsubscribeAll()
        {
            foreach (var pair in _activeObservers)
            {
                var list = pair.Value;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    list[i].OnUnsubscribe();
                }

                list.Clear();
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

        public void Dispose()
        {
            DestroyNative();
        }

        [ConsoleCommand("get_observer_count", "Prints the total number of observers in the presentation.")]
        public int DebugGetActiveObserverCount()
        {
            return _activeObservers.Count;
        }
    }
}
