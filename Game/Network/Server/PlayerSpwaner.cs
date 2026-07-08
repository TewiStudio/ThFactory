using FishNet;
using FishNet.Component.Spawning;
using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using Tewi.Helpers;
using Tewi.Helpers.Extensions;

namespace Tewi.Game.Network.Server
{
    public class PlayerSpwaner : NetworkBehaviour
    {
        [SerializeField] private NetworkObject _playerPrefab;

        [Tooltip("players will spawn at this position.")]
        public Collider defaultSpawnPosition = null;

        public override void OnStartServer()
        {
            base.OnStartServer();
            InstanceFinder.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            InstanceFinder.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;
        }

        private void SceneManager_OnClientLoadedStartScenes(FishNet.Connection.NetworkConnection conn, bool asServer)
        {
            if (!asServer) return;
            SpawnPlayer(conn, GetSpawnPosition(), Quaternion.identity);
        }

        public Vector3 GetSpawnPosition()
        {
            Vector3 spawnPosition = Vector3.zero;
            if (defaultSpawnPosition != null)
            {
                spawnPosition = defaultSpawnPosition.GetRandomPointInCollider();
            }
            return spawnPosition;
        }

        [Server]
        public void SpawnPlayer(NetworkConnection conn, Vector3 position, Quaternion quaternion)
        {
            NetworkObject playerObj = Instantiate(_playerPrefab, position, quaternion);

            Spawn(playerObj, conn);

            SceneManager.AddOwnerToDefaultScene(playerObj);

            Debug.Log($"[Server] Character {playerObj.ObjectId} set to {conn.ClientId}。Player count: {ClientManager.Clients.Count}");
        }

        [Server]
        public void DeSpawnPlayer(NetworkConnection conn)
        {
            conn.Disconnect(false);
        }
    }
}
