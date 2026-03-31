using System.Collections.Generic;
using Tewi.Game.Network.Server.Core;
using UnityEngine;

namespace Tewi.Game.Network.Server.Recipe
{
    [CreateAssetMenu(menuName = "Tewi/Recipe/New Recipe")]
    public class RecipeSO : ScriptableObject
    {
        public int id;

        public List<ResourceStack> inputs;

        public List<ResourceStack> outputs;

        public float duration = 1f;
    }

    public struct RecipeData
    {
        public int id;
        public float duration;

        public ResourceStack input1, input2, input3, input4, input5, input6;
        public ResourceStack output1, output2, output3, output4, output5, output6;
    }
}