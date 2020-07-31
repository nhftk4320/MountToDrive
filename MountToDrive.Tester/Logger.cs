using DokanNet.Logging;
using System;

namespace MountToDrive.Tester
{
    public class Logger : ILogger
    {
        public enum LogLevelEnum
        {
            Debug,
            Info,
            Warn,
            Error,
            Fatal
        }
        
        private readonly LogLevelEnum _logLevel;
        public Logger(LogLevelEnum logLevel)
        {
            this._logLevel = logLevel;
        }

        public void Debug(string message, params object[] args) => Log(LogLevelEnum.Debug, message, args);

        public void Error(string message, params object[] args) => Log(LogLevelEnum.Error, message, args);

        public void Fatal(string message, params object[] args) => Log(LogLevelEnum.Fatal, message, args);

        public void Info(string message, params object[] args) => Log(LogLevelEnum.Info, message, args);

        public void Warn(string message, params object[] args) => Log(LogLevelEnum.Warn, message, args);

        private void Log(LogLevelEnum logLevel, string logMessage, params object[] args)
        {
            if (logLevel >= this._logLevel)
            {
                logMessage = args.Length > 0 ? String.Format(logMessage, args) : logMessage;
                Console.WriteLine("{0} >> {1}: {2}",DateTime.Now.ToString("HH:mm:ss.ff"), logLevel.ToString(), logMessage);
            }
        }
    }
}
