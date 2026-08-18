using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using Tewi.Factory.Core;
using Tewi.Factory.Registry;
using Tewi.Factory.Simulation;
using Tewi.Factory.Presentation;
using FishNet.Transporting;

namespace Tewi.Game.Network
{
    public class FactoryManager : NetworkBehaviour
    {
        public NetworkGameManager networkGameManager;
        public NodeCoordinator nodeCoordinator;
        public ResourceDatabase resourceDB;
        public RecipeDatabase recipeDB;

        public ResourcesDatabase resourcesDatabase;
        public SimulationManager simulationManager;

        public PresentationManager presentationManager;
        public SpatialManager spatialManager;

        public int tps { get; private set; }
        public uint NetworkSimulationTick => _networkSimulationTick;

        private bool _simulationSynced = false;
        private int _tpsCounterTickCount;
        private float _windowTimer;

        private uint _networkSimulationTick = 0;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            resourcesDatabase = new ResourcesDatabase(TimeManager.TickRate, resourceDB, recipeDB);
            simulationManager = new SimulationManager(resourcesDatabase);
            spatialManager = new SpatialManager();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            TimeManager.OnTick += OnServerTick;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            TimeManager.OnTick -= OnServerTick;
            simulationManager.Dispose();
            spatialManager.Dispose();
            resourcesDatabase.Dispose();
            _networkSimulationTick = 0;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            presentationManager = new PresentationManager(simulationManager);
            if (!IsServerStarted)
            {
                TimeManager.OnTick += OnClientOnlyTick;
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            TimeManager.OnTick -= OnClientOnlyTick;
            presentationManager.Dispose();
            if (!IsServerStarted)
            {
                simulationManager.Dispose();
                spatialManager.Dispose();
                resourcesDatabase.Dispose();
            }
            _simulationSynced = false;
        }

        private void OnServerTick()
        {
            _networkSimulationTick++;
            //if (_networkSimulationTick % 5 == 0)
            ObserversApplySimulatedTick(_networkSimulationTick);

            simulationManager.TargetTick = _networkSimulationTick;
            _tpsCounterTickCount += simulationManager.Tick();
        }

        private void OnClientOnlyTick()
        {
            if (!_simulationSynced) return;
            simulationManager.TargetTick = _networkSimulationTick;
            _tpsCounterTickCount += simulationManager.Tick();
        }

        public void FixedUpdate()
        {
            _windowTimer += Time.fixedDeltaTime;
            if (_windowTimer >= 1f)
            {
                tps = _tpsCounterTickCount;
                _tpsCounterTickCount = 0;
                _windowTimer -= 1f;
            }
        }

        [ObserversRpc]
        private void ObserversApplySimulatedTick(uint simulatedTick, Channel channel = Channel.Reliable)
        {
            if (IsServerStarted || !_simulationSynced) return;
            _networkSimulationTick = simulatedTick;
        }

        #region Simulation sync
        [Server]
        public unsafe void SyncSimulation(NetworkConnection conn)
        {
            var syncStartTick = simulationManager.CurrentTick;
            var hash = simulationManager.CalculateHash();
            NativeArray<NodeState>.ReadOnly nodesSnapshot = simulationManager.NodesSnapshot;

            if (nodesSnapshot.Length == 0)
            {
                TargetSyncedWithNoNodes(conn, syncStartTick, hash);
                return;
            }

            int nodeCount = nodesSnapshot.Length;
            int stride = sizeof(NodeState);
            int totalBytes = nodeCount * stride;

            byte* srcPtr = (byte*)simulationManager.NodesSnapshotGetUnsafePtr();

            uint syncId = (uint)UnityEngine.Random.Range(0, int.MaxValue);
            int chunkSize = 1100; // 略小于 MTU

            for (int i = 0; i < totalBytes; i += chunkSize)
            {
                int currentChunkSize = Mathf.Min(chunkSize, totalBytes - i);
                byte[] chunk = new byte[currentChunkSize]; // 生产环境下建议从 ArrayPool 借用

                fixed (byte* destPtr = chunk)
                {
                    UnsafeUtility.MemCpy(destPtr, srcPtr + i, currentChunkSize);
                }

                // 使用 Reliable 传输
                TargetReceiveSimulationChunk(conn, syncId, chunk, i, totalBytes, nodeCount, syncStartTick, hash);
            }
        }

        private byte[] _syncBuffer;
        private int _receivedBytes = 0;
        private uint _currentSyncId = 0;
        private int _totalExpectedBytes = 0;

        [TargetRpc]
        public void TargetReceiveSimulationChunk(NetworkConnection conn, uint syncId, byte[] chunk, int offset, int totalBytes, int nodeCount, uint syncStartTick, ulong hash, Channel channel = Channel.Reliable)
        {
            // 如果是新的同步任务，初始化
            if (_currentSyncId != syncId)
            {
                _currentSyncId = syncId;
                _syncBuffer = new byte[totalBytes];
                _receivedBytes = 0;
                _totalExpectedBytes = totalBytes;
            }

            if (_syncBuffer == null || _syncBuffer.Length != totalBytes) return;

            Buffer.BlockCopy(chunk, 0, _syncBuffer, offset, chunk.Length);
            _receivedBytes += chunk.Length;

            if (_receivedBytes >= _totalExpectedBytes)
            {
                // 校验收到的字节数是否满足 nodeCount * stride
                int expectedSize = nodeCount * Marshal.SizeOf<NodeState>();
                if (_receivedBytes == expectedSize)
                {
                    simulationManager.FinalizeFullSync(_syncBuffer, nodeCount, syncStartTick, hash);
                    _networkSimulationTick = syncStartTick;
                }
                else
                {
                    Debug.LogError("Sync size mismatch!");
                }
                // 清理以便下次任务
                _currentSyncId = 0;
                _simulationSynced = true;
            }
        }

        [TargetRpc]
        public void TargetSyncedWithNoNodes(NetworkConnection conn, uint syncStartTick, ulong hash)
        {
            simulationManager.FinalizeFullSync(null, 0, syncStartTick, hash);
            _simulationSynced = true;
        }
        #endregion

        #region Spatial sync
        [Server]
        public unsafe void SyncSpatial(NetworkConnection conn)
        {
            NativePagedTable<NodeSpatialData> spatialTable = spatialManager.SpatialTable;

            for (int i = 0; i < spatialTable.maxPages; i++)
            {
                if (spatialTable.IsPageCreated(i))
                {
                    NativeArray<NodeSpatialData> page = spatialTable.GetPage(i);
                    int byteLength = page.Length * sizeof(NodeSpatialData);
                    byte[] bytes = new byte[byteLength];

                    fixed (void* dest = bytes)
                    {
                        UnsafeUtility.MemCpy(dest, page.GetUnsafeReadOnlyPtr(), byteLength);
                    }

                    TargetReceiveSpatialPage(conn, i, bytes);
                }
            }

            TargetSyncedSpatial(conn);
        }

        [TargetRpc]
        private void TargetReceiveSpatialPage(NetworkConnection conn, int pageIdx, byte[] data)
        {
            spatialManager.SpatialTable.RestorePageFromBytes(pageIdx, data);
            Debug.Log($"[SpatialManager] 接收到第 {pageIdx} 页数据");
        }

        [TargetRpc]
        private void TargetSyncedSpatial(NetworkConnection conn)
        {
            spatialManager.RebuildSpatialGrid();
        }
        #endregion
    }
}