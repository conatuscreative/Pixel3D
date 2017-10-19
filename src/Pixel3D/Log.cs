using NLog;

namespace Pixel3D.Core
{
    public static class Log
    {
        private const string LogName = "rcru";

        public static Logger Current
        {
            get
            {
                return LogManager.GetLogger(LogName);
            }
        }
    }
}