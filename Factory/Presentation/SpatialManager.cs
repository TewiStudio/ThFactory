using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Tewi.Factory.Presentation
{
    public class SpatialManager : IDisposable
    {
        private float _cellSize;
        private NativePagedTable<NodeSpatialData> _spatialTable;
        private NativeParallelMultiHashMap<int2, int> _spatialGrid;

        public float CellSize => _cellSize;
        public NativePagedTable<NodeSpatialData> SpatialTable => _spatialTable;
        public NativeParallelMultiHashMap<int2, int> SpatialGrid => _spatialGrid;

        public SpatialManager(float cellSize = 20f)
        {
            _cellSize = cellSize;

            _spatialTable = new NativePagedTable<NodeSpatialData>(Allocator.Persistent);
            _spatialGrid = new NativeParallelMultiHashMap<int2, int>(10000, Allocator.Persistent);
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
        }

        public int2 PosToGrid(Vector3 worldPos)
        {
            return new int2(
                (int)math.floor(worldPos.x / _cellSize),
                (int)math.floor(worldPos.z / _cellSize)
            );
        }

        public void RebuildSpatialGrid()
        {
            _spatialGrid.Clear();

            for (int i = 0; i < _spatialTable.maxPages; i++)
            {
                if (_spatialTable.IsPageCreated(i))
                {
                    var page = _spatialTable.GetPage(i);
                    for (int j = 0; j < page.Length; j++)
                    {
                        var data = page[j];
                        if (data.isActive)
                        {
                            int nodeId = (i << _spatialTable.pageShift) | j;
                            int2 gridPos = PosToGrid(data.position);
                            _spatialGrid.Add(gridPos, nodeId);
                        }
                    }
                }
            }

            Debug.Log("[Spatial] 空间格阵重建完成。");
        }

        #region lifecycle
        public void CleanUp()
        {
            _spatialTable.Dispose();
            if (_spatialGrid.IsCreated) _spatialGrid.Dispose();
        }

        public void Dispose()
        {
            CleanUp();
        }
        #endregion
    }

    public struct NodeSpatialData
    {
        public Vector3 position;
        public Quaternion rotation;
        public ushort nodeType;
        public bool isActive;
    }

    public struct SpatialPagePacket
    {
        public int PageIndex;
        public byte[] Data;
        public int NodeCount;
    }
}
