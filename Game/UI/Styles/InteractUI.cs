using UnityEngine;
using TMPro;
using TLab.UI.SDF;
using Tewi.Game.Player;

namespace Tewi.Game.UI.Styles
{
    public struct InteractUIContext
    {
        public string text;
    }

    internal class InteractUI : UIBase<InteractUIContext>
    {
        [Space(15)]
        [SerializeField] private TextMeshProUGUI InteractText;
        [SerializeField] private SDFArc InteractTimeLeft;

        public override bool IsModal => false;
        public override bool DefaultActiveSwitchAnimation => false;

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
            if (!IsPlayerReady) return;
            InteractionController interactionController = playerManager.interactionController;
            if (interactionController.interactKeyDown && interactionController.nowInteractItemPlayerLooks is not null)
            {
                if (interactionController.nowInteractItemPlayerLooks.InteractTime > 0)
                {
                    InteractTimeLeft.fillAmount = interactionController.holdInteractKeyTime /
                        interactionController.nowInteractItemPlayerLooks.InteractTime;
                }
                else
                {
                    InteractTimeLeft.fillAmount = 0;
                }
            }
            else
            {
                InteractTimeLeft.fillAmount = 0;
            }
        }
    }
}