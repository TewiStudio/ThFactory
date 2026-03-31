using UnityEngine;

namespace Tewi.Game.Player.Cameras
{
    public class ItemBreathing : MonoBehaviour
    {
        public float amplitude = 0.015f;
        public float frequency = 0.25f;

        Vector3 baseLocalPos;

        void Start()
        {
            baseLocalPos = transform.localPosition;
        }

        void LateUpdate()
        {
            float breath = Mathf.Sin(Time.time * Mathf.PI * 2f * frequency);
            transform.localPosition = baseLocalPos + amplitude * breath * Vector3.up;
        }
    }
}
