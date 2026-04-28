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

        private string GetDebugText(float deltaTime)
        {
            debugTextSb.Clear();
            debugTextSb.Append("FPS: ");
            debugTextSb.Append(MathF.Round(1.0f / deltaTime, 1));
            debugTextSb.Append(" (");
            debugTextSb.Append(MathF.Round(deltaTime * 1000f, 1));
            debugTextSb.Append("ms)\n");

            debugTextSb.Append("TPS: ");
            debugTextSb.Append(gameManager.simulationManager.tps);
            debugTextSb.Append(" (");
            debugTextSb.Append(Math.Round(1.0f / gameManager.simulationManager.tps, 2));
            debugTextSb.Append("ms, ");
            if (gameManager.simulationManager.IdToIndex.IsCreated)
                debugTextSb.Append(gameManager.presentationManager.ActiveObservers.Count).Append("/").Append(gameManager.simulationManager.IdToIndex.Count).Append(" nodes)\n");
            else
                debugTextSb.Append("N/A nodes)\n");

            debugTextSb.Append("Tick: ");

            if (gameManager.simulationManager.IsSimulationPaused)
            {
                debugTextSb.Append("Paused | ");
            }
            debugTextSb.Append(gameManager.TimeManager.LastPacketTick.RemoteTick).Append("/").Append(playerManager.TimeManager.Tick);

            debugTextSb.Append("\nHP: ");
            debugTextSb.Append(playerManager.playerHealth.CurrentHealth);
            return debugTextSb.ToString();
        }
    }
}