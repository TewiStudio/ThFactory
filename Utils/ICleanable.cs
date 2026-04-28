namespace Tewi.Helpers
{
    public interface ICleanable
    {
        // 定义优先级，数字越小越先清理
        int Priority { get; }
        void CleanUp();
    }
}
