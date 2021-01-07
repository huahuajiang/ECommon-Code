using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ECommon.Extensions
{
    public static class TaskFactoryExtensions
    {
        public static Task StartDelayedTask(this TaskFactory factory, int millisecondsDelay, Action action)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            if (millisecondsDelay < 0) throw new ArgumentOutOfRangeException("millisecondsDelay");
            if (action == null) throw new ArgumentNullException("action");

            //获取此任务工厂的默认取消令牌
            //IsCancellationRequested:获取是否已为此令牌请求取消
            if (factory.CancellationToken.IsCancellationRequested)
            {
                return new Task(() => { }, factory.CancellationToken);
            }

            //TaskCompletionSource<T>这是一种受你控制创建Task的方式。你可以使Task在任何你想要的时候完成，你也可以在任何地方给它一个异常让它失败。
            var tcs = new TaskCompletionSource<object>(factory.CreationOptions);
            //之所以会用到default关键字，是因为需要在不知道类型参数为值类型还是引用类型的情况下，为对象实例赋初
            var ctr = default(CancellationTokenRegistration);

            //创建计时器，但不要启动它。如果我们现在启动它，它可能会在ctr设置到正确的注册之前触发
            var timer = new Timer(self =>
            {
                //清除取消令牌和计时器，并尝试转换到“已完成”
                ctr.Dispose();
                ((Timer)self).Dispose();
                //成功
                tcs.TrySetResult(null);
            });

            //CanBeCanceled:获取此令牌是否能够处于取消状态
            if (factory.CancellationToken.CanBeCanceled)
            {
                //当取消发生时，请取消计时器并尝试转换到已取消。可能会有比赛，但这是良性的
                 ctr = factory.CancellationToken.Register(() =>
                {
                    timer.Dispose();
                    //失败
                    tcs.TrySetCanceled();
                });
            }

            try { timer.Change(millisecondsDelay, Timeout.Infinite); }
            catch (ObjectDisposedException) { }

            return tcs.Task.ContinueWith(_ => action(), factory.CancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, factory.Scheduler ?? TaskScheduler.Current);
        }
    }
}
