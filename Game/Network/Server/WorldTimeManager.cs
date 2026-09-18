using FishNet.Object;
using System;
using Tewi.Helpers;
using UnityEngine;
using UnityEngine.Rendering; // 引用渲染管线相关命名空间

namespace Tewi.Game.Network.Server
{
    [ExecuteAlways]
    public class WorldTimeManager : NetworkBehaviour
    {
        public NetworkGameManager networkGameManager;

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

        [Header("Stylized Skybox Integration")]
        public Material skyboxMaterial; // 拖入你手写的着色器材质
        private int resolution = 128;
        [Tooltip("环境刷新频率(秒)，设置为0则每帧刷新")]
        public float envUpdateInterval = 0.5f;
        private float _nextEnvUpdate;

        private RenderTexture _skyReflection;
        private Camera _reflectionCamera;

        [Header("Shadow Settings")]
        public LightShadows sunShadowType = LightShadows.Soft;
        public float maxSunShadowStrength = 1.0f;
        public LightShadows moonShadowType = LightShadows.Soft;
        public float maxMoonShadowStrength = 1.0f;
        public float shadowFadeSpeed = 2.0f;

        private enum ShadowCasterType { Sun, Moon }
        private ShadowCasterType _currentShadowCaster = ShadowCasterType.Sun;

        private float _currentSunShadowStrength = 1f;
        private float _currentMoonShadowStrength = 1f;
        private bool _isShadowInitialized = false;

        private double _startSeconds;

        // 缓存 Shader 属性 ID
        private static readonly int DayNightFactorID = Shader.PropertyToID("_DayNightFactor");

