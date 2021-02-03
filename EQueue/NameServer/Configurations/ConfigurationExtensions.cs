using EQueue.Broker.DeleteMessageStrategies;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace EQueue.NameServer.Configurations
{
    public static class ConfigurationExtensions
    {
        public static Configuration RegisterEQueueComponents(this Configuration configuration)
        {
            configuration.SetDefault< IDeleteMessageStrategy
        }
    }
}
