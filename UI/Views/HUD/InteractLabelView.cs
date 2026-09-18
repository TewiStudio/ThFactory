using FishNet.Managing.Timing;
using System;
using System.Text;
using Tewi.Game.Network;
using Tewi.Game.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tewi.Game.UI.Views
{
    public struct InteractLabelViewContext
    {
        public string text;
    }

    public sealed class InteractLabelView : ViewUI<Label, InteractLabelViewContext>
    {
        public override string ElementName => "InteractLabel";
        private string interactLastText = null;

        public override void SetData(InteractLabelViewContext data)
        {
            if (data.text is not null)
            {
                if (interactLastText != data.text)
                {
                    interactLastText = data.text;
                    Element.text = $"{data.text}({PlayerManager.interactKey})";
                }
            }
        }
    }
}