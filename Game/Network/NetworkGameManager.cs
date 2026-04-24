using UnityEngine;
using PrimeTween;
using FishNet;
using FishNet.Object;
using Tewi.Game.Factory;
using Tewi.Game.Network.Server;
using Tewi.Game.Factory.Simulation;
using Tewi.Game.Factory.Presentation;

namespace Tewi.Game.Network
{
    public class NetworkGameManager : NetworkBehaviour
    {
        [Tooltip("players will spawn at this position.")]
        public Vector3 defaultSpawnPosition = Vector3.zero;

        [Header("Components")]
        public PlayerSpwaner playerSpawner;
        public ResourcesDatabase resourcesDatabase;
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
            //Vector3 position = homeManager ? homeManager.transform.position + homeManager.transform.forward : spawnPosition;
            playerSpawner.SpawnPlayer(conn, defaultSpawnPosition, Quaternion.identity);
        }
    }
}
