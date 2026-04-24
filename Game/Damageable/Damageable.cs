using UnityEngine;

namespace Tewi.Game.Damageable
{
    public interface IDamageable
    {
        void ApplyDamage(DamageData damage);
    }

    public struct DamageData
    {
        public float amount;
        public DamageDealer source;
        public DamageType type;
        public DamageOrigin origin;
        public Vector3 hitPoint;

        public override string ToString()
        {
            return $"amount: {amount}, source: {source}, type: {type}, origin: {origin}, hitPoint: {hitPoint}.";
        }
    }

    public enum DamageType
    {
        Physical,
        Fire,
        Poison,
        Holy,
        Curse
    }

    public enum DamageOrigin
    {
        Entity,      // 其他玩家或怪物
        Fall,        // 摔落
        Trap,        // 陷阱
        Environment  // 其他环境伤害
    }
}