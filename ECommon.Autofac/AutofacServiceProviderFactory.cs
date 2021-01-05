using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ECommon.Autofac
{
    //IServiceProviderFactory:提供一个扩展点，用于创建特定于容器的生成器和系统.IServiceProvider
    public class AutofacServiceProviderFactory : IServiceProviderFactory<ContainerBuilder>
    {
        private readonly bool _autoSetObjectContainer;

        public AutofacServiceProviderFactory(bool autoSetObjectContainer = true)
        {
            _autoSetObjectContainer = autoSetObjectContainer;
        }

        public ContainerBuilder CreateBuilder(IServiceCollection services)
        {
            var builder = new ContainerBuilder();
            builder.Populate(services);
            return builder;
        }

        public IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            return new AutofacServiceProvider(containerBuilder.Build(), _autoSetObjectContainer);
        }
    }
}
