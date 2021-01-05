using System;

namespace ECommon.Logging
{
    public interface ILoggerFactory
    {
        ILogger Create(string name);

        ILogger Create(Type type);
    }
}
