﻿using ECommon.Logging;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using System;
using System.IO;
using System.Linq;

namespace ECommon.Log4Net
{
    public class Log4NetLoggerFactory : ILoggerFactory
    {
        private readonly string loggerRepository;

        public Log4NetLoggerFactory(string configFile,string loggerRepository= "NetStandardRepository")
        {
            this.loggerRepository = loggerRepository;

            var file = new FileInfo(configFile);
            if (!file.Exists)
            {
                file = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFile));
            }
            var repositories = LogManager.GetAllRepositories();
            if (repositories != null && repositories.Any(x => x.Name == loggerRepository))
            {
                return;
            }

            var repository = LogManager.CreateRepository(loggerRepository);
            if (file.Exists)
            {
                XmlConfigurator.ConfigureAndWatch(repository, file);
            }
            else
            {
                BasicConfigurator.Configure(repository, new ConsoleAppender { Layout = new PatternLayout() });
            }
        }

        public ILogger Create(string name)
        {
            return new Log4NetLogger(LogManager.GetLogger(loggerRepository, name));
        }

        public ILogger Create(Type type)
        {
            return new Log4NetLogger(LogManager.GetLogger(loggerRepository, type));
        }
    }
}
