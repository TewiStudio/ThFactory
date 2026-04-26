using UnityEngine;
using TMPro;
using GameKit.Dependencies.Utilities;

namespace Tewi.Game.Player.UI.Styles
{
    public struct InteractUIContext
    {
        public string text;
    }

    internal class InteractUI : UIBase<InteractUIContext>
    {
        [Space(15)]
        [SerializeField] private TextMeshProUGUI InteractText;
        [SerializeField] private Shapes2D.Shape InteractTimeLeft;

        public override bool IsModal => false;

        private string interactLastText = null;

        private void Update()
        {
            InteractTimeLeftAnimation();
        }

        internal override void OnOpen(InteractUIContext context)
        {
            base.OnOpen(context);
            if (context.text is not null)
            {
                if (interactLastText != context.text)
                {
                    interactLastText = context.text;
                    InteractText.text = $"{context.text}({playerManager.interactKey})";
                }
            }
        }

        internal override void OnClose()
        {
            base.OnClose();
        }

        private void InteractTimeLeftAnimation()
        {
            if (playerManager.interactionController.interactKeyDown && playerManager.interactionController.nowInteractItemPlayerLooks is not null)
            {
                if (playerManager.interactionController.nowInteractItemPlayerLooks.InteractTime > 0)
                {
                    InteractTimeLeft.settings.endAngle = 360f *
                        (1f - playerManager.interactionController.holdInteractKeyTime /
                        playerManager.interactionController.nowInteractItemPlayerLooks.InteractTime);
                }
                else
                {
                    InteractTimeLeft.settings.endAngle = 359.9999f;
                }
            }
            else
            {
                InteractTimeLeft.settings.endAngle = 359.9999f;
            }
        }
    }
}