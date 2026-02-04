using System;

namespace TFramework.Debug
{
    /// <summary>
    /// ログ出力先のインターフェース
    /// 複数の出力先（コンソール、ファイル、リモートなど）をサポート
    /// </summary>
    public interface ILogOutput : IDisposable
    {
        /// <summary>
        /// ログメッセージを出力する
        /// </summary>
        /// <param name="level">ログレベル</param>
        /// <param name="message">メッセージ</param>
        /// <param name="category">カテゴリ（オプション）</param>
        /// <param name="exception">例外（オプション）</param>
        void Write(Core.LogLevel level, string message, string category, Exception exception);

        /// <summary>
        /// この出力先が有効かどうか
        /// </summary>
        bool IsEnabled { get; }
    }
}
