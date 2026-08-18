using System;
using Tewi.Factory.Core;

namespace Tewi.Factory.Authoring
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