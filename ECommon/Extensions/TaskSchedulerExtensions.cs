using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ECommon.Extensions
{
    public static Task TaskSchedulerExtensions(this TaskFactory factory,int millisecondsDelay,Action action)
    {
        if (factory == null) throw new ArgumentNullException("factory");
        if (millisecondsDelay < 0) throw new ArgumentOutOfRangeException("millisecondsDelay");
        if (action == null) throw new ArgumentNullException("action");

        //获取此任务工厂的默认取消令牌
        if (factory.CancellationToken.IsCancellationRequested)
        {
            return new Task(() => { }, factory.CancellationToken);
        }
    }
}
