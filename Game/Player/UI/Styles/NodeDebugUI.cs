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

        /*
                public void ExecuteAdd()
                {
                    if (!gameManager.simulationManager) return;

                    int.TryParse(nodeId.text, out debugNodeId);

                    if (ushort.TryParse(in1ResId.text, out var in1Id) &&
                        ushort.TryParse(in1ResAmount.text, out var in1Amount))
                    {
                        gameManager.simulationManager.ChangeResource(debugNodeId, SlotType.In1, in1Id, in1Amount);
                    }

                    if (ushort.TryParse(in2ResId.text, out var in2Id) &&
                        ushort.TryParse(in2ResAmount.text, out var in2Amount))
                    {
                        gameManager.simulationManager.ChangeResource(debugNodeId, SlotType.In2, in2Id, in2Amount);
                    }

                    if (ushort.TryParse(out1ResId.text, out var out1Id) &&
                        ushort.TryParse(out1ResAmount.text, out var out1Amount))
                    {
                        gameManager.simulationManager.ChangeResource(debugNodeId, SlotType.Out1, out1Id, out1Amount);
                    }

                    if (ushort.TryParse(out2ResId.text, out var out2Id) &&
                        ushort.TryParse(out2ResAmount.text, out var out2Amount))
                    {
                        gameManager.simulationManager.ChangeResource(debugNodeId, SlotType.Out2, out2Id, out2Amount);
                    }
                }
        */

        public void ExecuteRemoveAll()
        {
            if (!gameManager.nodeCoordinator) return;
            gameManager.nodeCoordinator.RemoveAll();
        }

        private StringBuilder _sb = new StringBuilder();
        private string GetNodeStatesText(in NativeArray<NodeState>.ReadOnly _nodes, in NativeHashMap<int, int>.ReadOnly idToIndex)
        {
            _sb.Clear();
            for (int i = 0; i < _nodes.Length; i++)
            {
                var node = _nodes[i];
                var recipe = gameManager.resourcesDatabase.recipeTable[node.recipeId];

                _sb.Append("ID: ").Append(node.id).Append(" | internalIndex: ").Append(node.internalIndex)
                   .Append(" | status: ").Append(node.currentStatus)
                   .Append("\nRecipe: ").Append(node.recipeId.GetRecipeStringID())
                   .Append("\nprogress: ").Append((float)node.progressTicks / recipe.durationTicks)
                   .Append("\nprogressTicks: ").Append(node.progressTicks)
                   .Append(" | duraingTicks: ").Append(recipe.durationTicks)
                   .Append("\nin1: ").Append(((int)node.in1.id).GetResourceStringID()).Append(" *").Append(node.in1.amount)
                   .Append("\nin2: ").Append(((int)node.in2.id).GetResourceStringID()).Append(" *").Append(node.in2.amount)
                   .Append("\nout1: ").Append(((int)node.out1.id).GetResourceStringID()).Append(" *").Append(node.out1.amount)
                   .Append("\nout2: ").Append(((int)node.out2.id).GetResourceStringID()).Append(" *").Append(node.out2.amount)
                   .Append("\n----------------\n");

                if (i > 10)
                {
                    _sb.Append("......");
                    break;
                }
            }
            return _sb.ToString();
        }
    }
}