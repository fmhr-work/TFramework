using System;
using TFramework.Core;
using UnityEngine;

namespace TFramework.Debug
{
    /// <summary>
    /// Unity Consoleへのログ出力
    /// </summary>
    public class UnityConsoleLogOutput : ILogOutput
    {
        private readonly LogLevel _minimumLevel;

        public bool IsEnabled { get; private set; } = true;

        public UnityConsoleLogOutput(LogLevel minimumLevel = LogLevel.Debug)
        {
            _minimumLevel = minimumLevel;
        }

        public void Write(LogLevel level, string message, string category, Exception exception)
        {
            if (!IsEnabled || level < _minimumLevel)
                return;

            var formattedMessage = FormatMessage(level, message, category, exception);

            switch (level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(formattedMessage);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    if (exception != null)
                    {
                        UnityEngine.Debug.LogException(exception);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError(formattedMessage);
                    }
                    break;
            }
        }

        private static string FormatMessage(LogLevel level, string message, string category, Exception exception)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var categoryTag = string.IsNullOrEmpty(category) ? "" : $"[{category}] ";
            var levelTag = GetLevelTag(level);
            var exceptionInfo = exception != null ? $"\n{exception}" : "";

            return $"[{timestamp}] {levelTag} {categoryTag}{message}{exceptionInfo}";
        }

        private static string GetLevelTag(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => "[TRC]",
                LogLevel.Debug => "[DBG]",
                LogLevel.Info => "[INF]",
                LogLevel.Warning => "[WRN]",
                LogLevel.Error => "[ERR]",
                LogLevel.Fatal => "[FTL]",
                _ => "[???]"
            };
        }

        public void Dispose()
        {
            IsEnabled = false;
        }
    }
}
