using TMPro;
using System;
using UnityEngine;

namespace Tewi.Game.Player.UI
{
    public class UIManager : MonoBehaviour
    {
        public PlayerManager PlayerManager;
        public Canvas UIRoot;
        public RectTransform InteractTransform;
        public PauseManager pauseManager;
        public TextMeshProUGUI InteractText;
        public TextMeshProUGUI debugText;
        public Shapes2D.Shape InteractTimeLeft;
        public float scaler = 1f;

        private System.Text.StringBuilder debugTextSb = new(32);
        string GetDebugText(float deltaTime)
        {
            /*
            if (debugText)
            {
                var line1 = $"Frame Rate: {fps}\n";
                var line2 = $"HP: {playerHealth.CurrentHealth} Position: {character.position}\n";
                var line3 = $"Now Velocity: {Math.Round(characterMovement.velocity.magnitude, 2)} {characterMovement.velocity}\n";
                var line4 = $"Landed Velocity: {Mathf.RoundToInt(characterMovement.landedVelocity.magnitude)} {characterMovement.landedVelocity}\n";
                var line5 = "";//gameManager ? $"Time: {(gameManager.nowWorld ? $"{gameManager.nowWorld.worldTimeManager.CurrentTimeSpan}\n" : "null\n")}" : "";
                var line6 = characterMovement._parentPlatform ? $"Ground Parent: {characterMovement._parentPlatform}" : "Ground Parent: null";

                debugText.text = $"{line1}{line2}{line3}{line4}{line5}{line6}";
                //debugText.text = string.Format(debugTexts, fps, playerHittable.HealthPoint);
            }
*/
            debugTextSb.Clear();
            debugTextSb.Append("FPS: ");
            debugTextSb.Append(MathF.Round(1.0f / deltaTime, 1));
            debugTextSb.Append(" (");
            debugTextSb.Append(MathF.Round(deltaTime * 1000f, 1));
            debugTextSb.Append("ms)\n");

            debugTextSb.Append("TPS: ");
            debugTextSb.Append(PlayerManager.gameManager.simulationManager.tps);
            debugTextSb.Append(" (");
            debugTextSb.Append(Math.Round(1.0f / PlayerManager.gameManager.simulationManager.tps, 2));
            debugTextSb.Append("ms, ");
            debugTextSb.Append(PlayerManager.gameManager.simulationManager.IdToIndex.Count).Append(" nodes)\n");

            debugTextSb.Append("HP: ");
            debugTextSb.Append(PlayerManager.playerHealth.CurrentHealth);
            return debugTextSb.ToString();
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            if (PlayerManager.interactKeyDown && PlayerManager.nowInteractItemPlayerLooks)
            {
                if (PlayerManager.nowInteractItemPlayerLooks.interactTime > 0)
                {
                    InteractTimeLeft.settings.endAngle = 360f * (1f - PlayerManager.holdInteractKeyTime / PlayerManager.nowInteractItemPlayerLooks.interactTime);
                }
                else
                {
                    InteractTimeLeft.settings.endAngle = 359.9999f;
                }
            }
            else
            {
                InteractTimeLeft.settings.endAngle = 359.9999f;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                pauseManager.SetPause(!PlayerManager.isPaused);
            }
        }

        private float _debugIntervalTime = 0;
        private float _deltaTime = 0f;
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

        private void FixedUpdate()
        {
        }

        private string interactLastText = null;
        public void SetInteractActive(bool active, string text = null)
        {
            InteractTransform.gameObject.SetActive(active);
            if (text is not null)
            {
                if (interactLastText != text)
                {
                    interactLastText = text;
                    InteractText.text = $"{text}({PlayerManager.interactKey})";
                    //Debug.Log($"{transform} interact text changes: {text}");
                }
            }
        }
    }
}