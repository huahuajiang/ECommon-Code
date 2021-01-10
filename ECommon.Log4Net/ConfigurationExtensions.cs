using ECommon.Configurations;
using ECommon.Logging;

namespace ECommon.Log4Net
{
    public static class ConfigurationExtensions
    {
        public static Configuration UseLog4Net(this Configuration configuration)
        {
            return UseLog4Net(configuration, "log4net.config");
        }

        public static Configuration UseLog4Net(this Configuration configuration, string configFile, string loggerRepository = "NetStandardRepository")
        {
            configuration.SetDefault<ILoggerFactory, Log4NetLoggerFactory>(new Log4NetLoggerFactory(configFile, loggerRepository));
            return configuration;
        }
    }
}
