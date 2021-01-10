using System.Collections.Generic;

namespace ECommon.Socketing.BufferManagement
{
    public interface IPoolItemCreator<T>
    {
        IEnumerable<T> Create(int count);
    }
}
