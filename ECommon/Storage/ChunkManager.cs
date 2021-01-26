using ECommon.Components;
using ECommon.Logging;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ECommon.Storage
{
    public class ChunkManager:IDisposable
    {
        private static readonly ILogger _logger= ObjectContainer.Resolve<ILoggerFactory>().Create(typeof(ChunkManager));
        private readonly object _lockObj = new object();
        private readonly ChunkManagerConfig _config;
        private readonly IDictionary<int, Chunk> _chunks;

    }
}