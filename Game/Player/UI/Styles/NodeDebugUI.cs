using System.Text;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;
using PrimeTween;
using Tewi.Game.Console;
using Tewi.Game.Factory.Core;
using Tewi.Game.Factory.Utils;

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

        private CommandProcessor _processor;
        private List<string> _commandHistory = new();

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
            InstanceFinder.RegisterInstance(this);

            commands = new StringBuilder();
            _processor = new();
            _processor.ScanCommands();
            LogInfo("Type 'help' for available commands.");
        }

        public void LogInfo(string message)
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

            LogInfo($"> <color=yellow>{inputStr}</color>");

            string feedback = _processor.Execute(inputStr);

            if (!string.IsNullOrEmpty(feedback))
                LogInfo(feedback);

            command.text = "";
            command.ActivateInputField();
        }
    }
}