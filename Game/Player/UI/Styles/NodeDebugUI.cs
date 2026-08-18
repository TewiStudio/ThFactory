using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;

namespace Tewi.Game.Player.UI.Styles
{
    internal class NodeDebugUI : UIBase<object>
    {
        public ScrollRect scrollRect;
        public TextMeshProUGUI content;
        public TMP_InputField command;
        public StringBuilder commands;
        public int maxHistoryLines = 50;
        public override bool IsModal => true;
        public override KeyCode ModalHotKey => KeyCode.BackQuote;

        private List<string> _commandHistory = new();

        public override void SetActive(bool active, bool animation = true)
        {
            float duration = animation ? .35f : 0f;
            if (active)
            {
                gameObject.SetActive(true);
                PrimeTween.Tween.UIAnchoredPositionY(rectTransform, 0, duration, PrimeTween.Ease.OutCubic);
            }
            else
            {
                PrimeTween.Tween.UIAnchoredPositionY(rectTransform, rectTransform.rect.height, duration, PrimeTween.Ease.InCubic).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
            }
        }

        internal override void OnOpen(object context)
        {
            base.OnOpen(context);
            command.onSubmit.AddListener(HandleInput);
            command.ActivateInputField();
        }

        internal override void OnClose()
        {
            base.OnClose();
            command.onSubmit.RemoveListener(HandleInput);
        }

        protected override void Awake()
        {
            base.Awake();
            commands = new StringBuilder();

            Application.logMessageReceived += Application_logMessageReceived;

            Debug.Log("Type 'help' for available commands.");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Application.logMessageReceived -= Application_logMessageReceived;
        }

        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            string role;

            if (InstanceFinder.IsServerStarted && InstanceFinder.IsClientStarted)
                role = "HOST";
            else if (InstanceFinder.IsServerStarted)
                role = "SERVER";
            else if (InstanceFinder.IsClientStarted)
                role = "CLIENT";
            else
                role = "LOCAL";

            string logType;
            switch (type)
            {
                case LogType.Warning:
                    logType = $"<color=yellow>[Warning]</color>";
                    break;

                case LogType.Error:
                case LogType.Exception:
                    logType = $"<color=red>[Error]</color>";
                    break;

                case LogType.Assert:
                    logType = $"<color=orange>[Assert]</color>";
                    break;

                default:
                    logType = $"<color=white>[Info]</color>";
                    break;
            }

            string s;
            if (!gameManager || gameManager.SimulationManager is null)
                s = string.Empty;
            else
                s = $"[{gameManager.SimulationManager.CurrentTick}/{gameManager.TimeManager.Tick}]";

            string p = $"[{role}]{logType}{s} {condition}";
            DisplayLog(p);
        }

        private void DisplayLog(string message)
        {
            _commandHistory.Add(message);

            // 超过最大行数则移除旧的
            if (_commandHistory.Count > maxHistoryLines)
                _commandHistory.RemoveAt(0);

            // 重新构建显示文本
            commands.Clear();
            foreach (var line in _commandHistory)
            {
                commands.AppendLine(line);
            }

            content.text = commands.ToString();
            if (isActiveAndEnabled) StartCoroutine(ScrollBottom());
        }

        IEnumerator ScrollBottom()
        {
            yield return null;
            scrollRect.verticalNormalizedPosition = 0f;
        }

        float _lastInputTime = 0f;
        public void HandleInput(string inputStr)
        {
            if (Time.time - _lastInputTime < 0.2f)
            {
                command.ActivateInputField();
                return;
            }
            _lastInputTime = Time.time;

            Debug.Log($"> <color=yellow>{inputStr}</color>");

            string feedback = gameManager.CommandProcessor.Execute(inputStr);

            if (!string.IsNullOrEmpty(feedback))
                Debug.Log(feedback);

            command.text = "";
            command.ActivateInputField();
        }
    }
}