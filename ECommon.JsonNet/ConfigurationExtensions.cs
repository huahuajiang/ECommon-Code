using ECommon.Configurations;
using ECommon.Serializing;

namespace ECommon.JsonNet
{
    public static class ConfigurationExtensions
    {
        public static Configuration UseJsonNet(this Configuration configuration)
        {
            configuration.SetDefault<IJsonSerializer, NewtonsoftJsonSerializer>(new NewtonsoftJsonSerializer());
            return configuration;
        }
    }
}
