using System;

namespace ECommon.ILogger
{
    public interface ILoggerFactory
    {
        ILogger Create(string name);

        ILogger Create(Type type);
    }
}
