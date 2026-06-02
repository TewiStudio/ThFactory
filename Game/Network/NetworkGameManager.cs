using FishNet;
using FishNet.Object;
using PrimeTween;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tewi.Game.Console;
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

        public void RegisterCleanable(ICleanable cleanable)
        {
            if (!_objectsToClean.Contains(cleanable))
                _objectsToClean.Add(cleanable);
        }

        public void Awake()
        {
            PrimeTweenConfig.warnZeroDuration = false;
            InstanceFinder.TryRegisterInstance(this);

            transform.GetComponents<ICleanable>().ToList().ForEach(RegisterCleanable);
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
                    Debug.Log($"Cleaning up: {item.GetType().Name}");
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

        private void OnApplicationQuit()
        {
            ServerManager.StopConnection(true);
            ClientManager.StopConnection();
        }

        [ConsoleCommand("cleanables", "Prints all registered cleanable objects and their priorities.")]
        private string DebugGetAllCleanable()
        {
            if (_objectsToClean.Count == 0)
                return "No cleanable objects registered.";

            StringBuilder sb = new();

            sb.AppendLine($"Total Cleanables: {_objectsToClean.Count}");
            sb.AppendLine("--------------------------------");

            foreach (ICleanable cleanable in _objectsToClean.OrderBy(x => x.Priority))
            {
                sb.AppendLine(
                    $"Priority: {cleanable.Priority,-12} Type: {cleanable.GetType().FullName}");
            }

            return sb.ToString();
        }
    }
}
