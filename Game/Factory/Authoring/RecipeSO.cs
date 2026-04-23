using System;
using System.Collections.Generic;
using Tewi.Game.Factory.Core;
using UnityEngine;

namespace Tewi.Game.Factory.Authoring
{

    //[CreateAssetMenu(menuName = "Tewi/Recipe/New Recipe")]
    [Serializable]
    public class RecipeSO
    {
        public GlobalID id;

        public List<AuthoringResourceStack> inputs;

        public List<AuthoringResourceStack> outputs;

        [Tooltip("Seconds to complete the recipe. If zero, the recipe will be completed immediately.")]
        public float duration = 1f;
    }
}