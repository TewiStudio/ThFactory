using UnityEngine;

namespace Tewi.Game.Player.Cameras
{
    public class ItemWalkBob : MonoBehaviour
    {
        public float bobAmplitude = 0.03f;
        public float bobFrequency = 1.8f;
        public float returnSpeed = 8f;

        float phase;
        float currentStrength;
        Vector3 baseLocalPos;

        public float moveSpeed; // 外部注入（玩家速度）

        void Start()
        {
            baseLocalPos = transform.localPosition;
        }

        void LateUpdate()
        {
            float targetStrength = Mathf.Clamp01(moveSpeed);
            currentStrength = Mathf.Lerp(
                currentStrength,
                targetStrength,
                1f - Mathf.Exp(-returnSpeed * Time.deltaTime)
            );

            if (currentStrength > 0.001f)
                phase += Time.deltaTime * bobFrequency * Mathf.PI * 2f;

            float bobY = Mathf.Sin(phase) * bobAmplitude * currentStrength;
            float bobX = Mathf.Cos(phase * 0.5f) * bobAmplitude * 0.5f * currentStrength;

            transform.localPosition = baseLocalPos + new Vector3(bobX, bobY, 0f);
        }
    }

}
