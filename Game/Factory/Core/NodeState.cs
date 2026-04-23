using System.Runtime.InteropServices;

namespace Tewi.Game.Factory.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NodeState
    {
        public int id;
        public int internalIndex;
        public ushort nodeType;
        public ushort recipeId;
        public Status currentStatus;
        public ushort progressTicks;

        public ResourceStack in1, in2, in3, in4;
        public ResourceStack out1, out2, out3, out4;
    }
}
