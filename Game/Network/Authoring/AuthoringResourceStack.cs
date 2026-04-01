using System;
using Tewi.Game.Network.Core;

namespace Tewi.Game.Network.Authoring
{
    [Serializable]
    public struct AuthoringResourceStack
    {
        public GlobalID id;
        public int amount;
    }
}