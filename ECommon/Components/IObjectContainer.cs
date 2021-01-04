using System;

namespace ECommon.Components
{
    /// <summary>
    /// 表示对象容器接口
    /// </summary>
    public interface IObjectContainer
    {
        /// <summary>
        /// 构建容器
        /// </summary>
        void Build();
        /// <summary>
        /// 注册实现类型
        /// </summary>
        /// <param name="implementationType">实现类型</param>
        /// <param name="serviceName">服务名称</param>
        /// <param name="life">实现者类型的生命周期</param>
        void RegisterType(Type implementationType, string serviceName = null, LifeStyle life = LifeStyle.Singleton);
        /// <summary>
        /// 将实现者类型注册为服务实现
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <param name="implementationType">实现类型</param>
        /// <param name="serviceName">服务名称</param>
        /// <param name="life">实现者类型的生命周期</param>
        void RegisterType(Type serviceType, Type implementationType, string serviceName = null, LifeStyle life = LifeStyle.Singleton);
        /// <summary>
        /// 将实现者类型注册为服务实现
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <typeparam name="TImplementer">实现类型</typeparam>
        /// <param name="serviceName">服务名称</param>
        /// <param name="life">实现者类型的生命周期</param>
        void Register<TService, TImplementer>(string serviceName = null, LifeStyle life = LifeStyle.Singleton)
            where TService : class
            where TImplementer : class, TService;
        /// <summary>
        /// 将实现者类型实例注册为服务实现
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <typeparam name="TImplementer">实现类型</typeparam>
        /// <param name="instance">实现者类型实例</param>
        /// <param name="serviceName">服务名称</param>
        void RegisterInstance<TService, TImplementer>(TImplementer instance, string serviceName = null)
            where TService : class
            where TImplementer : class, TService;
        /// <summary>
        /// 解析服务
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <returns>提供服务的组件实例</returns>
        TService Resolve<TService>() where TService : class;
        /// <summary>
        /// 解析服务
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <returns>提供服务的组件实例</returns>
        object Resolve(Type serviceType);
        /// <summary>
        /// 尝试从容器中检索服务
        /// </summary>
        /// <typeparam name="TService">要解析的服务类型</typeparam>
        /// <param name="instance">提供服务的结果组件实例，或默认值（TService）</param>
        /// <returns>如果提供服务的组件可用，则为True</returns>
        bool TryResolve<TService>(out TService instance) where TService : class;
        /// <summary>
        /// 尝试从容器中检索服务
        /// </summary>
        /// <param name="serviceType">要解析的服务类型</param>
        /// <param name="instance">提供服务的结果组件实例，或为null</param>
        /// <returns>如果提供服务的组件可用，则为True</returns>
        bool TryResolve(Type serviceType, out object instance);
        /// <summary>
        /// 解析服务
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <param name="serviceName">服务名称</param>
        /// <returns>提供服务的组件实例</returns>
        TService ResolveNamed<TService>(string serviceName) where TService : class;
        /// <summary>
        /// 解析服务
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        /// <param name="serviceType">服务类型</param>
        /// <returns>提供服务的组件实例</returns>
        object ResolveNamed(string serviceName, Type serviceType);
        /// <summary>
        /// 尝试从容器中检索服务
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        /// <param name="serviceType">服务类型</param>
        /// <param name="instance">提供服务的结果组件实例，或为null</param>
        /// <returns>如果提供服务的组件可用，则为True</returns>
        bool TryResolveNamed(string serviceName, Type serviceType, out object instance);
    }

    public enum LifeStyle
    {
        Transient,//短暂
        Singleton//单例
    }
}
