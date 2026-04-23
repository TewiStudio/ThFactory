using System;
using Tewi.Helpers;
using UnityEngine;

namespace Tewi.Game.Factory.Core
{
    [Serializable]
    public struct GlobalID : ISerializationCallbackReceiver
    {
        public string @namespace;
        public string category;
        public string name;

        [ReadOnly] public string cachedFullID;

        // 当 Unity 序列化（保存到磁盘）时自动触发
        public void OnBeforeSerialize()
        {
            UpdateCachedFullID();
        }

        // 当 Unity 反序列化（从磁盘读取）后自动触发
        public void OnAfterDeserialize() { }

        public void UpdateCachedFullID()
        {
            cachedFullID = ToString();
        }

        public readonly bool IsValid()
        {
            return !string.IsNullOrEmpty(@namespace) && !string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(name);
        }

        public override string ToString()
        {
            return $"{@namespace}.{category}.{name}";
        }
    }
}
