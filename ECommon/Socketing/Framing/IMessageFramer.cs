using System;
using System.Collections.Generic;

namespace ECommon.Socketing.Framing
{
    public interface IMessageFramer
    {
        //按照功能排序：List<T> 《IList<T> 《ICollection<T>《IEnumerable<T>
        //按照性能排序：IEnumerable<T>《ICollection<T>《IList<T>《List<T>

        //结构体ArraySegment<T> 表示数组的一段，如果需要使用不同的方式去处理一个大型数组的不同部分,一个有效的方法是使用部分数组来代替创建多个数组
        void UnFrameData(IEnumerable<ArraySegment<byte>> data);

        void UnFrameData(ArraySegment<byte> data);

        IEnumerable<ArraySegment<byte>> FrameData(ArraySegment<byte> data);

        void RegisterMessageArrivedCallback(Action<ArraySegment<byte>> handler);

    }
}
