namespace Tewi.Game.Network.Core
{
    public struct RecipeData
    {
        public int id;
        public ushort durationTicks;

        public ResourceStack in1, in2, in3, in4, in5, in6;
        public ResourceStack out1, out2, out3, out4, out5, out6;
    }
}
