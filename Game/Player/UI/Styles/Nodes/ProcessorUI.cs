using TMPro;
using UnityEngine;
using Tewi.Game.Factory.Authoring;
using Tewi.Game.Factory.Core;
using Tewi.Game.Factory.Utils;

namespace Tewi.Game.Player.UI.Styles
{
    public class ProcessorUI : NodeUI
    {
        [SerializeField] private TMP_InputField selectRecipeID;
        [SerializeField] private TextMeshProUGUI RecipeText;
        [SerializeField] private TextMeshProUGUI StateText;
        [SerializeField] private TextMeshProUGUI in1Text;
        [SerializeField] private TextMeshProUGUI in2Text;
        [SerializeField] private TextMeshProUGUI out1Text;
        [SerializeField] private TextMeshProUGUI out2Text;
        [SerializeField] private Shapes2D.Shape processImage;

        private int _lastRecipeID;
        private RecipeSO _recipeSO;
        private ushort _recipeDurationTicks;
        private float _processProgress;

        internal override void OnOpen(NodeUIContext context)
        {
            base.OnOpen(context);
            UpdateUIStates(in context.nodeState);
        }

        public override void OnNodeStatePushed(in NodeState state)
        {
            base.OnNodeStatePushed(in state);
            UpdateUIStates(in state);
        }

        private void UpdateUIStates(in NodeState state)
        {
            if (state.recipeId != _lastRecipeID)
            {
                ChangeRecipe(state.recipeId);
            }
            _lastRecipeID = state.recipeId;

            RecipeText.text = $"{nodeId}";
            if (_recipeSO is null)
            {
                StateText.text = "Idle";
                in1Text.text = "in1";
                in2Text.text = "in2";
                out1Text.text = "out1";
                out2Text.text = "out2";
                return;
            }

            
            StateText.text = $"{state.currentStatus} {_recipeSO.id.cachedFullID}";

            in1Text.text = $"in1: {_recipeSO.inputs[0].id.cachedFullID} x{state.in1.amount}";
            if (_recipeSO.inputs.Count > 1) in2Text.text = $"in2: {_recipeSO.inputs[1].id.cachedFullID} x{state.in2.amount}";
            else in2Text.text = "in2";

            out1Text.text = $"out1: {_recipeSO.outputs[0].id.cachedFullID} x{state.out1.amount}";
            if (_recipeSO.outputs.Count > 1) out2Text.text = $"out2: {_recipeSO.outputs[1].id.cachedFullID} x{state.out2.amount}";
            else out2Text.text = "out2";

            _processProgress = (float)state.progressTicks / _recipeDurationTicks;
        }

        public void SelectedRecipeID()
        {
            if (int.TryParse(selectRecipeID.text, out int result))
            {
                gameManager.simulationManager.ChangeRecipe(nodeId, result);
                ChangeRecipe(result);
            }
        }

        private void ChangeRecipe(int recipeID)
        {
            _recipeSO = recipeID.GetRecipe();
            _recipeDurationTicks = gameManager.resourcesDatabase.GetRecipeTotalTicks(recipeID);
        }

        private void Update()
        {
            float newAngle = Mathf.MoveTowardsAngle(processImage.settings.endAngle, 360f * (1f - _processProgress), 500f * Time.deltaTime);
            processImage.settings.endAngle = newAngle;
        }
    }
}
