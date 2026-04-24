using System.Collections.Generic;
using UnityEngine;
using Tewi.Game.Factory.Authoring;

namespace Tewi.Game.Factory.Registry
{
    [CreateAssetMenu(menuName = "Tewi/Resource Data/Resource Database")]
    public class ResourceDatabase : ScriptableObject
    {
        public List<ResourceType> resources;

        // string -> int 
        private Dictionary<string, int> _stringToIntMap;

        // int -> string
        private Dictionary<int, string> _intToStringMap;

        // int -> ResourceType
        private Dictionary<int, ResourceType> _intToResourceMap;

        public void Init()
        {
            _stringToIntMap = new Dictionary<string, int>();
            _intToStringMap = new Dictionary<int, string>();
            _intToResourceMap = new Dictionary<int, ResourceType>();

            int currentRuntimeId = 1;

            foreach (var res in resources)
            {
                // 防检查是否漏填了 ID
                if (!res.id.IsValid())
                {
                    Debug.LogWarning($"[ResourceDatabase] ResourceType ID is empty, ignore.");
                    continue;
                }

                string id = res.id.ToString();
                // 检查是否填写了重复的 ID
                if (!_stringToIntMap.ContainsKey(id))
                {
                    // 建立双向映射
                    _stringToIntMap.Add(id, currentRuntimeId);
                    _intToStringMap.Add(currentRuntimeId, id);
                    _intToResourceMap.Add(currentRuntimeId, res);

                    currentRuntimeId++;
                }
                else
                {
                    Debug.LogError($"[ResourceDatabase] ResourceType ID repeated: '{res.id}'");
                }
            }

            Debug.Log($"[ResourceDatabase] Inited {_stringToIntMap.Count} resources。");
        }

        public int GetRuntimeId(string stringId)
        {
            if (string.IsNullOrEmpty(stringId)) return 0;

            if (_stringToIntMap.TryGetValue(stringId, out int runtimeId))
            {
                return runtimeId;
            }
            Debug.LogError($"[ResourceDatabase] Resource ID not found: {stringId}");
            return 0;
        }

        public void GetStringId(int runtimeId, out string stringId)
        {
            _intToStringMap.TryGetValue(runtimeId, out stringId);
        }

        public ResourceType GetResource(int runtimeId)
        {
            if (runtimeId == 0) return null;

            if (_intToResourceMap.TryGetValue(runtimeId, out ResourceType res))
            {
                return res;
            }
            return null;
        }

        public int GetTotalResourceCount()
        {
            return _stringToIntMap != null ? _stringToIntMap.Count : 0;
        }
    }
}