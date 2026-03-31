using FishNet;
using FishNet.Object;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tewi.Game.Network.Server.Node;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Tewi.Game.Network.Server
{
    public class SimulationManager : NetworkBehaviour
    {
        public NetworkGameManager networkGameManager => InstanceFinder.GetInstance<NetworkGameManager>();
        private NativeList<NodeState> _nodes;
        private NativeHashMap<int, int> _idToIndex;
        private int _nextId = 1;

        #region Debug
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
                    AddNode(typeValue, recipeValue);
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            _idRemoveInputText = GUILayout.TextField(_idRemoveInputText);
            if (GUILayout.Button("Remove"))
            {
                if (int.TryParse(_idRemoveInputText, out var idValue))
                {
                    RemoveNode(idValue);
                }
            }
            GUILayout.EndHorizontal();
        }

        private StringBuilder _sb = new StringBuilder();
        private GUIStyle _debugStyle;
        void DrawDebugInfo()
        {
            _sb.Clear(); // 清空上次的内容，复用内存

            // 使用普通的 for 循环遍历 NativeList 性能最好
            for (int i = 0; i < _nodes.Length; i++)
            {
                // 拿到引用（避免拷贝）
                var node = _nodes[i];

                _sb.Append("ID: ").Append(node.id)
                   .Append(" | Index: ").Append(node.internalIndex)
                   .Append("\nType: ").Append(node.nodeType)
                   .Append(" | Recipe: ").Append(node.recipeId)
                   .Append("\n----------------\n");

                // 如果节点太多，只显示前 20 个，防止 UI 撑爆屏幕
                if (i > 20)
                {
                    _sb.Append("... 更多节点已隐藏 ...");
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
                    if (networkGameManager.resourcesDatabase.resourceTable[result] is var res)
                        _checkResourceText =
                            $"id: {res.id}\n" +
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
                    if (networkGameManager.resourcesDatabase.recipeTable[result] is var recipe)
                        _checkRecipeText =
                            $"id: {recipe.id}\n" +
                            $"duration: {recipe.duration}\n" +
                            $"input1: {recipe.input1.id} x{recipe.input1.amount}\n" +
                            $"input2: {recipe.input2.id} x{recipe.input2.amount}\n" +
                            $"output1: {recipe.output1.id} x{recipe.output1.amount}\n" +
                            $"output2: {recipe.output2.id} x{recipe.output2.amount}\n";
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(_checkRecipeText);
        }

        private void OnGUI()
        {
            if (!_nodes.IsCreated || !_idToIndex.IsCreated) return;
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
        #endregion

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _nodes = new(1000, Allocator.Persistent);
            _idToIndex = new(1000, Allocator.Persistent);
            Debug.Log("Created nodes list.");

            AddNode(1, 0);
            AddNode(2, 1);
            AddNode(3, 2);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _nextId = 1;
            if (_nodes.IsCreated) _nodes.Dispose();
            if (_idToIndex.IsCreated) _idToIndex.Dispose();
            Debug.Log("Disposed nodes list.");
        }

        public int AddNode(ushort nodeType, ushort recipeId)
        {
            int id = _nextId++;
            int index = _nodes.Length;

            NodeState nodeState = new()
            {
                id = id,
                internalIndex = index,
                nodeType = nodeType,
                recipeId = recipeId,
                currentStatus = Status.Idle
            };

            _nodes.Add(nodeState);
            _idToIndex.Add(id, index); // 建立映射关系

            return id;
        }

        public void RemoveNode(int id)
        {
            // 通过映射表找到它当前的物理索引
            if (!_idToIndex.TryGetValue(id, out int targetIndex)) return;

            int lastIndex = _nodes.Length - 1;

            // 如果是最后一个，直接删
            if (targetIndex == lastIndex)
            {
                _idToIndex.Remove(id);
                _nodes.RemoveAtSwapBack(targetIndex);
                return;
            }

            // SwapBack 逻辑
            NodeState lastNode = _nodes[lastIndex];

            // 更新搬家节点的内部索引
            lastNode.internalIndex = targetIndex;
            _nodes[targetIndex] = lastNode;

            // 更新映射表，让搬家的机器 ID 重新指向它的新物理位置
            _idToIndex[lastNode.id] = targetIndex;

            // 清理被删除机器的信息
            _idToIndex.Remove(id);
            _nodes.RemoveAtSwapBack(lastIndex);
        }

        private unsafe void Tick()
        {
            if (!_nodes.IsCreated) return;
            NodeState* ptr = _nodes.GetUnsafePtr();
            for (int i = 0; i < _nodes.Length; i++)
            {
                SimulateNode(ref ptr[i]);
            }

        }

        private void SimulateNode(ref NodeState nodeState)
        {
            switch (nodeState.internalIndex)
            {
                case 1:
                    break;
                case 2:
                    break;
                default:
                    break;
            }
        }

        private void FixedUpdate()
        {
            Tick();
        }
    }
}
