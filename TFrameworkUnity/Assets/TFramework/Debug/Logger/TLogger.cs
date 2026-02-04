using System;
using System.Collections.Generic;
using System.Diagnostics;
using TFramework.Core;

namespace TFramework.Debug
{
    /// <summary>
    /// TFrameworkの静的ロガー
    /// 条件付きコンパイルによりReleaseビルドではDebug/Traceログが除去される
    /// </summary>
    public static class TLogger
    {
        private static readonly List<ILogOutput> _outputs = new();
        private static LogLevel _minimumLevel = LogLevel.Debug;
        private static bool _isInitialized;

        /// <summary>
        /// ロガーを初期化する
        /// </summary>
        /// <param name="settings">フレームワーク設定</param>
        public static void Initialize(TFrameworkSettings settings = null)
        {
            if (_isInitialized) return;

            settings ??= TFrameworkSettings.Instance;
            _minimumLevel = settings.MinimumLogLevel;

            // デフォルト出力を追加
            if (settings.EnableLogging)
            {
                AddOutput(new UnityConsoleLogOutput(_minimumLevel));
            }

            if (settings.EnableFileLogging)
            {
                AddOutput(new FileLogOutput("tframework", _minimumLevel));
            }

            _isInitialized = true;
        }

        /// <summary>
        /// 出力先を追加する
        /// </summary>
        public static void AddOutput(ILogOutput output)
        {
            if (output != null && !_outputs.Contains(output))
            {
                _outputs.Add(output);
            }
        }

        /// <summary>
        /// 出力先を削除する
        /// </summary>
        public static void RemoveOutput(ILogOutput output)
        {
            _outputs.Remove(output);
        }

        /// <summary>
        /// すべての出力先をクリアする
        /// </summary>
        public static void ClearOutputs()
        {
            foreach (var output in _outputs)
            {
                output.Dispose();
            }
            _outputs.Clear();
        }

        /// <summary>
        /// ロガーをシャットダウンする
        /// </summary>
        public static void Shutdown()
        {
            ClearOutputs();
            _isInitialized = false;
        }

        /// <summary>
        /// Traceレベルのログを出力（DEBUGビルドのみ）
        /// </summary>
        [Conditional("DEBUG")]
        public static void Trace(string message, string category = null)
        {
            Log(LogLevel.Trace, message, category, null);
        }

        /// <summary>
        /// Debugレベルのログを出力（DEBUGビルドのみ）
        /// </summary>
        [Conditional("DEBUG")]
        public static void Debug(string message, string category = null)
        {
            Log(LogLevel.Debug, message, category, null);
        }

        /// <summary>
        /// Infoレベルのログを出力
        /// </summary>
        public static void Info(string message, string category = null)
        {
            Log(LogLevel.Info, message, category, null);
        }

        /// <summary>
        /// Warningレベルのログを出力
        /// </summary>
        public static void Warning(string message, string category = null)
        {
            Log(LogLevel.Warning, message, category, null);
        }

        /// <summary>
        /// Errorレベルのログを出力
        /// </summary>
        public static void Error(string message, string category = null)
        {
            Log(LogLevel.Error, message, category, null);
        }

        /// <summary>
        /// Errorレベルのログを例外付きで出力
        /// </summary>
        public static void Error(string message, Exception exception, string category = null)
        {
            Log(LogLevel.Error, message, category, exception);
        }

        /// <summary>
        /// Fatalレベルのログを出力
        /// </summary>
        public static void Fatal(string message, Exception exception = null, string category = null)
        {
            Log(LogLevel.Fatal, message, category, exception);
        }

        private static void Log(LogLevel level, string message, string category, Exception exception)
        {
            if (level < _minimumLevel)
                return;

            // 初期化されていない場合はデフォルト出力を使用
            if (!_isInitialized || _outputs.Count == 0)
            {
                var formattedMessage = $"[TFramework] [{category ?? "General"}] {message}";
                switch (level)
                {
                    case LogLevel.Warning:
                        UnityEngine.Debug.LogWarning(formattedMessage);
                        break;
                    case LogLevel.Error:
                    case LogLevel.Fatal:
                        if (exception != null)
                            UnityEngine.Debug.LogException(exception);
                        else
                            UnityEngine.Debug.LogError(formattedMessage);
                        break;
                    default:
                        UnityEngine.Debug.Log(formattedMessage);
                        break;
                }
                return;
            }

            // 登録された出力先にログを送信
            foreach (var output in _outputs)
            {
                if (output.IsEnabled)
                {
                    try
                    {
                        output.Write(level, message, category, exception);
                    }
                    catch
                    {
                        // ログ出力中のエラーは無視
                    }
                }
            }
        }
    }
}
