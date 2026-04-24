using UnityEngine;
using System.Collections.Generic;

namespace Tewi.Game
{
    public class ColliderGroupController : MonoBehaviour
    {
        [System.Serializable]
        public class ColliderGroup
        {
            public string name;
            public Collider[] colliders;
        }

        public ColliderGroup[] groups;

        Dictionary<string, Collider[]> map;

        void Awake()
        {
            map = new();
            foreach (var g in groups)
                map[g.name] = g.colliders;
        }

        #region 获取碰撞体
        public IEnumerable<Collider> GetAllColliders()
        {
            foreach (var kv in map)
            {
                var colliders = kv.Value;
                for (int i = 0; i < colliders.Length; i++)
                    yield return colliders[i];
            }
        }

        public IEnumerable<Collider> GetEnabledColliders()
        {
            foreach (var c in GetAllColliders())
                if (c.enabled)
                    yield return c;
        }

        public IEnumerable<Collider> GetTriggers()
        {
            foreach (var c in GetAllColliders())
                if (c.isTrigger)
                    yield return c;
        }
        #endregion

        #region 遍历碰撞体
        public void ForEachCollider(System.Action<Collider> action)
        {
            foreach (var kv in map)
            {
                var colliders = kv.Value;
                for (int i = 0; i < colliders.Length; i++)
                {
                    action(colliders[i]);
                }
            }
        }

        public void ForEachCollider(
            System.Func<Collider, bool> filter,
            System.Action<Collider> action)
        {
            foreach (var kv in map)
            {
                var colliders = kv.Value;
                for (int i = 0; i < colliders.Length; i++)
                {
                    var c = colliders[i];
                    if (filter(c))
                        action(c);
                }
            }
        }
        #endregion

        public IEnumerable<Collider> GetGroups(params string[] groupNames)
        {
            foreach (var name in groupNames)
            {
                if (!map.TryGetValue(name, out var colliders))
                    continue;

                foreach (var c in colliders)
                    yield return c;
            }
        }

        public void EnableOnly(params string[] groupNames)
        {
            foreach (var kv in map)
            {
                bool enable = System.Array.Exists(groupNames, n => n == kv.Key);
                foreach (var c in kv.Value)
                    c.enabled = enable;
            }
        }
    }
}
