using System;
using Tewi.Helpers;
using UnityEngine;

namespace Tewi.Game.Worlds
{
    public class WorldTimeManager : MonoBehaviour
    {
        [Header("Time")]
        public int startHour = 8;
        public int startMinute = 0;
        public float dayLengthInMinutes = 10; // 一天现实里用几分钟模拟
        public double currentTime;
        public TimeSpan CurrentTimeSpan => TimeSpan.FromSeconds(currentTime);

        [Header("World Light Settings")]
        public Transform directionalLights;
        public Light sun;
        public Light moon;
        public float axisTilt = 23.5f;
        private float eTime = 0;

        private void OnValidate()
        {
            currentTime = new TimeSpan(startHour, startMinute, 0).TotalSeconds;
            UpdateSunAndMoonPosition();
        }

        public void OnWorldStart()
        {
            currentTime = new TimeSpan(startHour, startMinute, 0).TotalSeconds;
        }

        public void UpdateSunAndMoonPosition()
        {
            // 计算角度（0:00 对应 -90°，12:00 对应 90°）
            float timePercent = (float)(currentTime % 86400) / 86400f;
            float angle = timePercent * 360f - 90f;

            directionalLights.transform.localRotation = Quaternion.Euler(angle, 0, 0);
            directionalLights.transform.localRotation *= Quaternion.Euler(0, axisTilt, 0);

            if (angle <= -17 || angle >= 195.5)
            {
                sun.shadows = LightShadows.None;
                moon.shadows = LightShadows.Hard;
            }
            else
            {
                sun.shadows = LightShadows.Soft;
                moon.shadows = LightShadows.None;
            }
        }

        private void FixedUpdate()
        {
            // 每帧推进当前时间
            if (dayLengthInMinutes > 0)
            {
                double daySeconds = dayLengthInMinutes * 60.0;
                double secondsPerGameSecond = daySeconds / 86400.0; // 真实秒对应游戏秒
                currentTime += Time.deltaTime / secondsPerGameSecond;
            }

            eTime += Time.deltaTime;
            if (eTime >= 0)
            {
                eTime = 0;
                UpdateSunAndMoonPosition();
            }
        }
    }
}