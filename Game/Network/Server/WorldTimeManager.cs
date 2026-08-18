using FishNet.Object;
using System;
using Tewi.Helpers;
using UnityEngine;

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
        [ReadOnly] public double currentTime;
        [ReadOnly] public string currentTimeDisplay;

        [Header("World Light Settings")]
        public Transform directionalLights;
        public Light sun;
        public Light moon;
        public float axisTilt = 23.5f;
        public float sunIntensity = 255;
        public float moonIntensity = 0.4f;
        public float ambientIntensityDay = 0.7f;
        public float ambientIntensityNight = 0.3f;
        public float transitionRange = 0.15f;

        public LightShadows sunShadowType = LightShadows.Soft;
        public float maxSunShadowStrength = 1.0f;
        public LightShadows moonShadowType = LightShadows.Soft;
        public float maxMoonShadowStrength = 1.0f;
        public float shadowFadeSpeed = 2.0f;
/*
        public HDAdditionalLightData _sunData;
        public HDAdditionalLightData _moonData;
*/
        private enum ShadowCasterType { Sun, Moon }
        private ShadowCasterType _currentShadowCaster = ShadowCasterType.Sun;

        private float _currentSunShadowStrength = 1f;
        private float _currentMoonShadowStrength = 1f;
        private bool _isShadowInitialized = false;

        // 初始偏移量（以秒为单位）
        private double _startSeconds;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (!Application.isPlaying)
            {
                SyncTimeFromPreview();
                _isShadowInitialized = false;
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

        private void InitializeShadowStates(float dayWeight)
        {
            if (_isShadowInitialized) return;
            _isShadowInitialized = true;

            // 首次运行或载入时，硬定位阴影强度，防止进入游戏时阴影产生多余的“渐入”过程
            if (dayWeight > 0f)
            {
                _currentShadowCaster = ShadowCasterType.Sun;
                _currentSunShadowStrength = maxSunShadowStrength;
                _currentMoonShadowStrength = 0f;
            }
            else
            {
                _currentShadowCaster = ShadowCasterType.Moon;
                _currentSunShadowStrength = 0f;
                _currentMoonShadowStrength = maxMoonShadowStrength;
            }
        }

        private void UpdateLighting()
        {
            if (!directionalLights || !sun || !moon) return;

            float timePercent = (float)(currentTime / 86400.0);
            float angle = timePercent * 360f - 90f;

            directionalLights.localRotation =
                Quaternion.Euler(angle, 0, 0) *
                Quaternion.Euler(0, axisTilt, 0);

            float sunHeight = Mathf.Sin(angle * Mathf.Deg2Rad);

            float dayWeight = Mathf.InverseLerp(-transitionRange, transitionRange, sunHeight);
            float nightWeight = Mathf.InverseLerp(transitionRange, -transitionRange, sunHeight);

            dayWeight = Mathf.SmoothStep(0f, 1f, dayWeight);
            nightWeight = Mathf.SmoothStep(0f, 1f, nightWeight);

            sun.intensity = Mathf.Lerp(0f, sunIntensity, dayWeight);
            moon.intensity = Mathf.Lerp(0f, moonIntensity, nightWeight);

            RenderSettings.ambientIntensity = Mathf.Lerp(ambientIntensityNight, ambientIntensityDay, dayWeight);

            InitializeShadowStates(dayWeight);

            if (_currentShadowCaster == ShadowCasterType.Sun)
            {
                if (dayWeight <= 0f)
                {
                    _currentShadowCaster = ShadowCasterType.Moon;
                    _currentMoonShadowStrength = 0f;
                }
            }
            else
            {
                if (nightWeight <= 0f)
                {
                    _currentShadowCaster = ShadowCasterType.Sun;
                    _currentSunShadowStrength = 0f;
                }
            }

            float deltaTime = Application.isPlaying ? Time.deltaTime : 0.016f;

            if (_currentShadowCaster == ShadowCasterType.Sun)
            {
                sun.shadows = sunShadowType;
                moon.shadows = LightShadows.None;

                _currentSunShadowStrength = Mathf.MoveTowards(_currentSunShadowStrength, maxSunShadowStrength, deltaTime * shadowFadeSpeed);
                //_sunData.shadowDimmer = _currentSunShadowStrength;

                _currentMoonShadowStrength = 0f;
                //_moonData.shadowDimmer = 0f;
            }
            else
            {
                sun.shadows = LightShadows.None;
                moon.shadows = moonShadowType;

                _currentMoonShadowStrength = Mathf.MoveTowards(_currentMoonShadowStrength, maxMoonShadowStrength, deltaTime * shadowFadeSpeed);
                //_moonData.shadowDimmer = _currentMoonShadowStrength;

                _currentSunShadowStrength = 0f;
                //_sunData.shadowDimmer = 0f;
            }
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