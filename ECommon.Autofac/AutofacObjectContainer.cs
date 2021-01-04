using Autofac;
using ECommon.Components;
using System;

namespace ECommon.Autofac
{
    /// <summary>
    /// IObjectContainer的Autofac实现
    /// </summary>
    public class AutofacObjectContainer : IObjectContainer
    {
        public ContainerBuilder ContainerBuilder { get; }

        public IContainer Container { get; set; }

        public AutofacObjectContainer():this(new ContainerBuilder())
        {

        }

        public AutofacObjectContainer(ContainerBuilder containerBuilder)
        {
            ContainerBuilder = containerBuilder;
        }

        public AutofacObjectContainer(IContainer container)
        {
            Container = container;
        }

        public void Build()
        {
            Container = ContainerBuilder.Build();
        }

        public void RegisterType(Type implementationType, string serviceName = null, LifeStyle life = LifeStyle.Singleton)
        {
            if (implementationType.IsGenericType)
            {
               
            }
        }

        public void RegisterType(Type serviceType, Type implementationType, string serviceName = null, LifeStyle life = LifeStyle.Singleton)
        {
            throw new NotImplementedException();
        }

        public TService Resolve<TService>() where TService : class
        {
            throw new NotImplementedException();
        }

        public object Resolve(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public TService ResolveNamed<TService>(string serviceName) where TService : class
        {
            throw new NotImplementedException();
        }

        public object ResolveNamed(string serviceName, Type serviceType)
        {
            throw new NotImplementedException();
        }

        public bool TryResolve<TService>(out TService instance) where TService : class
        {
            throw new NotImplementedException();
        }

        public bool TryResolve(Type serviceType, out object instance)
        {
            throw new NotImplementedException();
        }

        public bool TryResolveNamed(string serviceName, Type serviceType, out object instance)
        {
            throw new NotImplementedException();
        }

        void IObjectContainer.Register<TService, TImplementer>(string serviceName, LifeStyle life)
        {
            throw new NotImplementedException();
        }

        void IObjectContainer.RegisterInstance<TService, TImplementer>(TImplementer instance, string serviceName)
        {
            throw new NotImplementedException();
        }
    }
}
