using System.Collections.Generic;
using UnityEngine;

namespace Tewi.Game.Network.Server.ResourceDatas
{
    [CreateAssetMenu(menuName = "Tewi/Resource Data/Resource Database")]
    public class ResourceDatabase : ScriptableObject
    {
        public List<ResourceType> resources;

        private Dictionary<int, ResourceType> lookup;

        public void Init()
        {
            lookup = new Dictionary<int, ResourceType>();

            foreach (var res in resources)
            {
                if (!lookup.ContainsKey(res.id))
                {
                    lookup.Add(res.id, res);
                }
                else
                {
                    Debug.LogError($"Duplicate Resource ID: {res.id}");
                }
            }
        }

        public ResourceType Get(int id)
        {
            return lookup[id];
        }
    }
}
