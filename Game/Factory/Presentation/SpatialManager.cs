using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;
using Tewi.Game.Network;
using Tewi.Helpers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Tewi.Game.Factory.Presentation
{
    public class SpatialManager : NetworkBehaviour, ICleanable
    {
        public NetworkGameManager gameManager;
        public float cellSize = 20f;

        private NativePagedTable<NodeSpatialData> _spatialTable;
        private NativeParallelMultiHashMap<int2, int> _spatialGrid;

        public NativePagedTable<NodeSpatialData> SpatialTable => _spatialTable;
        public NativeParallelMultiHashMap<int2, int> SpatialGrid => _spatialGrid;
        public int Priority => -99;

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

        #region lifecycle
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _spatialTable = new NativePagedTable<NodeSpatialData>(Allocator.Persistent);
            _spatialGrid = new NativeParallelMultiHashMap<int2, int>(10000, Allocator.Persistent);
        }

        public void CleanUp()
        {
            _spatialTable.Dispose();
            if (_spatialGrid.IsCreated) _spatialGrid.Dispose();
        }
        #endregion

        #region sync
        [Server]
        public unsafe void SendSpatialInitialSync(NetworkConnection conn)
        {
            for (int i = 0; i < _spatialTable.maxPages; i++)
            {
                if (_spatialTable.IsPageCreated(i))
                {
                    NativeArray<NodeSpatialData> page = _spatialTable.GetPage(i);
                    int byteLength = page.Length * sizeof(NodeSpatialData);
                    byte[] bytes = new byte[byteLength];

                    fixed (void* dest = bytes)
                    {
                        UnsafeUtility.MemCpy(dest, page.GetUnsafeReadOnlyPtr(), byteLength);
                    }

                    TargetReceiveSpatialPage(conn, i, bytes);
                }
            }

            // 发送一个结束标记，告诉客户端可以开始重建 Grid 了
            TargetCompleteSpatialSync(conn);
        }

        [TargetRpc]
        private void TargetReceiveSpatialPage(NetworkConnection conn, int pageIdx, byte[] data)
        {
            _spatialTable.RestorePageFromBytes(pageIdx, data);
            Debug.Log($"[SpatialManager] 接收到第 {pageIdx} 页数据");
        }

        [TargetRpc]
        private void TargetCompleteSpatialSync(NetworkConnection conn)
        {
            RebuildSpatialGrid();
        }

        private void RebuildSpatialGrid()
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
            // 重建完成后，触发一次视距裁剪检查
            //presentationManager.ForceUpdateCulling();
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
