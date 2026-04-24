using System;
using UnityEngine;
using Tewi.Game.Factory.Core;

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