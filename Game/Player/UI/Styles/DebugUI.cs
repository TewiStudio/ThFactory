using FishNet.Managing.Timing;
using System;
using System.Text;
using TMPro;
using UnityEngine;

namespace Tewi.Game.Player.UI.Styles
{
    internal class DebugUI : UIBase<object>
    {
        [Space(15)]
        [SerializeField] private TextMeshProUGUI debugText;
        
        public override bool IsModal => false;

        private float _debugIntervalTime = 0;
        private float _deltaTime = 0f;
        private StringBuilder debugTextSb = new(32);

        private void LateUpdate()
        {
            if (debugText)
            {
                _debugIntervalTime += Time.unscaledDeltaTime;
                _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

                if (_debugIntervalTime >= .5f)
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

            debugTextSb
                .Append(SystemInfo.graphicsDeviceType).Append(" | HDR Active: ").Append(HDROutputSettings.main.active).Append("/").Append(HDROutputSettings.main.available).Append("\n");
            
            debugTextSb
                .Append("FPS: ").Append(MathF.Round(1.0f / deltaTime, 1)).Append(" (").Append(MathF.Round(deltaTime * 1000f, 1)).Append("ms)\n");

            debugTextSb
                .Append("TPS: ").Append(gameManager.simulationManager.tps).Append(" (").Append(Math.Round(1.0f / gameManager.simulationManager.tps, 2)).Append("ms, ");
            if (gameManager.simulationManager.IdToIndex.IsCreated)
                debugTextSb.Append(gameManager.presentationManager.ActiveObservers.Count).Append("/").Append(gameManager.simulationManager.IdToIndex.Count).Append(" nodes)");
            else
                debugTextSb.Append("N/A nodes)");

            uint remoteTick = gameManager.TimeManager.LastPacketTick.RemoteTick;
            uint currentTick = gameManager.TimeManager.Tick;
            debugTextSb.Append("\nTick: ");
            if (gameManager.simulationManager.IsSimulationPaused)
            {
                debugTextSb.Append("Paused | ");
            }
            debugTextSb
                .Append("s").Append(remoteTick).Append(" / c").Append(currentTick).Append(" (diff: ").Append((int)currentTick - (int)remoteTick).Append(")\n");

            uint simulatedTickCount = gameManager.simulationManager.SimulatedTickCount;
            debugTextSb
                .Append("Simulated: ").Append(simulatedTickCount).Append(" (diff: ")
                .Append(simulatedTickCount - lastSimulatedCount).Append(" / ")
                .Append("s").Append((int)remoteTick - simulatedTickCount).Append(" / c").Append((int)currentTick - simulatedTickCount).Append(")\n");
            lastSimulatedCount = gameManager.simulationManager.SimulatedTickCount;

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
    }
}