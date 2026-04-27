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

        internal void SetVisibleAnimation(bool isVisible, bool animate = true)
        {
            float duration = animate ? (isVisible ? .25f : .15f) : 0f;

            if (isVisible)
            {
                canvasGroup.interactable = true;
                SetActive(true);

                transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
                Sequence.Create()
                    .Group(Tween.Scale(transform, Vector3.one, duration))
                    .Group(Tween.Custom(0f, 1f, duration, newVal => canvasGroup.alpha = newVal));
            }
            else
            {
                canvasGroup.interactable = false;
                transform.localScale = Vector3.one;
                Sequence.Create()
                    .Group(Tween.Scale(transform, new Vector3(1.15f, 1.15f, 1.15f), duration))
                    .Group(Tween.Custom(1f, 0f, duration, newVal => canvasGroup.alpha = newVal)).OnComplete(() =>
                    {
                        canvasGroup.interactable = false;
                        SetActive(false);
                    });
            }
        }

        internal override void OnOpen(object context)
        {
            SetVisibleAnimation(true);
            command.onSubmit.AddListener(HandleInput);
            command.ActivateInputField();
        }

        internal override void OnClose()
        {
            SetVisibleAnimation(false);
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