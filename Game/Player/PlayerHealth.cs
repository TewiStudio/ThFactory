using System.Collections;
using UnityEngine;
using FishNet.Object;
using Tewi.Helpers;
using Tewi.Game.Damageable;
using Tewi.Helpers.Extensions;

namespace Tewi.Game.Player.Damageable
{
    public class PlayerHealth : Health
    {
        #region attrs
        public PlayerManager player;
        public bool godMod = false;

        [Header("Player State")]

        [Tooltip("死亡一击承受的最大伤害")]
        public const float CriticalDamageMax = 50f;

        [Tooltip("低生命值")]
        public const float CriticalHealth = 20f;

        [Tooltip("严重低生命值")]
        public const float SuperCriticalHealth = 10f;

        [Tooltip("低生命值")]
        public const float HealthSelfWhenInCriticalHealthInterval = 1f;

        [Tooltip("允许最后一次机会")]
        public bool allowLastChance = true;

        [Tooltip("致命伤治愈间隔")]
        public float HealthSelfInterval = 1f;

        public bool IsCriticalHealth => CurrentHealth < CriticalHealth;
        public bool IsSuperCriticalHealth => CurrentHealth < SuperCriticalHealth;

        [ReadOnly] public int lastFallVelocity;

        private float _lastLandedMagnitude;
        private bool _inHealthSelfLoop = false;
        #endregion

        #region Owner
        public override void OnStartClient()
        {
            base.OnStartClient();

            // 只有持有的客户端需要注册 FoundGround 事件。
            if (IsOwner)
                player.character.FoundGround += Character_FoundGround;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            if (IsOwner)
            {
                player.character.FoundGround -= Character_FoundGround;
            }
        }

        private void Character_FoundGround(ref ECM2.FindGroundResult foundGround)
        {
            float currentMag = player.characterMovement.landedVelocity.magnitude;

            // 简单的去重逻辑
            if (Mathf.Approximately(_lastLandedMagnitude, currentMag)) return;
            _lastLandedMagnitude = currentMag;

            // 只有当摔落速度超过一定阈值时才处理，以避免频繁触发。
            var fallVelocity = Mathf.RoundToInt(Mathf.Abs(currentMag));
            if (fallVelocity < 20f) return;

            // 向服务器请求处理摔落伤害。
            RequestProcessLandVelocity(fallVelocity);
        }

        protected override void OnOwnerHealthChanged(float prev, float next)
        {
            base.OnOwnerHealthChanged(prev, next);

            bool critical = next < CriticalHealth;

            // 处于极低血量状态时禁用冲刺和跳跃
            player.sprintAbility.doSprint = !critical;
            player.character.canEverJump = !critical;
        }

        protected override void OnOwnerDeath(DamageData damageData)
        {
            base.OnOwnerDeath(damageData);
            if (player.gameManager)
                player.character.TeleportPosition(player.gameManager.defaultSpawnPosition);
            else player.character.TeleportPosition(Vector3.zero);
        }
        #endregion

        #region Server
        /// <summary>
        /// 由客户端输入摔落速度，服务器计算伤害并应用。
        /// </summary>
        /// <remarks><see cref="ServerRpcAttribute"/></remarks>
        [ServerRpc]
        private void RequestProcessLandVelocity(int fallVelocity)
        {
            float amount = CalculateFallDamage(fallVelocity);
            if (amount < 0) return;
            DamageData damageData = new()
            {
                amount = amount,
                type = DamageType.Physical,
                origin = DamageOrigin.Fall,
                source = null,
                hitPoint = transform.position
            };
            ApplyDamage(damageData);
        }

        /// <summary>
        /// 根据摔落速度计算伤害。
        /// </summary>
        /// <remarks><see cref="ServerAttribute"/></remarks>
        [Server]
        private float CalculateFallDamage(int velocity)
        {
            if (velocity >= 30) return 100f;
            if (velocity >= 28) return 90f;
            if (velocity >= 26) return 80f;
            if (velocity >= 24) return 60f;
            if (velocity >= 20) return 40f;
            return 0f;
        }

        /// <summary>
        /// 应用伤害逻辑
        /// </summary>
        /// <remarks><see cref="ServerAttribute"/></remarks>
        [Server]
        public override void ApplyDamage(DamageData damageData)
        {
            if (godMod) return;

            // Last Chance 逻辑：如果这次伤害会致死且满足条件，强制保留 5 点生命值
            if (allowLastChance &&
                damageData.amount <= CriticalDamageMax &&
                CurrentHealth >= CriticalHealth &&
                CurrentHealth <= damageData.amount)
            {
                damageData.amount = CurrentHealth - 5f;
                Debug.Log($"[Server] Last Chance triggered for player {OwnerId}.");
            }

            base.ApplyDamage(damageData);

            // 如果受伤后进入危机状态且协程未运行，启动服务器自愈协程
            if (IsCriticalHealth && !_inHealthSelfLoop && CurrentHealth > 0)
            {
                StartCoroutine(ProcessHealthSelfLoop());
            }
        }

        /// <summary>
        /// 当玩家处于危机状态时，每间隔 <see cref="HealthSelfInterval"/> 自动恢复少量生命值，直到脱离危机状态或死亡。
        /// </summary>
        /// <remarks><see cref="ServerAttribute"/></remarks>
        [Server]
        private IEnumerator ProcessHealthSelfLoop()
        {
            _inHealthSelfLoop = true;
            while (IsCriticalHealth && CurrentHealth > 0)
            {
                yield return new WaitForSeconds(HealthSelfInterval);

                // 再次确认状态
                if (IsCriticalHealth && CurrentHealth > 0)
                {
                    ApplyHeal(1f);
                }
            }
            _inHealthSelfLoop = false;
        }

        protected override void OnServerDeath(DamageData damageData)
        {
            base.OnServerDeath(damageData);
            _inHealthSelfLoop = false;
            ApplyHeal(MaxHealth, true);
        }
        #endregion
    }
}