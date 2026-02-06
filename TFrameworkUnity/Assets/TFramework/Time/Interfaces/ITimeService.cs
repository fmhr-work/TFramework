using System;
using R3;
using TFramework.Core;

namespace TFramework.Time
{
    /// <summary>
    /// 時間管理サービスのインターフェース
    /// ゲーム時間のスケーリング、一時停止、タイマー管理を提供する
    /// </summary>
    public interface ITimeService : IService
    {
        /// <summary>
        /// 現在のタイムスケール
        /// </summary>
        float TimeScale { get; }
        
        /// <summary>
        /// タイムスケール変更通知
        /// </summary>
        Observable<float> OnTimeScaleChanged { get; }
        
        /// <summary>
        /// 一時停止中かどうか
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// タイムスケールを設定（スタック管理）
        /// </summary>
        /// <param name="scale">スケール値</param>
        /// <param name="source">変更元識別子</param>
        /// <param name="priority">優先度（高い方が優先）</param>
        IDisposable SetTimeScale(float scale, string source, int priority = 0);

        /// <summary>
        /// ゲームを一時停止
        /// </summary>
        /// <param name="source">一時停止元識別子</param>
        IDisposable Pause(string source);
    }
}
