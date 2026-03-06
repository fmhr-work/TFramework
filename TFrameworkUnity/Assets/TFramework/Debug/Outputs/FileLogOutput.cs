using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using TFramework.Core;

namespace TFramework.Debug
{
    /// <summary>
    /// ファイルへのログ出力
    /// ログファイルは永続データパスに保存される
    /// </summary>
    public class FileLogOutput : ILogOutput
    {
        private readonly LogLevel _minimumLevel;
        private readonly string _filePath;
        private readonly object _lock = new();
        private StreamWriter _writer;

        public bool IsEnabled { get; private set; } = true;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="fileName">ログファイル名（拡張子なし）</param>
        /// <param name="minimumLevel">最小ログレベル</param>
        public FileLogOutput(string fileName = "tframework", LogLevel minimumLevel = LogLevel.Info)
        {
            _minimumLevel = minimumLevel;

            // ファイルパスを生成（日時付き）
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _filePath = Path.Combine(
                UnityEngine.Application.persistentDataPath,
                "Logs",
                $"{fileName}_{timestamp}.log"
            );

            InitializeWriter();
        }

        private void InitializeWriter()
        {
            try
            {
                // ディレクトリを作成
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _writer = new StreamWriter(_filePath, true, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                // ヘッダーを書き込み
                _writer.WriteLine($"=== TFramework Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                _writer.WriteLine($"Platform: {UnityEngine.Application.platform}");
                _writer.WriteLine($"Version: {UnityEngine.Application.version}");
                _writer.WriteLine(new string('=', 60));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[TFramework] Failed to initialize file log: {ex.Message}");
                IsEnabled = false;
            }
        }

        public void Write(LogLevel level, string message, string category, Exception exception)
        {
            if (!IsEnabled || level < _minimumLevel || _writer == null)
                return;

            var formattedMessage = FormatMessage(level, message, category, exception);

            lock (_lock)
            {
                try
                {
                    _writer.WriteLine(formattedMessage);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[TFramework] Failed to write log: {ex.Message}");
                }
            }
        }

        private static string FormatMessage(LogLevel level, string message, string category, Exception exception)
        {
            var sb = new StringBuilder();
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var categoryTag = string.IsNullOrEmpty(category) ? "" : $"[{category}] ";
            var levelTag = level.ToString().ToUpper().PadRight(7);

            sb.Append($"[{timestamp}] {levelTag} {categoryTag}{message}");

            if (exception != null)
            {
                sb.AppendLine();
                sb.Append("  Exception: ");
                sb.Append(exception.GetType().Name);
                sb.Append(": ");
                sb.AppendLine(exception.Message);
                sb.Append("  StackTrace: ");
                sb.Append(exception.StackTrace);
            }

            return sb.ToString();
        }

        public void Dispose()
        {
            IsEnabled = false;

            lock (_lock)
            {
                if (_writer != null)
                {
                    try
                    {
                        _writer.WriteLine($"=== TFramework Log Ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                        _writer.Flush();
                        _writer.Dispose();
                    }
                    catch
                    {
                        // 無視
                    }
                    finally
                    {
                        _writer = null;
                    }
                }
            }
        }
    }
}
