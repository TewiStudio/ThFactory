/*using System.Text;
using Unity.Collections;
using UnityEngine;
using Tewi.Game.Factory.Core;
using Tewi.Game.Factory.Utils;
using Tewi.Game.Factory.Simulation;

namespace Tewi.Game.Factory.Presentation
{
    public class DebugGUI : MonoBehaviour
    {
        public SimulationManager simulationManager;

        private NativeArray<NodeState>.ReadOnly _nodes;

        private string _typeInputText = "type";
        private string _recipeInputText = "recipe";
        private string _input1Text = "input1";
        private string _input1amountText = "amount1";
        private string _input2Text = "input2";
        private string _input2amountText = "amount2";
        private string _countText = "count";
        private string _idRemoveInputText = "id";
        private void DrawButtons()
        {
            GUILayout.BeginHorizontal();
            _typeInputText = GUILayout.TextField(_typeInputText);
            _recipeInputText = GUILayout.TextField(_recipeInputText);
            _input1Text = GUILayout.TextField(_input1Text);
            _input1amountText = GUILayout.TextField(_input1amountText);
            _input2Text = GUILayout.TextField(_input2Text);
            _input2amountText = GUILayout.TextField(_input2amountText);
            _countText = GUILayout.TextField(_countText);
            if (GUILayout.Button("Add"))
            {
                if (ushort.TryParse(_typeInputText, out var typeValue) &&
                    ushort.TryParse(_recipeInputText, out var recipeValue) &&
                    ushort.TryParse(_input1Text, out var in1) &&
                    ushort.TryParse(_input1amountText, out var in1a) &&
                    ushort.TryParse(_input2Text, out var in2) &&
                    ushort.TryParse(_input2amountText, out var in2a) &&
                    int.TryParse(_countText, out var count))
                {
                    for (int i = 0; i < count; i++)
                        simulationManager.AddNode(typeValue, recipeValue, in1, in1a, in2, in2a);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            _idRemoveInputText = GUILayout.TextField(_idRemoveInputText);
            if (GUILayout.Button("Remove"))
            {
                if (int.TryParse(_idRemoveInputText, out var idValue))
                {
                    simulationManager.RemoveNode(idValue);
                }
            }
            if (GUILayout.Button("Remove All"))
            {
                foreach (var item in simulationManager.IdToIndex)
                {
                    simulationManager.RemoveNode(item.Key);
                }
            }
            GUILayout.EndHorizontal();
        }

        private StringBuilder _sb = new StringBuilder();
        private GUIStyle _debugStyle;
        void DrawDebugInfo()
        {
            _drawNodeStringCount += Time.unscaledDeltaTime;
            if (_drawNodeStringCount > .5f)
            {
                _drawNodeStringCount = 0;
                _sb.Clear();

                for (int i = 0; i < _nodes.Length; i++)
                {
                    var node = _nodes[i];
                    var recipe = simulationManager.networkGameManager.resourcesDatabase.recipeTable[node.recipeId];

                    _sb.Append("ID: ").Append(node.id)
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
            }

            if (_debugStyle == null)
            {
                _debugStyle = new GUIStyle(GUI.skin.label);
                _debugStyle.fontSize = 14;
                _debugStyle.fontStyle = FontStyle.Bold;
                _debugStyle.normal.textColor = Color.white;
            }
            GUILayout.BeginScrollView(Vector2.zero);
            GUILayout.Label(_sb.ToString(), _debugStyle);
            GUILayout.EndScrollView();
        }

        private string _checkResourceIDInputText = "id";
        private string _checkResourceText = "None";
        private string _checkRecipeIDInputText = "id";
        private string _checkRecipeText = "None";
        void DrawCheckResourceUI()
        {
            GUILayout.BeginHorizontal();
            _checkResourceIDInputText = GUILayout.TextField(_checkResourceIDInputText);
            if (GUILayout.Button("Check"))
            {
                if (ushort.TryParse(_checkResourceIDInputText, out var result))
                {
                    if (simulationManager.networkGameManager.resourcesDatabase.resourceTable[result] is var res)
                        _checkResourceText =
                            $"id: {res.id} | {res.id.GetResourceStringID()}\n" +
                            $"maxStack: {res.maxStack}\n" +
                            $"tags: {res.tags}";
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(_checkResourceText);

            GUILayout.BeginHorizontal();
            _checkRecipeIDInputText = GUILayout.TextField(_checkRecipeIDInputText);
            if (GUILayout.Button("Check"))
            {
                if (ushort.TryParse(_checkRecipeIDInputText, out var result))
                {
                    if (simulationManager.networkGameManager.resourcesDatabase.recipeTable[result] is var recipe)
                        _checkRecipeText =
                            $"id: {recipe.id} | {recipe.id.GetRecipeStringID()}\n" +
                            $"duration: {recipe.durationTicks}\n" +
                            $"in1: {recipe.in1.id} | {((int)recipe.in1.id).GetResourceStringID()} (x{recipe.in1.amount})\n" +
                            $"in2: {recipe.in2.id} | {((int)recipe.in2.id).GetResourceStringID()} (x{recipe.in2.amount})\n" +
                            $"out1: {recipe.out1.id} | {((int)recipe.out1.id).GetResourceStringID()} (x{recipe.out1.amount})\n" +
                            $"out2: {recipe.out2.id} | {((int)recipe.out2.id).GetResourceStringID()} (x{recipe.out2.amount})\n";
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(_checkRecipeText);
        }

        float _drawNodeStringCount = 0;
        private void OnGUI()
        {
            return;
            if (!_nodes.IsCreated) return;
            GUILayout.BeginArea(new Rect(300, 0, Screen.width - 300, Screen.height));
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            DrawButtons();
            DrawDebugInfo();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            DrawCheckResourceUI();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void Start()
        {
            //simulationManager.networkGameManager.presentationManager.NodeSimulationCompletedEvent += PresentationManager_NodeSimulationCompletedEvent;
        }

        private void OnDestroy()
        {
            //if (simulationManager)
            //    simulationManager.networkGameManager.presentationManager.NodeSimulationCompletedEvent -= PresentationManager_NodeSimulationCompletedEvent;
        }

        private void PresentationManager_NodeSimulationCompletedEvent(in NativeArray<NodeState>.ReadOnly nodes, in NativeHashMap<int, int>.ReadOnly idToIndex)
        {
            _nodes = nodes;
        }
    }
}
*/