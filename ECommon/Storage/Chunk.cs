using ECommon.Components;
using ECommon.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommon.Storage
{
    public unsafe class Chunk:IDisposable
    {
        #region Private Variables

        private static readonly ILogger _logger= ObjectContainer.Resolve<ILoggerFactory>().Create(typeof(Chunk));
        private ChunkHeader _chunkHeader;
        private ChunkFooter _chunkFooter;

        private readonly string _filename;
        private readonly ChunkManager _chunkManager;

    }
}
