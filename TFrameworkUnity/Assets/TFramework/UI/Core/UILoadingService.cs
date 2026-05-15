using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Debug;
using UnityEngine;

namespace TFramework.UI
{
    /// <summary>
    /// loading制御サービス
    /// </summary>
    internal sealed class UILoadingService
    {
        private readonly UISettings _settings;
        private readonly Func<CancellationToken> _managerTokenProvider;

        private int _loadingRefCount;

        public UILoadingService(UISettings settings, Func<CancellationToken> managerTokenProvider)
        {
            _settings = settings;
            _managerTokenProvider = managerTokenProvider;
        }

        /// <summary>
        /// loading表示状態
        /// </summary>
        public bool IsLoading => _loadingRefCount > 0;

        /// <summary>
        /// loading表示開始
        /// </summary>
        public IDisposable ShowLoading(string message)
        {
            _loadingRefCount++;
            if (_loadingRefCount == 1)
            {
                ShowLoadingInternal(message).Forget();
            }

            return new LoadingScope(this);
        }

        /// <summary>
        /// loading表示終了
        /// </summary>
        public void HideLoading()
        {
            _loadingRefCount = Mathf.Max(0, _loadingRefCount - 1);
            if (_loadingRefCount == 0)
            {
                HideLoadingInternal().Forget();
            }
        }

        /// <summary>
        /// toast通知
        /// </summary>
        public void ShowToast(string message, float duration)
        {
            float actualDuration = duration > 0f ? duration : _settings.ToastDefaultDuration;
            TLogger.Info($"[UILoadingService] Toast: {message} (duration: {actualDuration}s)");
        }

        /// <summary>
        /// loading表示遅延処理
        /// </summary>
        private async UniTaskVoid ShowLoadingInternal(string message)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_settings.LoadingDelay), cancellationToken: _managerTokenProvider());
                if (_loadingRefCount > 0)
                {
                    TLogger.Debug($"[UILoadingService] Loading shown: {message}");
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// loading終了通知処理
        /// </summary>
        private async UniTaskVoid HideLoadingInternal()
        {
            try
            {
                await UniTask.Yield(cancellationToken: _managerTokenProvider());
                TLogger.Debug("[UILoadingService] Loading hidden");
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// loading参照保持スコープ
        /// </summary>
        private sealed class LoadingScope : IDisposable
        {
            private readonly UILoadingService _loadingService;
            private bool _disposed;

            public LoadingScope(UILoadingService loadingService)
            {
                _loadingService = loadingService;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _loadingService.HideLoading();
            }
        }
    }
}
