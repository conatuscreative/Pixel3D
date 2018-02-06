using System;

namespace Pixel3D
{
    public interface ILogger
    {
        void Trace(string message, params object[] args);
        void Info(string message, params object[] args);


        void Warn(string message, params object[] args);
        void Warn(Exception exception, string message);
        
        void Error(string message);
        void Error(string message, Exception exception);
        void ErrorException(string message, Exception exception, params object[] args);
        
        
        void Fatal(string message, params object[] args);
        void FatalException(string message, Exception exception, params object[] args);
        void Fatal(Exception exception);
        void Fatal(string message, Exception exception);

        void WarnException(string message, Exception exception);
    }
}