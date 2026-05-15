using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace TFramework.UI
{
    /// <summary>
    /// 結果なしダイアログ基底クラス
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIDialogBase : MonoBehaviour, IUIDialog, IScenePersistentUI
    {
        [Header("Dialog Settings")]
        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private Button _backgroundButton;

        [SerializeField]
        private bool _closeOnBackgroundClick = true;

        [SerializeField]
        private bool _persistAcrossScenes;

        private CancellationTokenSource _dialogCts;
        private UniTaskCompletionSource _completionSource;
        private CompositeDisposable _disposables;

        /// <summary>
        /// CanvasGroup参照
        /// </summary>
        public CanvasGroup CanvasGroup => _canvasGroup;

        /// <summary>
        /// scene跨ぎ保持可否
        /// </summary>
        public bool PersistAcrossScenes => _persistAcrossScenes;

        /// <summary>
        /// ダイアログ存続中のキャンセルトークン
        /// </summary>
        protected CancellationToken DialogToken => _dialogCts?.Token ?? CancellationToken.None;

        /// <summary>
        /// 表示前初期化経路
        /// </summary>
        async UniTask IUIDialog.OnPreOpenAsync(object param, CancellationToken ct)
        {
            _dialogCts = new CancellationTokenSource();
            _completionSource = new UniTaskCompletionSource();
            _disposables = new CompositeDisposable();
            SubscribeBackgroundClose();
            await OnPreOpenAsync(param, ct);
        }

        /// <summary>
        /// 表示確定経路
        /// </summary>
        void IUIDialog.OnOpened()
        {
            OnOpened();
        }

        /// <summary>
        /// 非表示前処理経路
        /// </summary>
        async UniTask IUIDialog.OnPreCloseAsync(CancellationToken ct)
        {
            await OnPreCloseAsync(ct);
        }

        /// <summary>
        /// 非表示確定経路
        /// </summary>
        void IUIDialog.OnClosed()
        {
            _dialogCts?.Cancel();
            _dialogCts?.Dispose();
            _dialogCts = null;
            _disposables?.Dispose();
            _disposables = null;
            OnClosed();
        }

        /// <summary>
        /// 最終破棄経路
        /// </summary>
        void IUIDialog.OnTerminate()
        {
            _dialogCts?.Cancel();
            _dialogCts?.Dispose();
            _dialogCts = null;
            _disposables?.Dispose();
            _disposables = null;
            _completionSource = null;
            OnTerminate();
        }

        protected virtual UniTask OnPreOpenAsync(object param, CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnOpened()
        {
        }

        protected virtual UniTask OnPreCloseAsync(CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnClosed()
        {
        }

        protected virtual void OnTerminate()
        {
        }

        protected virtual void OnBackgroundClicked()
        {
            Close();
        }

        /// <summary>
        /// 閉鎖待機
        /// </summary>
        public async UniTask WaitUntilClosedAsync(CancellationToken ct = default)
        {
            if (_completionSource == null)
            {
                return;
            }

            await _completionSource.Task.AttachExternalCancellation(ct);
        }

        /// <summary>
        /// 閉鎖要求
        /// </summary>
        public void Close()
        {
            _completionSource?.TrySetResult();
        }

        /// <summary>
        /// 表示状態反映
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        /// <summary>
        /// 背景クリック購読登録
        /// </summary>
        private void SubscribeBackgroundClose()
        {
            if (_backgroundButton == null || !_closeOnBackgroundClick)
            {
                return;
            }

            _backgroundButton.OnClickAsObservable()
                .Subscribe(_ => OnBackgroundClicked())
                .AddTo(_disposables);
        }
    }

    /// <summary>
    /// 結果ありダイアログ基底クラス
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIDialogBase<TResult> : MonoBehaviour, IUIDialog, IScenePersistentUI
    {
        [Header("Dialog Settings")]
        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private Button _backgroundButton;

        [SerializeField]
        private bool _closeOnBackgroundClick = true;

        [SerializeField]
        private bool _persistAcrossScenes;

        private CancellationTokenSource _dialogCts;
        private UniTaskCompletionSource<TResult> _completionSource;
        private CompositeDisposable _disposables;

        /// <summary>
        /// CanvasGroup参照
        /// </summary>
        public CanvasGroup CanvasGroup => _canvasGroup;

        /// <summary>
        /// scene跨ぎ保持可否
        /// </summary>
        public bool PersistAcrossScenes => _persistAcrossScenes;

        /// <summary>
        /// ダイアログ存続中のキャンセルトークン
        /// </summary>
        protected CancellationToken DialogToken => _dialogCts?.Token ?? CancellationToken.None;

        /// <summary>
        /// 表示前初期化経路
        /// </summary>
        async UniTask IUIDialog.OnPreOpenAsync(object param, CancellationToken ct)
        {
            _dialogCts = new CancellationTokenSource();
            _completionSource = new UniTaskCompletionSource<TResult>();
            _disposables = new CompositeDisposable();
            SubscribeBackgroundClose();
            await OnPreOpenAsync(param, ct);
        }

        /// <summary>
        /// 表示確定経路
        /// </summary>
        void IUIDialog.OnOpened()
        {
            OnOpened();
        }

        /// <summary>
        /// 非表示前処理経路
        /// </summary>
        async UniTask IUIDialog.OnPreCloseAsync(CancellationToken ct)
        {
            await OnPreCloseAsync(ct);
        }

        /// <summary>
        /// 非表示確定経路
        /// </summary>
        void IUIDialog.OnClosed()
        {
            _dialogCts?.Cancel();
            _dialogCts?.Dispose();
            _dialogCts = null;
            _disposables?.Dispose();
            _disposables = null;
            OnClosed();
        }

        /// <summary>
        /// 最終破棄経路
        /// </summary>
        void IUIDialog.OnTerminate()
        {
            _dialogCts?.Cancel();
            _dialogCts?.Dispose();
            _dialogCts = null;
            _disposables?.Dispose();
            _disposables = null;
            _completionSource = null;
            OnTerminate();
        }

        protected virtual UniTask OnPreOpenAsync(object param, CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnOpened()
        {
        }

        protected virtual UniTask OnPreCloseAsync(CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnClosed()
        {
        }

        protected virtual void OnTerminate()
        {
        }

        protected virtual void OnBackgroundClicked()
        {
            CloseWithResult(default);
        }

        /// <summary>
        /// result待機
        /// </summary>
        public async UniTask<TResult> WaitForResultAsync(CancellationToken ct = default)
        {
            if (_completionSource == null)
            {
                return default;
            }

            return await _completionSource.Task.AttachExternalCancellation(ct);
        }

        /// <summary>
        /// result確定と閉鎖
        /// </summary>
        public void CloseWithResult(TResult result)
        {
            _completionSource?.TrySetResult(result);
        }

        /// <summary>
        /// 表示状態反映
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        /// <summary>
        /// 背景クリック購読登録
        /// </summary>
        private void SubscribeBackgroundClose()
        {
            if (_backgroundButton == null || !_closeOnBackgroundClick)
            {
                return;
            }

            _backgroundButton.OnClickAsObservable()
                .Subscribe(_ => OnBackgroundClicked())
                .AddTo(_disposables);
        }
    }
}
