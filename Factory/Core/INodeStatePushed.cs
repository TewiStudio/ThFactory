namespace Tewi.Factory.Core
{
    public interface INodeStatePushed
    {
        int NodeId { get; set; }
        void OnNodeStatePushed(in NodeState state);
        void OnSubscribe();
        void OnUnsubscribe();
    }
}
