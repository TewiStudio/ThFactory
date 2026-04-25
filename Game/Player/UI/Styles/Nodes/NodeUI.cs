using PrimeTween;
using Tewi.Game.Factory.Core;
using UnityEngine;

namespace Tewi.Game.Player.UI.Styles
{
    public struct NodeUIContext
    {
        public NodeState nodeState;
    }

    public class NodeUI : UIBase<NodeUIContext>, INodeStatePushed
    {
        public int nodeId { get; set; }

        internal override void OnOpen(NodeUIContext context)
        {
            //base.OnOpen(context);
            SetVisibleAnimation(true);
            if (context.nodeState.id != 0)
            {
                nodeId = context.nodeState.id;
                uiManager.playerManager.gameManager.presentationManager.Subscribe(this);
            }
        }

        internal override void OnClose()
        {
            //base.OnClose();
            SetVisibleAnimation(false);
            uiManager.playerManager.gameManager.presentationManager.Unsubscribe(this);
            nodeId = 0;
        }

        public virtual void OnNodeStatePushed(in NodeState state)
        {

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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                gameManager.nodeCoordinator.DestroyNode(nodeId);
                Close();
            }
        }
    }
}
