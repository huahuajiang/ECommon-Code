namespace ECommon.Socketing.BufferManagement
{
    public class BufferPool : IntelliPool<byte[]>, IBufferPool
    {
        public int BufferSize { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufferSize">大小</param>
        /// <param name="initialCount">数量</param>
        public BufferPool(int bufferSize, int initialCount)
            : base(initialCount, new BufferItemCreator(bufferSize))
        {
            BufferSize = bufferSize;
        }
    }
}
