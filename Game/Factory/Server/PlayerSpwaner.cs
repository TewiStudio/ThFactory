using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

namespace Tewi.Game.Factory.Server
{
    public class PlayerSpwaner : NetworkBehaviour
    {
        [SerializeField] private NetworkObject _playerPrefab;

        private readonly Dictionary<NetworkConnection, NetworkObject> _spawnedPlayers = new();

        [Server]
        public void SpawnPlayer(NetworkConnection conn, Vector3 position, Quaternion quaternion)
        {
            NetworkObject playerObj = Instantiate(_playerPrefab, position, quaternion);

            Spawn(playerObj, conn);

            SceneManager.AddOwnerToDefaultScene(playerObj);

            _spawnedPlayers[conn] = playerObj;

            Debug.Log($"[Server] Character {playerObj.ObjectId} set to {conn.ClientId}。");
        }

        [Server]
        public void DeSpawnPlayer(NetworkConnection conn)
        {
            if (_spawnedPlayers.TryGetValue(conn, out NetworkObject playerObj))
            {
                // 移除记录
                _spawnedPlayers.Remove(conn);

                if (playerObj != null && playerObj.IsSpawned)
                {
                    Despawn(playerObj);
                }

                Debug.Log($"[Server] Character {playerObj.ObjectId} owenr {conn.ClientId} is destroyed。");
            }
        }
    }
}
