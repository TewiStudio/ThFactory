using FishNet;
using FishNet.Object;
using PrimeTween;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tewi.Console;
using Tewi.Factory;
using Tewi.Factory.Presentation;
using Tewi.Factory.Simulation;
using Tewi.Game.Network.Server;
using Tewi.Game.Player;
using Tewi.Game.UI;
using Tewi.Helpers;
using UnityEngine;

namespace Tewi.Game.Network
{
    public class NetworkGameManager : NetworkBehaviour
    {
        private List<ICleanable> _objectsToClean = new();

        [Header("Components")]
        [HideInInspector] public PlayerManager localPlayer;
        public UIManager uiManager;
        public UIToolkitManager uiToolkitManager;
        [SerializeField] private PlayerSpwaner playerSpawner;
        [SerializeField] private AudioListener audioListener;
        [SerializeField] private CommandProcessor commandProcessor;
        [SerializeField] private FactoryManager factoryManager;

        public PlayerSpwaner PlayerSpawner => playerSpawner;
        public AudioListener AudioListener => audioListener;
        public CommandProcessor CommandProcessor => commandProcessor;
        public FactoryManager FactoryManager => factoryManager;
        public NodeCoordinator NodeCoordinator => factoryManager.nodeCoordinator;
        public SimulationManager SimulationManager => factoryManager.simulationManager;
        public PresentationManager PresentationManager => factoryManager.presentationManager;
        public SpatialManager SpatialManager => factoryManager.spatialManager;

        public void RegisterCleanable(ICleanable cleanable)
        {
            if (!_objectsToClean.Contains(cleanable))
                _objectsToClean.Add(cleanable);
        }

        public void Awake()
        {
            PrimeTweenConfig.warnZeroDuration = false;
            PrimeTweenConfig.warnEndValueEqualsCurrent = false;
            //Application.targetFrameRate = int.MaxValue;
            //QualitySettings.vSyncCount = 1;
            InstanceFinder.TryRegisterInstance(this);

            transform.GetComponents<ICleanable>().ToList().ForEach(RegisterCleanable);
            commandProcessor = new();
        }

        public void OnDestroy()
        {
            InstanceFinder.UnregisterInstance<NetworkGameManager>();
            commandProcessor = null;
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
            ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
            
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;
        }

        private void ServerManager_OnRemoteConnectionState(FishNet.Connection.NetworkConnection arg1, FishNet.Transporting.RemoteConnectionStateArgs arg2)
        {/*
            if (arg2.ConnectionState == FishNet.Transporting.RemoteConnectionState.Started)
            {
                simulationManager.SendFullSync(arg1);
            }*/
            CommandProcessor.ScanCommands();
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
