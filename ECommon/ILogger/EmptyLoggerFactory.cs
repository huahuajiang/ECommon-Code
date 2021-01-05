using System;

namespace ECommon.ILogger
{
    public class EmptyLoggerFactory : ILoggerFactory
    {
        private static readonly EmptyLogger Logger = new EmptyLogger();

        public ILogger Create(string name)
        {
            return Logger;
        }

        public ILogger Create(Type type)
        {
            return Logger;
        }
    }
}
