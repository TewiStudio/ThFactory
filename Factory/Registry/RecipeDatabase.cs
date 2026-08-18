using System.Collections.Generic;
using UnityEngine;
using Tewi.Factory.Authoring;

namespace Tewi.Factory.Registry
{
    [CreateAssetMenu(menuName = "Tewi/Recipe/Recipe Database")]
    public class RecipeDatabase : ScriptableObject
    {
        public List<RecipeSO> recipes;

        // string -> int
        private readonly Dictionary<string, int> _stringToIntMap = new();

        // int -> string
        private readonly Dictionary<int, string> _intToStringMap = new();

        // int -> RecipeSO
        private readonly Dictionary<int, RecipeSO> _intToRecipeSOMap = new();

        public Dictionary<string, int> StringToIntMap => _stringToIntMap;
        public Dictionary<int, string> IntToStringMap => _intToStringMap;
        public Dictionary<int, RecipeSO> IntToRecipeSOMap => _intToRecipeSOMap;

        public void Init()
        {
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

        public void Clear()
        {
            _stringToIntMap.Clear();
            _intToStringMap.Clear();
            _intToRecipeSOMap.Clear();
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

        public void GetStringId(int runtimeId, out string stringId)
        {
            _intToStringMap.TryGetValue(runtimeId, out stringId);
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