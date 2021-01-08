using Autofac;
using ECommon.Components;
using ECommon.Configurations;

namespace ECommon.Autofac
{
    public static class ConfigurationExtensions
    {
        public static Configuration UseAutofac(this Configuration configuration)
        {
            return UseAutofac(configuration, new ContainerBuilder());
        }

        public static Configuration UseAutofac(this Configuration configuration,ContainerBuilder containerBuilder)
        {
            ObjectContainer.SetContainer(new AutofacObjectContainer(containerBuilder));
            return configuration;
        }
    }
}
