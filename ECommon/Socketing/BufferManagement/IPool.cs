namespace ECommon.Socketing.BufferManagement
{
    public interface IPool
    {
        int TotalCount { get; }

        /// <summary>
        /// 获取可用计数，即可使用的项目计数
        /// </summary>
        int AvailableCount { get; }

        /// <summary>
        /// 缩小此池
        /// </summary>
        /// <returns></returns>
        bool Shrink();
    }

    public interface IPool<T> : IPool
    {
        T Get();

        void Return(T item);
    }
}
