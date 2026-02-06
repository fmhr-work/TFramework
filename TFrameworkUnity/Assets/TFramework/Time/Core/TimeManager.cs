using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TFramework.Core;
using TFramework.Debug;
using UnityEngine;
using VContainer;

namespace TFramework.Time
{
    /// <summary>
    /// 時間管理の実装
    /// タイムスケールのスタック管理を行う
    /// </summary>
    public sealed class TimeManager : ITimeService, IInitializable, IDisposable
    {
        private class TimeScaleEntry
        {
            public float Scale;
            public string Source;
            public int Priority;
            public int Id; // 一意なID（作成順）
        }

        private readonly TimeSettings _settings;
        private readonly List<TimeScaleEntry> _scaleStack = new();
        private readonly Subject<float> _onTimeScaleChangedSubject = new();
        
        private int _nextId = 0;
        private bool _isDisposed;

        public float TimeScale => UnityEngine.Time.timeScale;
        public Observable<float> OnTimeScaleChanged => _onTimeScaleChangedSubject;
        public bool IsPaused => UnityEngine.Time.timeScale == 0f;

        [Inject]
        public TimeManager(TimeSettings settings)
        {
            _settings = settings;
        }

        public async UniTask InitializeAsync(CancellationToken token)
        {
            TLogger.Info("[TimeManager] Initializing...");
            ResetTimeScale();
            await UniTask.CompletedTask;
        }

        public IDisposable SetTimeScale(float scale, string source, int priority = 0)
        {
            if (_isDisposed) return Disposable.Empty;

            scale = Mathf.Clamp(scale, _settings.MinTimeScale, _settings.MaxTimeScale);
            
            var entry = new TimeScaleEntry
            {
                Scale = scale,
                Source = source,
                Priority = priority,
                Id = _nextId++
            };

            _scaleStack.Add(entry);
            UpdateTimeScale();

            TLogger.Info($"[TimeManager] SetTimeScale request: {scale} (Source: {source}, Priority: {priority})");

            return Disposable.Create(() =>
            {
                if (_isDisposed) return;
                _scaleStack.Remove(entry);
                UpdateTimeScale();
                TLogger.Info($"[TimeManager] Removed TimeScale request from: {source}");
            });
        }

        public IDisposable Pause(string source)
        {
            // Pause is effectively setting time scale to 0 with high priority
            return SetTimeScale(0f, source, int.MaxValue);
        }

        private void UpdateTimeScale()
        {
            if (_scaleStack.Count == 0)
            {
                ApplyTimeScale(_settings.DefaultTimeScale);
                return;
            }

            // 優先度が高い順、同じ優先度なら新しい順（Idが大きい順）にソート
            // ただし、0（ポーズ）は特別な扱いをせずとも、優先度最大で登録すれば勝てる
            // スタック方式なので、「現在有効なもの」を決定するロジックが必要
            
            // 最も優先度が高いエントリを探す
            var activeEntry = _scaleStack
                .OrderByDescending(x => x.Priority)
                .ThenByDescending(x => x.Id)
                .First();

            ApplyTimeScale(activeEntry.Scale);
        }

        private void ApplyTimeScale(float scale)
        {
            if (Math.Abs(UnityEngine.Time.timeScale - scale) > Mathf.Epsilon)
            {
                UnityEngine.Time.timeScale = scale;
                _onTimeScaleChangedSubject.OnNext(scale);
                TLogger.Info($"[TimeManager] TimeScale updated to: {scale}");
            }
        }

        private void ResetTimeScale()
        {
            _scaleStack.Clear();
            ApplyTimeScale(_settings.DefaultTimeScale);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _onTimeScaleChangedSubject.Dispose();
            ResetTimeScale();
        }
    }
}
