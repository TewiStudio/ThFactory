using UnityEngine;
using FishNet.Object;
using Tewi.Game.Console;
using Tewi.Game.Factory.Presentation;
using Tewi.Game.Factory.Simulation;

namespace Tewi.Game.Factory
{
    public class NodeCoordinator : NetworkBehaviour
    {
        [SerializeField] internal SimulationManager simulationManager;
        [SerializeField] internal SpatialManager spatialManager;
        [SerializeField] internal PresentationManager presentationManager;

        [Server]
        public void CreateNode(ushort nodeType, Vector3 position, Quaternion rotation)
        {
            int newNodeID = simulationManager.AddNode();
            spatialManager.AddNode(newNodeID, nodeType, position, rotation);
        }

        [Server]
        public void DestroyNode(int nodeID)
        {
            simulationManager.RemoveNode(nodeID);
            spatialManager.RemoveNode(nodeID);
        }

        [Server]
        public void RemoveAll()
        {
            foreach (var item in simulationManager.NodesSnapshot)
            {
                DestroyNode(item.id);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ServerRequestCreateNode(ushort nodeType, Vector3 position, Quaternion rotation)
        {
            CreateNode(nodeType, position, rotation);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ServerRequestRemoveNode(int nodeID)
        {
            DestroyNode(nodeID);
        }

        [ConsoleCommand("remove_node_all", "移除所有节点")]
        public string DebugRemoveAllNodes()
        {
            if (!IsServerStarted)
                return "仅服务器可以执行此命令。";
            RemoveAll();
            return "已移除所有节点。";
        }

        [ConsoleCommand("remove_node", "移除指定节点")]
        public string DebugRemoveNode(int nodeID)
        {
            if (!IsServerStarted)
                return "仅服务器可以执行此命令。";
            DestroyNode(nodeID);
            return $"已移除节点 {nodeID}。";
        }

        [ConsoleCommand("spawn_node", "生成节点")]
        public string DebugCreateNode(ushort nodeType, Vector3 position, Quaternion rotation)
        {
            if (!IsServerStarted)
                return "仅服务器可以执行此命令。";
            CreateNode(nodeType, position, rotation);
            return $"已创建节点类型 {nodeType} 于位置 {position}。";
        }

        [ConsoleCommand("spawn_node_rect", "生成矩形节点阵列")]
        public string DebugCreateRectangleArray(int rows, int cols, float spacing, ushort recipeId)
        {
            float offsetX = (rows - 1) * spacing / 2f;
            float offsetZ = (cols - 1) * spacing / 2f;

            Vector3 startPos = transform.position;

            int count = 0;
            for (int x = 0; x < rows; x++)
            {
                for (int z = 0; z < cols; z++)
                {
                    Vector3 spawnPos = startPos + new Vector3(
                        x * spacing - offsetX,
                        0,
                        z * spacing - offsetZ
                    );

                    CreateNode(recipeId, spawnPos, Quaternion.identity);
                    count++;
                }
            }

            return $"成功生成矩形阵列：{rows}x{cols}，共 {count} 个节点。";
        }

    }
}
