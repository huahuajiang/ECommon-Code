using Autofac;
using ECommon.Components;
using System;

namespace ECommon.Autofac
{
    //IServiceProvider:定义用于检索服务对象的机制；即，为其他对象提供自定义支持的对象
    //IDisposable:提供释放非托管资源的机制
    public class AutofacServiceProvider : IServiceProvider, IDisposable
    {
        private bool _disposed = false;//表示是否已经被回收
        public IContainer Container { get; }

        public AutofacServiceProvider(IContainer container,bool autoSetObjectContainer = true)
        {
            Container = container;
            if(autoSetObjectContainer&& ObjectContainer.Current is AutofacObjectContainer autofacObjectContainer)
            {
                autofacObjectContainer.Container = container;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    Container.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            //告诉CLR不要再触发我的析构函数
            GC.SuppressFinalize(this);
        }

        public object GetService(Type serviceType)
        {
            return Container.Resolve(serviceType);
        }
    }
}
