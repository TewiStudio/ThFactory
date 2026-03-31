using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace Tewi.Game.Damageable
{
    public class Health : NetworkBehaviour, IDamageable
    {
        [Header("Health State")]
        [SerializeField] private readonly SyncVar<float> _maxHealth = new(100f);
        [SerializeField] private readonly SyncVar<float> _currentHealth = new(100f);

        public float CurrentHealth => _currentHealth.Value;
        public float MaxHealth => _maxHealth.Value;

        private bool _isDead;

        public override void OnStartNetwork()
        {
            _currentHealth.OnChange += OnChangeCurrentHealth;
            
            // Server side
            if (IsServerInitialized)
            {
                _currentHealth.Value = _maxHealth.Value;
                _isDead = false;
            }
        }

        public override void OnStopNetwork()
        {
            _currentHealth.OnChange -= OnChangeCurrentHealth;
        }

        #region Server logic
        /// <summary>
        /// 服务器调用以处理伤害逻辑。
        /// </summary>
        /// <remarks><see cref="ServerAttribute"/></remarks>
        [Server]
        public virtual void ApplyDamage(DamageData damage)
        {
            if (_isDead) return;
            float nextHealth = _currentHealth.Value - damage.amount;
            _currentHealth.Value = Mathf.Clamp(nextHealth, 0, _maxHealth.Value);
            Debug.Log($"[Server] {OwnerId} damaged: {damage}.");

            ObserversTakeDamage(damage);
            if (_currentHealth.Value <= 0)
            {
                ApplyDeath(damage);
            }
        }

        /// <summary>
        /// 服务器调用以处理治疗逻辑。
        /// </summary>
        /// <remarks><see cref="ServerAttribute"/></remarks>
        [Server]
        public void ApplyHeal(float amount, bool allowRevive = false)
        {
            if (!allowRevive && _isDead) return;

            float prev = _currentHealth.Value;
            _currentHealth.Value = Mathf.Clamp(_currentHealth.Value + amount, 0, _maxHealth.Value);
            Debug.Log($"[Server] {OwnerId} heal {amount}.");

            if (_currentHealth.Value > prev)
            {
                if (_isDead && _currentHealth.Value > 0) _isDead = false;
                ObserversHeal(amount);
            }
        }

        /// <summary>
        /// 服务器调用以处理死亡逻辑。
        /// </summary>
        /// <remarks><see cref="ServerAttribute"/></remarks>
        [Server]
        private void ApplyDeath(DamageData damageData)
        {
            if (_isDead) return;
            _isDead = true; 

            Debug.Log($"[Server] {gameObject.name} death handled.");

            OnServerDeath(damageData);
            ObserversDeath(damageData);
        }

        /// <summary>
        /// 客户端请求服务器执行以立即杀死玩家。
        /// </summary>
        /// <remarks><see cref="ServerRpcAttribute"/></remarks>
        [ServerRpc]
        public void RequestKill()
        {
            ApplyDamage(new()
            {
                amount = float.MaxValue,
                hitPoint = transform.position,
                origin = DamageOrigin.Environment,
                type = DamageType.Holy,
                source = null,
            });
        }

        /// <summary>
        /// 当玩家在服务器判定死亡时执行
        /// </summary>
        /// <remarks>Server side</remarks>
        protected virtual void OnServerDeath(DamageData damageData) { }
        #endregion

        #region Client logic
        /// <summary>
        /// 服务器调用以通知所有客户端当前玩家受到伤害。
        /// </summary>
        /// <remarks><see cref="ObserversRpcAttribute"/></remarks>
        [ObserversRpc]
        private void ObserversTakeDamage(DamageData damageData)
        {
            if (IsOwner) OnOwnerTakeDamage(damageData);
            else OnObserversTakeDamage(damageData);
        }

        /// <summary>
        /// 服务器调用以通知所有客户端当前玩家被治疗。
        /// </summary>
        /// <remarks><see cref="ObserversRpcAttribute"/></remarks>
        [ObserversRpc]
        private void ObserversHeal(float amount)
        {
            if (IsOwner) OnOwnerHeal(amount);
            else OnObserversHeal(amount);
        }

        /// <summary>
        /// 服务器调用以通知所有客户端当前玩家死亡。
        /// </summary>
        /// <remarks><see cref="ObserversRpcAttribute"/></remarks>
        [ObserversRpc]
        private void ObserversDeath(DamageData damageData)
        {
            if (IsOwner) OnOwnerDeath(damageData);
            else OnObserversDeath(damageData);
        }

        // 所有人都能看到
        protected virtual void OnObserversHealthChanged(float prev, float next) { }
        protected virtual void OnObserversTakeDamage(DamageData damageData) { }
        protected virtual void OnObserversHeal(float amount) { }
        protected virtual void OnObserversDeath(DamageData damageData) { }

        // 只有 Owner 看到的
        protected virtual void OnOwnerHealthChanged(float prev, float next) { }
        protected virtual void OnOwnerTakeDamage(DamageData damageData) { }
        protected virtual void OnOwnerHeal(float amount) { }
        protected virtual void OnOwnerDeath(DamageData damageData) { }
        #endregion

        private void OnChangeCurrentHealth(float prev, float next, bool asServer)
        {
            if (IsOwner)
            {
                OnOwnerHealthChanged(prev, next);
            }
            else
            {
                OnObserversHealthChanged(prev, next);
            }
        }
    }
}
