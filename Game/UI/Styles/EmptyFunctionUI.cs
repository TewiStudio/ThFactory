using PrimeTween;
using System;
using Tewi.Game.Network;
using UnityEngine;

namespace Tewi.Game.UI.Styles
{
    public class EmptyFunctionUI : UIBase<object>
    {
        public bool isModal = true;
        public bool animated = true;

        public override bool IsModal => IsModal;
        public override bool DefaultActiveSwitchAnimation => animated;
    }
}