        private void Awake()
        {
            CreateReflection();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (!Application.isPlaying)
            {
                SyncTimeFromPreview();
                UpdateLighting(); // 编辑器下实时刷新
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

        private void CreateReflection()
        {
            _skyReflection = new RenderTexture(
                32,
                32,
                24,
                RenderTextureFormat.ARGBHalf
            );

            _skyReflection.dimension = UnityEngine.Rendering.TextureDimension.Cube;
            _skyReflection.volumeDepth = 6;
            _skyReflection.useMipMap = true;
            _skyReflection.autoGenerateMips = true;
            _skyReflection.filterMode = FilterMode.Bilinear;
            _skyReflection.wrapMode = TextureWrapMode.Clamp;

            _skyReflection.Create();


            GameObject go =
                new GameObject("Sky Reflection Camera");

            go.hideFlags =
                HideFlags.HideAndDontSave;

            _reflectionCamera =
                go.AddComponent<Camera>();

            _reflectionCamera.enabled = false;

            _reflectionCamera.clearFlags =
                CameraClearFlags.Skybox;

            _reflectionCamera.cullingMask = 0;

            _reflectionCamera.nearClipPlane = 0.01f;
            _reflectionCamera.farClipPlane = 1000f;

            _reflectionCamera.allowHDR = true;


            RenderSettings.defaultReflectionMode =
                DefaultReflectionMode.Custom;

            RenderSettings.customReflectionTexture =
                _skyReflection;
        }

        private int _face;
        private void UpdateReflection()
        {
            if (networkGameManager?.localPlayer is null) return;

            Camera mainCamera = networkGameManager.localPlayer.cameraManager.playerCamera;

            if (mainCamera == null)
                return;

            _reflectionCamera.transform.position =
                mainCamera.transform.position;

            _reflectionCamera.RenderToCubemap(
                _skyReflection,
                1 << _face
            );
            _face = (_face + 1) % 6;
        }

        private void CalculateTimeBasedOnTick()
        {
            if (dayLengthInMinutes <= 0) return;

            uint tickRate = TimeManager.TickRate;
            double totalTicksPerGameDay = (dayLengthInMinutes * 60.0) * tickRate;
            double startTickOffset = (_startSeconds / 86400.0) * totalTicksPerGameDay;
            double currentTickInCycle = (TimeManager.Tick + startTickOffset) % totalTicksPerGameDay;
            double dayProgress = currentTickInCycle / totalTicksPerGameDay;
            currentTime = dayProgress * 86400.0;
        }

        private void SyncTimeFromPreview()
        {
            // currentTime = (startHour * 3600) + (startMinute * 60);
        }

        private void InitializeShadowStates(float dayWeight)
        {
            if (_isShadowInitialized) return;
            _isShadowInitialized = true;

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

            // 1. 更新光源旋转
            directionalLights.localRotation =
                Quaternion.Euler(angle, 0, 0) *
                Quaternion.Euler(0, axisTilt, 0);

            float sunHeight = Mathf.Sin(angle * Mathf.Deg2Rad);

            // 2. 计算权重
            float dayWeight = Mathf.InverseLerp(-transitionRange, transitionRange, sunHeight);
            float nightWeight = Mathf.InverseLerp(transitionRange, -transitionRange, sunHeight);

            dayWeight = Mathf.SmoothStep(0f, 1f, dayWeight);
            nightWeight = Mathf.SmoothStep(0f, 1f, nightWeight);

            // 3. 更新着色器参数 (核心修改)
            if (skyboxMaterial != null)
            {
                // nightWeight 在深夜是 1，白天是 0，完美对应我们的 _DayNightFactor
                //skyboxMaterial.SetFloat(DayNightFactorID, nightWeight);
                RenderSettings.skybox.SetFloat(DayNightFactorID, nightWeight);
            }

            // 4. 更新光源强度
            sun.intensity = Mathf.Lerp(0f, sunIntensity, dayWeight);
            moon.intensity = Mathf.Lerp(0f, moonIntensity, nightWeight);

            // 5. 环境光感应
            RenderSettings.ambientIntensity = Mathf.Lerp(ambientIntensityNight, ambientIntensityDay, dayWeight);

            // 6. 刷新场景 GI（反射和环境照明）
            UpdateEnvironmentLighting();

            // 7. 阴影管理逻辑 (适配 URP)
            HandleShadows(dayWeight, nightWeight);
        }

        private void UpdateEnvironmentLighting()
        {
            if (!Application.isPlaying)
            {
                UpdateReflection();
                return;
            }

            if (Time.time >= _nextEnvUpdate)
            {
                UpdateReflection();
                _nextEnvUpdate = Time.time + envUpdateInterval;
            }
        }

        private void HandleShadows(float dayWeight, float nightWeight)
        {
            InitializeShadowStates(dayWeight);

            if (_currentShadowCaster == ShadowCasterType.Sun)
            {
                if (dayWeight <= 0f) _currentShadowCaster = ShadowCasterType.Moon;
            }
            else
            {
                if (nightWeight <= 0f) _currentShadowCaster = ShadowCasterType.Sun;
            }

            float deltaTime = Application.isPlaying ? Time.deltaTime : 0.016f;

            if (_currentShadowCaster == ShadowCasterType.Sun)
            {
                sun.shadows = sunShadowType;
                moon.shadows = LightShadows.None;
                _currentSunShadowStrength = Mathf.MoveTowards(_currentSunShadowStrength, maxSunShadowStrength, deltaTime * shadowFadeSpeed);
                sun.shadowStrength = _currentSunShadowStrength;
            }
            else
            {
                sun.shadows = LightShadows.None;
                moon.shadows = moonShadowType;
                _currentMoonShadowStrength = Mathf.MoveTowards(_currentMoonShadowStrength, maxMoonShadowStrength, deltaTime * shadowFadeSpeed);
                moon.shadowStrength = _currentMoonShadowStrength;
            }
        }

        private void UpdateDebugDisplay()
        {
            TimeSpan ts = TimeSpan.FromSeconds(currentTime);
            currentTimeDisplay = string.Format("{0:D2}:{1:D2}:{2:D2} (Tick: {3})",
                ts.Hours, ts.Minutes, ts.Seconds,
                Application.isPlaying && TimeManager != null ? TimeManager.Tick.ToString() : "Editor");
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                // 在编辑器里，我们根据 startHour 和 startMinute 手动计算预览时间
                currentTime = (startHour * 3600) + (startMinute * 60);
                UpdateLighting();
                UpdateDebugDisplay();
            }
        }
    }
}