using UnityEngine;
using Tewi.Game.Player;
using Tewi.Game.UI.Styles;
using Tewi.Game.Network;
using Tewi.Factory.Core;

namespace Tewi.Game.Interactable.Nodes
{
    public class Node : MonoBehaviour, INodeStatePushed, IInteractable
    {
        public int nodeId { get; set; }
        public string ItemName => nodeName;
        public float InteractTime => 0f; // 机器通常瞬间打开 UI
        public bool IsInteractable => true;
        public string DefaultPlayerLookText => interactionText;

        public NetworkGameManager gameManager;
        public int currentRecipeID = 0;
        protected NodeState lastState;

        public string nodeName = "Precessor";
        public string interactionText = "Check";

        public void OnNodeStatePushed(in NodeState state)
        {
            lastState = state;
        }

        public void OnSubscribe()
        {
            // 订阅时可以做一些初始化操作
            //Debug.Log($"Node {nodeId} subscribed to PresentationManager.");
        }

        public void OnUnsubscribe()
        {
            // 取消订阅时可以做一些清理操作
            nodeId = 0;
            //Debug.Log($"Node {nodeId} unsubscribed from PresentationManager.");
        }

        public void OnInteract(PlayerManager player)
        {
            player.uiManager.Open<ProcessorUI, NodeUIContext>(new() { nodeState = lastState });
        }

        public virtual void OnPlayerLookAt(PlayerManager player) => this.HandleLookAt(player);

        public virtual void OnPlayerNotLooking(PlayerManager player) => this.HandleNotLooking(player);

        public virtual void UpdatePlayerShowsTextUI(PlayerManager player) => this.HandleUpdatePlayerShowsTextUI(player);
    }
}
