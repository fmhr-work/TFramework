using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace TFramework.Core
{
    /// <summary>
    /// UniTask関連の拡張メソッド
    /// </summary>
    public static class UniTaskExtensions
    {
        /// <summary>
        /// UniTaskを安全に実行し、例外をハンドリングする
        /// Fire-and-forgetパターンで使用
        /// </summary>
        /// <param name="task">実行するタスク</param>
        /// <param name="onError">エラー発生時のコールバック（省略時はDebug.LogError）</param>
        public static void ForgetWithErrorHandler(this UniTask task, Action<Exception> onError = null)
        {
            task.Forget(ex =>
            {
                if (ex is OperationCanceledException)
                {
                    // キャンセルは無視
                    return;
                }

                if (onError != null)
                {
                    onError(ex);
                }
                else
                {
                    UnityEngine.Debug.LogError($"[TFramework] Unhandled exception in async operation: {ex}");
                }
            });
        }

        /// <summary>
        /// タイムアウト付きでUniTaskを実行
        /// </summary>
        /// <param name="task">実行するタスク</param>
        /// <param name="timeout">タイムアウト時間</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>結果を含むUniTask</returns>
        public static async UniTask<T> WithTimeout<T>(this UniTask<T> task, TimeSpan timeout, CancellationToken ct = default)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);

            try
            {
                return await task.AttachExternalCancellation(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds.");
            }
        }

        /// <summary>
        /// タイムアウト付きでUniTaskを実行（戻り値なし）
        /// </summary>
        public static async UniTask WithTimeout(this UniTask task, TimeSpan timeout, CancellationToken ct = default)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);

            try
            {
                await task.AttachExternalCancellation(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds.");
            }
        }

        /// <summary>
        /// リトライ付きでUniTaskを実行
        /// </summary>
        /// <param name="taskFactory">タスクを生成するファクトリ</param>
        /// <param name="retryCount">リトライ回数</param>
        /// <param name="retryDelay">リトライ間隔</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>結果を含むUniTask</returns>
        public static async UniTask<T> WithRetry<T>(
            Func<CancellationToken, UniTask<T>> taskFactory,
            int retryCount = 3,
            TimeSpan? retryDelay = null,
            CancellationToken ct = default)
        {
            var delay = retryDelay ?? TimeSpan.FromSeconds(1);
            Exception lastException = null;

            for (int i = 0; i <= retryCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    return await taskFactory(ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (i < retryCount)
                    {
#if DEBUG
                        UnityEngine.Debug.LogWarning(
                            $"[TFramework] Retry {i + 1}/{retryCount} after error: {ex.Message}");
#endif
                        await UniTask.Delay(delay, cancellationToken: ct);
                    }
                }
            }

            throw lastException ?? new Exception("Unknown error during retry operation.");
        }
    }
}
