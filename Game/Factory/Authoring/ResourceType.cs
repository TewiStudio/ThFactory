using System;
using Tewi.Game.Factory.Core;
using UnityEngine;

namespace Tewi.Game.Factory.Authoring
{
    //[CreateAssetMenu(menuName = "Tewi/Resource Data/Resource Type")]
    [Serializable]
    public class ResourceType
    {
        public GlobalID id;
        public int maxStack = 100;
        public int tags;

        [Header("Client Views")]
        public GameObject prefab;
    }
}