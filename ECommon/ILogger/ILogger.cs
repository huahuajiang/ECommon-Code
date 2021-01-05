﻿using System;

namespace ECommon.ILogger
{
    public interface ILogger
    {
        bool IsDebugEnabled { get; }

        void Debug(string message);

        void DebugFormat(string format, params object[] args);

        void Debug(string message, Exception exception);

        void Info(string message);

        void InfoFormat(string format, params object[] args);

        void Error(string message);

        void ErrorFoemat(string format, params object[] args);

        void Error(string message, Exception exception);

        void Warn(string message);

        void WarnFormat(string format, params object[] args);

        void Warn(string message, Exception exception);

        void Fatal(string message);

        void FatalFormat(string format, params object[] args);

        void Fatal(string message, Exception exception);
    }
}
