namespace Tewi.Game.Factory.Core
{
    public interface INodeStatePushed
    {
        int nodeId { get; set; }
        void OnNodeStatePushed(in NodeState state);
    }
}
