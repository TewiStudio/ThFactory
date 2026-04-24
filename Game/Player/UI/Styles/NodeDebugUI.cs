using System.Text;
using Unity.Collections;
using UnityEngine;
using TMPro;
using PrimeTween;
using Tewi.Game.Factory.Core;
using Tewi.Game.Factory.Simulation;
using Tewi.Game.Factory.Utils;

namespace Tewi.Game.Player.UI.Styles
{
    internal class NodeDebugUI : UIBase<object>
    {
        public override bool IsModal => true;

        [SerializeField] private TMP_InputField nodeId;
        [SerializeField] private TMP_InputField slot;
        [SerializeField] private TMP_InputField resourceId;
        [SerializeField] private TMP_InputField resourceAmount;
        [SerializeField] private TextMeshProUGUI debugNodesText;

        private int debugNodeId;
        private SlotType debugSlot;
        private ushort debugResourceId;
        private ushort debugAmount;

        public void ExecuteAdd()
        {
            if (!gameManager.simulationManager) return;

            int.TryParse(nodeId.text, out debugNodeId);
            int.TryParse(slot.text, out int slotValue);
            debugSlot = (SlotType)slotValue;
            ushort.TryParse(resourceId.text, out debugResourceId);
            ushort.TryParse(resourceAmount.text, out debugAmount);
            
            gameManager.simulationManager.ChangeResource(debugNodeId, debugSlot, debugResourceId, debugAmount);
        }

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
            gameManager.presentationManager.NodeSimulationCompletedEvent += PresentationManager_NodeSimulationCompletedEvent;
        }

        internal override void OnClose()
        {
            SetVisibleAnimation(false);
            gameManager.presentationManager.NodeSimulationCompletedEvent -= PresentationManager_NodeSimulationCompletedEvent;
        }

        private StringBuilder _sb = new StringBuilder();
        private void PresentationManager_NodeSimulationCompletedEvent(in NativeArray<NodeState>.ReadOnly _nodes, in NativeHashMap<int, int>.ReadOnly idToIndex)
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

                if (i > 1000)
                {
                    _sb.Append("......");
                    break;
                }
            }
            debugNodesText.text = _sb.ToString();
        }
    }
}