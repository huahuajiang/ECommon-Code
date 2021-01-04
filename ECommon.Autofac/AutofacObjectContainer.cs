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
            //IsGenericType获取一个值，该值指示当前类型是否为泛型类型
            if (implementationType.IsGenericType)
            {
                //泛型注册,可以通过容器返回List<T> 如:List<string>,List<int>等等
                var registrationBuilder = ContainerBuilder.RegisterGeneric(implementationType);
                if (serviceName != null)
                {
                    //提供可用于检索组件的文本名称
                    registrationBuilder.Named(serviceName, implementationType);
                }
                if (life == LifeStyle.Singleton)
                {
                    //配置组件，使每个依赖组件或对Resolve（）的调用获得相同的共享实例
                    registrationBuilder.SingleInstance();
                }
            }
            else
            {
                var registrationBuilder = ContainerBuilder.RegisterType(implementationType);
                if (serviceName != null)
                {
                    registrationBuilder.Named(serviceName, implementationType);
                }
                if (life == LifeStyle.Singleton)
                {
                    registrationBuilder.SingleInstance();
                }
            }
        }

        public void RegisterType(Type serviceType, Type implementationType, string serviceName = null, LifeStyle life = LifeStyle.Singleton)
        {
            if (implementationType.IsGenericType)
            {
                var registrationBuilder = ContainerBuilder.RegisterGeneric(implementationType).As(serviceType);
                if (serviceName != null)
                {
                    registrationBuilder.Named(serviceName, implementationType);
                }
                if (life == LifeStyle.Singleton)
                {
                    registrationBuilder.SingleInstance();
                }
            }
            else
            {
                var registrationBuilder = ContainerBuilder.RegisterType(implementationType).As(serviceType);
                if (serviceName != null)
                {
                    registrationBuilder.Named(serviceName, serviceType);
                }
                if (life == LifeStyle.Singleton)
                {
                    registrationBuilder.SingleInstance();
                }
            }
        }

        public TService Resolve<TService>() where TService : class
        {
            return Container.Resolve<TService>();
        }

        public object Resolve(Type serviceType)
        {
            return Container.Resolve(serviceType);
        }

        public TService ResolveNamed<TService>(string serviceName) where TService : class
        {
            return Container.ResolveNamed<TService>(serviceName);
        }

        public object ResolveNamed(string serviceName, Type serviceType)
        {
            return Container.ResolveNamed(serviceName, serviceType);
        }

        public bool TryResolve<TService>(out TService instance) where TService : class
        {
            return Container.TryResolve(out instance);
        }

        public bool TryResolve(Type serviceType, out object instance)
        {
            return Container.TryResolve(serviceType, out instance);
        }

        public bool TryResolveNamed(string serviceName, Type serviceType, out object instance)
        {
            return Container.TryResolveNamed(serviceName, serviceType, out instance);
        }

        public void Register<TService, TImplementer>(string serviceName, LifeStyle life = LifeStyle.Singleton)
            where TService : class
            where TImplementer : class, TService
        {
            var registrationBuilder = ContainerBuilder.RegisterType<TImplementer>().As<TService>();
            if (serviceName != null)
            {
                registrationBuilder.Named<TService>(serviceName);
            }
            if (life == LifeStyle.Singleton)
            {
                registrationBuilder.SingleInstance();
            }
        }

        public void RegisterInstance<TService, TImplementer>(TImplementer instance, string serviceName = null)
            where TService : class
            where TImplementer : class, TService
        {
            var registrationBuilder = ContainerBuilder.RegisterInstance(instance).As<TService>().SingleInstance();
            if (serviceName != null)
            {
                registrationBuilder.Named<TService>(serviceName);
            }
        }
    }
}
