using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Component.Prediction;

namespace Tewi.Game.Damageable
{
    [RequireComponent(typeof(NetworkTrigger))]
    public class DamageDealer : NetworkBehaviour
    {
        public float hitDamage = 20f;
        public float damageInterval = 1.0f;
        private readonly Dictionary<IDamageable, float> _hittables = new();
        public NetworkTrigger networkTrigger;

        protected override void OnValidate()
        {
            if (networkTrigger != null)
                networkTrigger = GetComponent<NetworkTrigger>();
            base.OnValidate();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (IsServerInitialized)
            {
                networkTrigger.OnEnter += _networkTrigger_OnEnter;
                networkTrigger.OnExit += _networkTrigger_OnExit;
                TimeManager.OnTick += TimeManager_OnTick;
            }
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            networkTrigger.OnEnter -= _networkTrigger_OnEnter;
            networkTrigger.OnExit -= _networkTrigger_OnExit;
            TimeManager.OnTick -= TimeManager_OnTick;
        }

        private void _networkTrigger_OnEnter(Collider obj)
        {
            if (obj.TryGetComponent(out IDamageable hittable))
            {
                if (!_hittables.ContainsKey(hittable))
                {
                    ServerApplyDamage(hittable);
                    _hittables.Add(hittable, Time.time + damageInterval);
                }
            }
        }

        private void _networkTrigger_OnExit(Collider obj)
        {
            if (obj.TryGetComponent(out IDamageable hittable))
            {
                _hittables.Remove(hittable);
            }
        }

        private readonly List<IDamageable> _keysCache = new();
        private void TimeManager_OnTick()
        {
            if (_hittables.Count == 0) return;

            float currentTime = Time.time;

            _keysCache.Clear();
            _keysCache.AddRange(_hittables.Keys);

            for (int i = 0; i < _keysCache.Count; i++)
            {
                IDamageable target = _keysCache[i];

                // 检查目标是否依然在字典中，防止在循环中途被删除。
                if (_hittables.TryGetValue(target, out float nextDamageTime))
                {
                    if (currentTime >= nextDamageTime)
                    {
                        ServerApplyDamage(target);
                        _hittables[target] = currentTime + damageInterval;
                    }
                }
            }
        }

        [Server]
        private void ServerApplyDamage(IDamageable target)
        {
            DamageData data = new DamageData
            {
                amount = hitDamage,
                source = this,
                type = DamageType.Physical,
                origin = DamageOrigin.Trap,
                hitPoint = transform.position
            };

            target.ApplyDamage(data);
        }
    }
}