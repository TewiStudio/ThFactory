using UnityEngine;

namespace Tewi.Game.Network.Server.ResourceDatas
{
    [CreateAssetMenu(menuName = "Tewi/Resource Data/Resource Type")]
    public class ResourceType : ScriptableObject
    {
        [Header("Info")]
        public int id;
        public string displayName;

        [Header("Stack")]
        public int maxStack = 100;

        [Header("Tag")]
        public int tags;

        [Header("Client Views")]
        public GameObject prefab;
    }

    public struct ResourceData
    {
        public int id;
        public int maxStack;
        public int tags;
    }
}