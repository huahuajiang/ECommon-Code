﻿using ECommon.Configurations;
using ECommon.Logging;
using Serilog;
using Serilog.Events;

namespace ECommon.Serilog
{
    public static class ConfigurationExtensions
    {
        public static Configuration UseSerilog(this Configuration configuration,
            string defaultLoggerName = "default",
            string defaultLoggerFileName = "default",
            string defaultLoggerFileExtensions = "-.log",
            string contextPropertyName = "logger",
            string defaultLoggerConsoleOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] - {Message:lj}{NewLine}{Exception}",
            string defaultLoggerFileOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{logger}] - {Message:lj}{NewLine}{Exception}",
            bool defaultLoggerFileBuffered = true,
            long? defaultLoggerFileSizeLimitBytes = null,
            RollingInterval defaultLoggerFileRollingInterval = RollingInterval.Day,
            bool defaultLoggerFileRollOnFileSizeLimit = true,
            int? defaultLoggerFileRetainedFileCountLimit = null,
            int? defaultLoggerFileFlushToDiskIntervalSenconds = 1,
            LogEventLevel consoleMinimumLevel = LogEventLevel.Information,
            LogEventLevel fileMinimumLevel = LogEventLevel.Information)
        {
            return UseSerilog(configuration, new SerilogLoggerFactory(
                defaultLoggerName,
                defaultLoggerFileName,
                defaultLoggerFileExtensions,
                contextPropertyName,
                defaultLoggerConsoleOutputTemplate,
                defaultLoggerFileOutputTemplate,
                defaultLoggerFileBuffered,
                defaultLoggerFileSizeLimitBytes,
                defaultLoggerFileRollingInterval,
                defaultLoggerFileRollOnFileSizeLimit,
                defaultLoggerFileRetainedFileCountLimit,
                defaultLoggerFileFlushToDiskIntervalSenconds,
                consoleMinimumLevel,
                fileMinimumLevel));
        }

        public static Configuration UseSerilog(this Configuration configuration, SerilogLoggerFactory loggerFactory)
        {
            configuration.SetDefault<ILoggerFactory, SerilogLoggerFactory>(loggerFactory);
            return configuration;
        }
    }
}
