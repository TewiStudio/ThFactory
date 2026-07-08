using System;
using UnityEngine;
using FishNet.Object;

namespace Tewi.Game.Network.Server
{
    [ExecuteAlways]
    public class WorldTimeManager : NetworkBehaviour
    {
        [Header("Time Settings")]
        public int startHour = 8;
        public int startMinute = 0;
        [Tooltip("现实中多少分钟等于游戏内一天")]
        public float dayLengthInMinutes = 10;

        [Header("Read Only (Debug)")]
        public double currentTime;
        public string currentTimeDisplay;

        [Header("World Light Settings")]
        public Transform directionalLights;
        public Light sun;
        public Light moon;
        public float axisTilt = 23.5f;

        // 初始偏移量（以秒为单位）
        private double _startSeconds;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (!Application.isPlaying)
            {
                SyncTimeFromPreview();
            }
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _startSeconds = (startHour * 3600) + (startMinute * 60);

            if (TimeManager != null)
                TimeManager.OnTick += OnTick;
        }

        public override void OnStopNetwork()
        {
            if (TimeManager != null)
                TimeManager.OnTick -= OnTick;
            base.OnStopNetwork();
        }

        private void OnTick()
        {
            CalculateTimeBasedOnTick();
            UpdateLighting();
            UpdateDebugDisplay();
        }

        private void CalculateTimeBasedOnTick()
        {
            if (dayLengthInMinutes <= 0) return;

            // 核心逻辑：
            // 1. 获取服务器当前的 Tick 频率 (例如 50 TPS)
            uint tickRate = TimeManager.TickRate;

            // 2. 计算游戏内一天（86400秒）在现实中对应的总 Tick 数
            // 公式：现实分钟 * 60秒 * 每秒Tick数
            double totalTicksPerGameDay = (dayLengthInMinutes * 60.0) * tickRate;

            // 3. 将初始时间转换为“初始 Tick 偏移”
            // 公式：(初始秒数 / 86400) * 一天的总Tick
            double startTickOffset = (_startSeconds / 86400.0) * totalTicksPerGameDay;

            // 4. 获取当前总 Tick 并加上偏移，然后取模实现循环
            double currentTickInCycle = (TimeManager.Tick + startTickOffset) % totalTicksPerGameDay;

            // 5. 计算当前时间占一整天的百分比
            double dayProgress = currentTickInCycle / totalTicksPerGameDay;

            // 6. 映射回 0-86400 秒
            currentTime = dayProgress * 86400.0;
        }

        private void SyncTimeFromPreview()
        {
            currentTime = (startHour * 3600) + (startMinute * 60);
        }

        private void UpdateLighting()
        {
            if (!directionalLights || !sun || !moon) return;

            float timePercent = (float)(currentTime / 86400.0);
            float angle = (timePercent * 360f) - 90f;

            directionalLights.localRotation = Quaternion.Euler(angle, 0, 0);
            directionalLights.localRotation *= Quaternion.Euler(0, axisTilt, 0);

            // 阴影切换
            bool isDay = !(angle <= -17f || angle >= 195.5f);
            sun.shadows = isDay ? LightShadows.Soft : LightShadows.None;
            moon.shadows = isDay ? LightShadows.None : LightShadows.Soft;
        }

        private void UpdateDebugDisplay()
        {
            TimeSpan ts = TimeSpan.FromSeconds(currentTime);
            currentTimeDisplay = string.Format("{0:D2}:{1:D2}:{2:D2} (Tick: {3})",
                ts.Hours, ts.Minutes, ts.Seconds,
                Application.isPlaying ? TimeManager.Tick.ToString() : "Editor");
        }

        private void Update()
        {
            // 仅用于编辑器预览模式下的平滑刷新
            if (!Application.isPlaying)
            {
                SyncTimeFromPreview();
                UpdateLighting();
                UpdateDebugDisplay();
            }
        }
    }
}