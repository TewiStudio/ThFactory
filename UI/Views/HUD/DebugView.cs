using FishNet.Managing.Timing;
using System;
using System.Text;
using Tewi.Game.Network;
using Tewi.Game.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tewi.Game.UI.Views
{
    public sealed class DebugView : ViewUI<Label>
    {
        public override string ElementName => "DebugText";

        private void Update()
        {
            if (Element != null)
                Element.text = GetDebugText(Time.unscaledDeltaTime);
        }

        private StringBuilder debugTextSb = new(32);
        uint lastSimulatedCount = 0;
        private string GetDebugText(float deltaTime)
        {
            debugTextSb.Clear();

            if (!IsPlayerReady) return string.Empty;
            debugTextSb
                .Append(SystemInfo.graphicsDeviceType).Append(" | HDR Active: ").Append(HDROutputSettings.main.active).Append("/").Append(HDROutputSettings.main.available).Append("\n");

            debugTextSb
                .Append("FPS: ").Append(MathF.Round(1.0f / deltaTime, 1)).Append(" (").Append(MathF.Round(deltaTime * 1000f, 1)).Append("ms)\n");

            if (GameManager.FactoryManager && GameManager.PresentationManager is not null)
            {
                FactoryManager factoryManager = GameManager.FactoryManager;
                Factory.Simulation.SimulationManager simulationManager = factoryManager.simulationManager;
                Factory.Presentation.PresentationManager presentationManager = factoryManager.presentationManager;

                debugTextSb
                    .Append("TPS: ").Append(factoryManager.tps).Append('/').Append(GameManager.TimeManager.TickRate)
                    .Append(" (").Append(Math.Round(1.0f / factoryManager.tps, 2)).Append("ms, ");
                if (simulationManager.IdToIndex.IsCreated)
                    debugTextSb.Append(presentationManager.ActiveObservers.Count).Append("/").Append(simulationManager.IdToIndex.Count).Append(" nodes)");
                else
                    debugTextSb.Append("N/A nodes)");

                uint remoteTick = GameManager.TimeManager.LastPacketTick.RemoteTick;
                uint currentTick = GameManager.TimeManager.Tick;
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

            debugTextSb.Append("HP: ").Append(PlayerManager.playerHealth.CurrentHealth);
            return debugTextSb.ToString();
        }
    }
}