using FishNet;
using FishNet.Object;
using PrimeTween;
using System;
using System.Collections.Generic;
using System.Linq;
using Tewi.Game.Factory;
using Tewi.Game.Factory.Presentation;
using Tewi.Game.Factory.Simulation;
using Tewi.Game.Network.Server;
using Tewi.Helpers;
using UnityEngine;

namespace Tewi.Game.Network
{
    public class NetworkGameManager : NetworkBehaviour
    {
        private List<ICleanable> _objectsToClean = new();

        [Tooltip("players will spawn at this position.")]
        public Vector3 defaultSpawnPosition = Vector3.zero;

        [Header("Components")]
        public PlayerSpwaner playerSpawner;
        public ResourcesDatabase resourcesDatabase;
        public SpatialManager spatialManager;
        public PresentationManager presentationManager;
        public SimulationManager simulationManager;
        public NodeCoordinator nodeCoordinator;

        public void Awake()
        {
            PrimeTweenConfig.warnZeroDuration = false;
            InstanceFinder.TryRegisterInstance(this);
        }

        public void OnDestroy()
        {
            InstanceFinder.UnregisterInstance<NetworkGameManager>();
        }

        public override void OnStopNetwork()
        {
            var sortedList = _objectsToClean.OrderBy(x => x.Priority);

            foreach (var item in sortedList)
            {
                try
                {
                    item.CleanUp();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Cleanup failed: {item.GetType().Name} - {e.Message}");
                }
            }

            base.OnStopNetwork();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            InstanceFinder.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;
            ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
            
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            InstanceFinder.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;
            ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;
        }

        private void SceneManager_OnClientLoadedStartScenes(FishNet.Connection.NetworkConnection conn, bool asServer)
        {
            if (!asServer) return;
            playerSpawner.SpawnPlayer(conn, defaultSpawnPosition, Quaternion.identity);
        }

        private void ServerManager_OnRemoteConnectionState(FishNet.Connection.NetworkConnection arg1, FishNet.Transporting.RemoteConnectionStateArgs arg2)
        {/*
            if (arg2.ConnectionState == FishNet.Transporting.RemoteConnectionState.Started)
            {
                simulationManager.SendFullSync(arg1);
            }*/
        }
    }
}
