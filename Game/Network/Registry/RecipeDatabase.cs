using System.Collections.Generic;
using Tewi.Game.Network.Authoring;
using UnityEngine;

namespace Tewi.Game.Network.Registry
{
    [CreateAssetMenu(menuName = "Tewi/Recipe/Recipe Database")]
    public class RecipeDatabase : ScriptableObject
    {
        public List<RecipeSO> recipes;

        // string -> int
        private Dictionary<string, int> _stringToIntMap;

        // int -> string
        private Dictionary<int, string> _intToStringMap;

        // int -> RecipeSO
        private Dictionary<int, RecipeSO> _intToRecipeSOMap;

        public void Init()
        {
            _stringToIntMap = new Dictionary<string, int>();
            _intToStringMap = new Dictionary<int, string>();
            _intToRecipeSOMap = new Dictionary<int, RecipeSO>();

            // 运行时 ID 从 1 开始分配，0 为 null
            int currentRuntimeId = 1;

            foreach (var recipe in recipes)
            {
                // 检查是否漏填了 ID
                if (!recipe.id.IsValid())
                {
                    Debug.LogWarning($"[RecipeDatabase] RecipeSO ID is empty, ignore.");
                    continue;
                }

                string id = recipe.id.ToString();
                // 检查是否填写了重复的 ID
                if (!_stringToIntMap.ContainsKey(id))
                {
                    // 建立双向映射
                    _stringToIntMap.Add(id, currentRuntimeId);
                    _intToStringMap.Add(currentRuntimeId, id);
                    _intToRecipeSOMap.Add(currentRuntimeId, recipe);

                    currentRuntimeId++;
                }
                else
                {
                    Debug.LogError($"[RecipeDatabase] Recipe id repeated: '{recipe.id}'");
                }
            }

            Debug.Log($"[RecipeDatabase] Inited {_stringToIntMap.Count} recipes.");
        }

        public int GetRuntimeId(string stringId)
        {
            if (_stringToIntMap.TryGetValue(stringId, out int runtimeId))
            {
                return runtimeId;
            }
            Debug.LogError($"[RecipeDatabase] Recipe ID not found: {stringId}");
            return 0;
        }

        public string GetStringId(int runtimeId)
        {
            if (_intToStringMap.TryGetValue(runtimeId, out string stringId))
            {
                return stringId;
            }
            return string.Empty;
        }

        public RecipeSO GetRecipeSO(int runtimeId)
        {
            if (_intToRecipeSOMap.TryGetValue(runtimeId, out RecipeSO so))
            {
                return so;
            }
            return null;
        }

        public int GetTotalRecipeCount()
        {
            return _stringToIntMap != null ? _stringToIntMap.Count : 0;
        }
    }
}