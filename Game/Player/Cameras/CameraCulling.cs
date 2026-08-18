using FishNet.Managing.Timing;
using FishNet.Object;
using System.Collections.Generic;
using Tewi.Console;
using Tewi.Factory.Presentation;
using Tewi.Game.Interactable.Nodes;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

namespace Tewi.Game.Player.Cameras
{
    public class CameraCulling : NetworkBehaviour
    {
        public CameraManager cameraManager;
        
        [SerializeField] private Node nodePrefab;
        private IObjectPool<Node> _nodePool;

        public float spawnDist = 50f;
        public float despawnDist = 60f;
        public float CullingInterval
        {
            get => _cullingInterval;
            set => _cullingInterval = Mathf.Max(1f, value);
        }

        [SerializeField] private float _cullingInterval = .1f;
        private float _spawnDistSq;
        private float _despawnDistSq;
        private readonly List<int> _toRemoveCache = new();

        private PlayerManager playerManager => cameraManager.playerManager;
        private SpatialManager spatialManager => playerManager.gameManager.FactoryManager.spatialManager;
        private PresentationManager presentationManager => playerManager.gameManager.FactoryManager.presentationManager;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner)
            {
                CreatePool();
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            var nodeList = FindObjectsByType<Node>(FindObjectsSortMode.None);
            if (nodeList != null && nodeList.Length > 0)
            {
                foreach (var item in nodeList)
                {
                    if (_nodePool is null) break;
                    _nodePool.Release(item);
                }
            }
            _nodePool?.Clear();
        }

        private void FixedUpdate()
        {
            UpdateCameraCulling();
        }

        float _delta = 0;
        private void UpdateCameraCulling()
        {
            if (!IsClientStarted || !Owner.IsLocalClient || !playerManager.gameManager) return;

            _delta += Time.deltaTime;
            if (_delta < _cullingInterval) return;
            _delta = 0;

            Vector3 playerPos = playerManager.transform.position;

            _spawnDistSq = spawnDist * spawnDist;
            _despawnDistSq = despawnDist * despawnDist;

            var spatial = spatialManager;
            var grid = spatial.SpatialGrid;
            var table = spatial.SpatialTable;
            var activeObservers = presentationManager.ActiveObservers;

            // Spawn & Local Despawn
            int2 centerGrid = spatial.PosToGrid(playerPos);

            // 确保搜索范围覆盖 _spawnDist
            // 如果 cellSize=20, spawnDist=50，则需要 x = -3 to 3 (7x7格)
            int searchRange = Mathf.CeilToInt(spawnDist / spatial.CellSize);

            for (int x = -searchRange; x <= searchRange; x++)
            {
                for (int y = -searchRange; y <= searchRange; y++)
                {
                    int2 gridPos = centerGrid + new int2(x, y);

                    if (grid.TryGetFirstValue(gridPos, out int id, out var it))
                    {
                        do
                        {
                            var spatialData = table.Get(id);
                            float distSq = math.distancesq(spatialData.position, playerPos);

                            if (distSq < _spawnDistSq)
                            {
                                if (!activeObservers.ContainsKey(id))
                                {
                                    AddObserver(id, 1, spatialData.position, spatialData.rotation);
                                }
                            }
                        } while (grid.TryGetNextValue(out id, ref it));
                    }
                }
            }

            // cleanup
            _toRemoveCache.Clear();
            foreach (var pair in activeObservers)
            {
                int id = pair.Key;
                var spatialData = table.Get(id);

                if (!spatialData.isActive || math.distancesq(spatialData.position, playerPos) > _despawnDistSq)
                {
                    _toRemoveCache.Add(id);
                }
            }

            for (int i = 0; i < _toRemoveCache.Count; i++)
            {
                RemoveObserver(_toRemoveCache[i]);
            }
        }

        private void CreatePool()
        {
            _nodePool = new ObjectPool<Node>(
                createFunc: () => Instantiate(nodePrefab),
                actionOnGet: (node) =>
                {
                    node.gameObject.SetActive(true);
                },
                actionOnRelease: (node) =>
                {
                    presentationManager.Unsubscribe(node);
                    node.gameObject.SetActive(false);
                },
                actionOnDestroy: (node) =>
                {
                    presentationManager.Unsubscribe(node);
                    Destroy(node.gameObject);
                },
#if UNITY_EDITOR
                collectionCheck: true,
#else
                collectionCheck: false,
#endif
                defaultCapacity: 200,
                maxSize: 3000
            );
        }

        public void AddObserver(int nodeId, ushort nodeType, Vector3 position, Quaternion rotation)
        {
            Node node = _nodePool.Get();
            node.gameManager = cameraManager.playerManager.gameManager;
            node.transform.position = position;
            node.transform.rotation = rotation;
            node.nodeId = nodeId;
            presentationManager.Subscribe(node);
        }

        public void RemoveObserver(int nodeId)
        {
            if (presentationManager.ActiveObservers.TryGetValue(nodeId, out var list))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var observer = list[i];
                    if (observer is Node node)
                    {
                        _nodePool.Release(node);
                    }
                }
            }
        }

        [ConsoleCommand("pcam_cull_dist", "Set camera culling distances for spawning and despawning objects.")]
        public string DebugSetCameraDistance(float spawnDist, float despawnDist)
        {
            this.spawnDist = spawnDist;
            this.despawnDist = despawnDist;
            return $"Spawn: {spawnDist}, Despawn: {despawnDist}";
        }

        [ConsoleCommand("pcam_cull_interval", "Set the interval (in frames) for camera culling checks.")]
        public string DebugSetCullingInterval(int interval)
        {
            _cullingInterval = interval;
            return $"Culling Interval: {interval} frames";
        }
    }
}
