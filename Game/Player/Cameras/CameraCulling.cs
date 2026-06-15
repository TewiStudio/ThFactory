using FishNet.Object;
using System.Collections.Generic;
using Tewi.Game.Console;
using Tewi.Game.Factory.Presentation;
using Unity.Mathematics;
using UnityEngine;

namespace Tewi.Game.Player.Cameras
{
    public class CameraCulling : NetworkBehaviour
    {
        public CameraManager cameraManager;
        public float spawnDist = 50f;
        public float despawnDist = 60f;

        private float _spawnDistSq;
        private float _despawnDistSq;
        private int _cullingInterval = 10;
        private readonly List<int> _toRemoveCache = new();

        private PlayerManager playerManager => cameraManager.playerManager;
        private SpatialManager spatialManager => playerManager.gameManager.spatialManager;
        private PresentationManager presentationManager => playerManager.gameManager.presentationManager;

        public override void OnStartClient()
        {
            base.OnStartClient();
            TimeManager.OnTick -= TimeManager_OnTick;
            if (IsOwner)
            {
                TimeManager.OnTick += TimeManager_OnTick;
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            TimeManager.OnTick -= TimeManager_OnTick;
        }

        private void TimeManager_OnTick()
        {
            UpdateCameraCulling();
        }

        private void UpdateCameraCulling()
        {
            if (!Owner.IsLocalClient) return;
            if ((Time.frameCount + GetHashCode()) % _cullingInterval != 0) return;

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
            int searchRange = Mathf.CeilToInt(spawnDist / spatial.cellSize);

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
                                    presentationManager.AddObserver(id, 1, spatialData.position, spatialData.rotation);
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
                presentationManager.RemoveObserver(_toRemoveCache[i]);
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
