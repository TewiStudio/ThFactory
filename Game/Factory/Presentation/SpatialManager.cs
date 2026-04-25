using FishNet.Object;
using System.Collections.Generic;
using Tewi.Game.Network;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Tewi.Game.Factory.Presentation
{
    public class SpatialManager : NetworkBehaviour
    {
        public NetworkGameManager gameManager;
        public float cellSize = 20f;

        private NativePagedTable<NodeSpatialData> _spatialTable;
        // Key 是格子坐标，Value 是机器 ID
        private NativeParallelMultiHashMap<int2, int> _spatialGrid;

        public NativePagedTable<NodeSpatialData> SpatialTable => _spatialTable;
        public NativeParallelMultiHashMap<int2, int> SpatialGrid => _spatialGrid;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _spatialTable = new NativePagedTable<NodeSpatialData>(Allocator.Persistent);
            _spatialGrid = new NativeParallelMultiHashMap<int2, int>(10000, Allocator.Persistent);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _spatialTable.Dispose();
            if (_spatialGrid.IsCreated) _spatialGrid.Dispose();
        }

        public void AddNode(int nodeId, ushort nodeType, Vector3 position, Quaternion rotation)
        {
            NodeSpatialData data = new()
            {
                position = position,
                rotation = rotation,
                nodeType = nodeType,
                isActive = true
            };
            _spatialTable.Set(nodeId, data);

            // 存入空间格阵
            int2 gridPos = PosToGrid(position);
            _spatialGrid.Add(gridPos, nodeId);
        }

        public void RemoveNode(int nodeId)
        {
            NodeSpatialData data = _spatialTable.Get(nodeId);
            if (!data.isActive) return;

            // 从格阵移除
            int2 gridKey = PosToGrid(data.position);

            _spatialGrid.Remove(gridKey, nodeId);

            data.isActive = false;
            _spatialTable.Set(nodeId, data);

            gameManager.presentationManager.RemoveObserver(nodeId);
        }

        public int2 PosToGrid(Vector3 worldPos)
        {
            return new int2(
                (int)math.floor(worldPos.x / cellSize),
                (int)math.floor(worldPos.z / cellSize)
            );
        }
    }

    public struct NodeSpatialData
    {
        public Vector3 position;
        public Quaternion rotation;
        public ushort nodeType;
        public bool isActive;
    }
}
