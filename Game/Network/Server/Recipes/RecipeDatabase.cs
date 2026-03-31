using System.Collections.Generic;
using UnityEngine;

namespace Tewi.Game.Network.Server.Recipe
{
    [CreateAssetMenu(menuName = "Tewi/Recipe/Recipe Database")]
    public class RecipeDatabase : ScriptableObject
    {
        public List<RecipeSO> recipes;

        private Dictionary<int, RecipeSO> lookup;

        public void Init()
        {
            lookup = new Dictionary<int, RecipeSO>();

            foreach (var res in recipes)
            {
                if (!lookup.ContainsKey(res.id))
                {
                    lookup.Add(res.id, res);
                }
                else
                {
                    Debug.LogError($"Duplicate Recipe ID: {res.id}");
                }
            }
        }

        public RecipeSO Get(int id)
        {
            return lookup[id];
        }
    }
}