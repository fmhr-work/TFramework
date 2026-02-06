using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace TFramework.Time
{
    public static class UniTaskTimeExtensions
    {
        /// <summary>
        /// 指定秒数待機する（タイムスケール影響あり）
        /// </summary>
        public static UniTask DelaySeconds(this ITimeService _, float seconds, CancellationToken ct = default)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(seconds), DelayType.DeltaTime, PlayerLoopTiming.Update, ct);
        }

        /// <summary>
        /// 指定秒数待機する（タイムスケール無視）
        /// </summary>
        public static UniTask DelayRealtime(this ITimeService _, float seconds, CancellationToken ct = default)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(seconds), DelayType.Realtime, PlayerLoopTiming.Update, ct);
        }
    }
}
