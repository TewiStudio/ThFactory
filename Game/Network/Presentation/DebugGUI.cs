using System.Text;
using UnityEngine;
using Tewi.Game.Network.Utils;
using Tewi.Game.Network.Simulation;
using Tewi.Game.Network.Core;
using Unity.Collections;

namespace Tewi.Game.Network.Presentation
{
    public class DebugGUI : MonoBehaviour
    {
        public SimulationManager simulationManager;

        private NativeArray<NodeState> _nodes;

        private string _typeInputText = "type";
        private string _recipeInputText = "recipe";
        private string _idRemoveInputText = "id";
        private void DrawButtons()
        {
            GUILayout.BeginHorizontal();
            _typeInputText = GUILayout.TextField(_typeInputText);
            _recipeInputText = GUILayout.TextField(_recipeInputText);
            if (GUILayout.Button("Add"))
            {
                if (ushort.TryParse(_typeInputText, out var typeValue) && ushort.TryParse(_recipeInputText, out var recipeValue))
                {
                    simulationManager.AddNode(typeValue, recipeValue);
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
            GUILayout.EndHorizontal();
        }

        private StringBuilder _sb = new StringBuilder();
        private GUIStyle _debugStyle;
        void DrawDebugInfo()
        {
            _sb.Clear();

            for (int i = 0; i < _nodes.Length; i++)
            {
                var node = _nodes[i];

                _sb.Append("ID: ").Append(node.id).Append(" | internalIndex: ").Append(node.internalIndex)
                   .Append(" | status: ").Append(node.currentStatus)
                   .Append("\nType: ").Append(node.nodeType)
                   .Append(" | Recipe: ").Append(node.recipeId.GetRecipeStringID())
                   .Append("\nin1: ").Append(node.in1.id.GetResourceStringID()).Append(" *").Append(node.in1.amount)
                   .Append("\nin2: ").Append(node.in2.id.GetResourceStringID()).Append(" *").Append(node.in2.amount)
                   .Append("\nout1: ").Append(node.out1.id.GetResourceStringID()).Append(" *").Append(node.out1.amount)
                   .Append("\nout2: ").Append(node.out2.id.GetResourceStringID()).Append(" *").Append(node.out2.amount)
                   .Append("\n----------------\n");

                if (i > 20)
                {
                    _sb.Append("......");
                    break;
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
                    if (simulationManager.NetworkGameManagerInstance.resourcesDatabase.resourceTable[result] is var res)
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
                    if (simulationManager.NetworkGameManagerInstance.resourcesDatabase.recipeTable[result] is var recipe)
                        _checkRecipeText =
                            $"id: {recipe.id} | {recipe.id.GetRecipeStringID()}\n" +
                            $"duration: {recipe.durationTicks}\n" +
                            $"in1: {recipe.in1.id} | {recipe.in1.id.GetResourceStringID()} (x{recipe.in1.amount})\n" +
                            $"in2: {recipe.in2.id} | {recipe.in2.id.GetResourceStringID()} (x{recipe.in2.amount})\n" +
                            $"out1: {recipe.out1.id} | {recipe.out1.id.GetResourceStringID()} (x{recipe.out1.amount})\n" +
                            $"out2: {recipe.out2.id} | {recipe.out2.id.GetResourceStringID()} (x{recipe.out2.amount})\n";
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(_checkRecipeText);
        }

        private void OnGUI()
        {
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
            simulationManager.NetworkGameManagerInstance.presentationManager.NodeSimulationCompletedEvent += PresentationManager_NodeSimulationCompletedEvent;
        }

        private void OnDestroy()
        {
            _nodes.Dispose();
            if (simulationManager)
                simulationManager.NetworkGameManagerInstance.presentationManager.NodeSimulationCompletedEvent -= PresentationManager_NodeSimulationCompletedEvent;
        }

        private void PresentationManager_NodeSimulationCompletedEvent(NativeArray<NodeState> nodes)
        {
            _nodes = nodes;
        }
    }
}
