using System;
using Tewi.Game.Factory.Core;

namespace Tewi.Game.Factory.Authoring
{
    [Serializable]
    public struct AuthoringResourceStack
    {
        public GlobalID id;
        public int amount;
        public override string ToString()
        {
            return $"{id.cachedFullID}x{amount}";
        }
    }
}