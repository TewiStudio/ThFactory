using System;
using System.Collections.Generic;
using Tewi.Game.Network.Core;
using UnityEngine;

namespace Tewi.Game.Network.Authoring
{

    //[CreateAssetMenu(menuName = "Tewi/Recipe/New Recipe")]
    [Serializable]
    public class RecipeSO
    {
        public GlobalID id;

        public List<AuthoringResourceStack> inputs;

        public List<AuthoringResourceStack> outputs;

        public float duration = 1f;
    }
}