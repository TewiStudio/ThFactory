using Tewi.Game.Worlds;
using UnityEngine;

namespace Tewi.Game.Worlds
{
    [RequireComponent(typeof(ReflectionProbe))]
    public class TimeReflectionProbe : MonoBehaviour
    {
        public WorldTimeManager worldTimeManager;
        public ReflectionProbe reflectionProbe;

        private void OnValidate()
        {
            if (reflectionProbe == null)
                reflectionProbe = GetComponent<ReflectionProbe>();
        }
    }
}
