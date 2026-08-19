using FishNet.Managing.Timing;
using System;
using System.Text;
using Tewi.Console;
using Tewi.Game.Network;
using TMPro;
using UnityEngine;

namespace Tewi.Game.UI.Styles
{
    internal class DebugUI : UIBase<object>
    {
        [Space(15)]
        [SerializeField] private TextMeshProUGUI debugText;
        
        public override bool IsModal => false;
        public float intervalTime = 0.5f;

        private float _debugIntervalTime = 0;
        private float _deltaTime = 0f;
        private StringBuilder debugTextSb = new(32);

        private void LateUpdate()
        {
            if (debugText)
            {
                _debugIntervalTime += Time.unscaledDeltaTime;
                _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

                if (_debugIntervalTime >= intervalTime)
                {
                    debugText.text = GetDebugText(_deltaTime);
                    _debugIntervalTime = 0f;
                }
            }
        }

        uint lastSimulatedCount = 0;
        private string GetDebugText(float deltaTime)
        {
            debugTextSb.Clear();

            if (!uiManager.IsPlayerReady) return string.Empty;
            debugTextSb
                .Append(SystemInfo.graphicsDeviceType).Append(" | HDR Active: ").Append(HDROutputSettings.main.active).Append("/").Append(HDROutputSettings.main.available).Append("\n");
            
            debugTextSb
                .Append("FPS: ").Append(MathF.Round(1.0f / deltaTime, 1)).Append(" (").Append(MathF.Round(deltaTime * 1000f, 1)).Append("ms)\n");

            if (gameManager.FactoryManager && gameManager.PresentationManager is not null)
            {
                FactoryManager factoryManager = gameManager.FactoryManager;
                Factory.Simulation.SimulationManager simulationManager = factoryManager.simulationManager;
                Factory.Presentation.PresentationManager presentationManager = factoryManager.presentationManager;
                
                debugTextSb
                    .Append("TPS: ").Append(factoryManager.tps).Append('/').Append(gameManager.TimeManager.TickRate)
                    .Append(" (").Append(Math.Round(1.0f / factoryManager.tps, 2)).Append("ms, ");
                if (simulationManager.IdToIndex.IsCreated)
                    debugTextSb.Append(presentationManager.ActiveObservers.Count).Append("/").Append(simulationManager.IdToIndex.Count).Append(" nodes)");
                else
                    debugTextSb.Append("N/A nodes)");

                uint remoteTick = gameManager.TimeManager.LastPacketTick.RemoteTick;
                uint currentTick = gameManager.TimeManager.Tick;
                debugTextSb.Append("\nTick ");
                if (simulationManager.IsSimulationPaused)
                {
                    debugTextSb.Append("paused | ");
                }
                debugTextSb.Append("remote: ").Append(remoteTick).Append(" / local: ").Append(currentTick).Append(" (delta: ").Append((int)currentTick - (int)remoteTick).Append(")\n");

                uint simulatedTickCount = simulationManager.CurrentTick;
                debugTextSb
                    .Append("Simulated: ").Append(simulatedTickCount).Append(" / Network: ").Append(factoryManager.NetworkSimulationTick)
                    .Append(" (delta: ").Append(simulatedTickCount - lastSimulatedCount).Append(")\n");

                debugTextSb.Append(simulationManager.SimulationHash.ToString("X16")).Append("\n");
                lastSimulatedCount = simulationManager.CurrentTick;
            }

            debugTextSb.Append("HP: ").Append(playerManager.playerHealth.CurrentHealth);
            return debugTextSb.ToString();
        }

        private void Start()
        {
            if (HDROutputSettings.main.available &&
                !HDROutputSettings.main.active)
            {
                HDROutputSettings.main.RequestHDRModeChange(true);
            }
        }

        [ConsoleCommand("debug_setinterval")]
        public string DebugSetInterval(float interval)
        {
            if (interval <= 0f) return "Interval must be greater than 0.";
            intervalTime = interval;
            return $"Debug interval set to {intervalTime} seconds.";
        }
    }
}