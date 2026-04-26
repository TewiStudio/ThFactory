using UnityEngine;
using Tewi.Game.Player;
using Tewi.Game.Player.UI.Styles;
using Tewi.Game.Network;
using Tewi.Game.Factory.Core;

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

        public void Init()
        {
            gameManager.presentationManager.Subscribe(this);
        }

        public void Deinit()
        {
            gameManager.presentationManager.Unsubscribe(this);
        }

        public void OnNodeStatePushed(in NodeState state)
        {
            lastState = state;
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
