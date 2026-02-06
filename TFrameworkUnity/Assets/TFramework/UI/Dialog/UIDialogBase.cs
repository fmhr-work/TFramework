using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace TFramework.UI
{
    /// <summary>
    /// ダイアログの基底クラス（結果なし）
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIDialogBase : MonoBehaviour, IUIDialog
    {
        #region Serialized Fields
        [Header("Dialog Settings")]
        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private Button _backgroundButton;

        [SerializeField]
        private bool _closeOnBackgroundClick = true;
        #endregion

        #region Private Fields
        private CancellationTokenSource _dialogCts;
        private UniTaskCompletionSource _completionSource;
        private CompositeDisposable _disposables;
        #endregion

        #region Properties
        public CanvasGroup CanvasGroup => _canvasGroup;
        protected CancellationToken DialogToken => _dialogCts?.Token ?? CancellationToken.None;
        #endregion

        #region Lifecycle (IUIDialog)
        async UniTask IUIDialog.OnPreOpenAsync(object param, CancellationToken ct)
        {
            _dialogCts = new CancellationTokenSource();
            _completionSource = new UniTaskCompletionSource();
            _disposables = new CompositeDisposable();

            // 背景クリックイベント登録（Unity event methodsを使わずR3で実装）
            if (_backgroundButton != null && _closeOnBackgroundClick)
            {
                _backgroundButton.OnClickAsObservable()
                    .Subscribe(_ => OnBackgroundClicked())
                    .AddTo(_disposables);
            }

            await OnPreOpenAsync(param, ct);
        }

        void IUIDialog.OnOpened()
        {
            OnOpened();
        }

        async UniTask IUIDialog.OnPreCloseAsync(CancellationToken ct)
        {
            await OnPreCloseAsync(ct);
        }

        void IUIDialog.OnClosed()
        {
            _dialogCts?.Cancel();
            _dialogCts?.Dispose();
            _disposables?.Dispose();
            OnClosed();
        }
        #endregion

        #region Protected Virtual Methods
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

        protected virtual void OnBackgroundClicked()
        {
            Close();
        }
        #endregion

        #region Public Methods
        public async UniTask WaitUntilClosedAsync(CancellationToken ct = default)
        {
            if (_completionSource == null)
            {
                return;
            }

            await _completionSource.Task.AttachExternalCancellation(ct);
        }

        public void Close()
        {
            _completionSource?.TrySetResult();
        }

        public void SetVisible(bool visible)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
        #endregion
    }

    /// <summary>
    /// ダイアログの基底クラス（結果あり）
    /// </summary>
    /// <typeparam name="TResult">結果の型</typeparam>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIDialogBase<TResult> : MonoBehaviour, IUIDialog
    {
        #region Serialized Fields
        [Header("Dialog Settings")]
        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private Button _backgroundButton;

        [SerializeField]
        private bool _closeOnBackgroundClick = true;
        #endregion

        #region Private Fields
        private CancellationTokenSource _dialogCts;
        private UniTaskCompletionSource<TResult> _completionSource;
        private CompositeDisposable _disposables;
        #endregion

        #region Properties
        public CanvasGroup CanvasGroup => _canvasGroup;
        protected CancellationToken DialogToken => _dialogCts?.Token ?? CancellationToken.None;
        #endregion

        #region Lifecycle (IUIDialog)
        async UniTask IUIDialog.OnPreOpenAsync(object param, CancellationToken ct)
        {
            _dialogCts = new CancellationTokenSource();
            _completionSource = new UniTaskCompletionSource<TResult>();
            _disposables = new CompositeDisposable();

            // 背景クリックイベント登録
            if (_backgroundButton != null && _closeOnBackgroundClick)
            {
                _backgroundButton.OnClickAsObservable()
                    .Subscribe(_ => OnBackgroundClicked())
                    .AddTo(_disposables);
            }

            await OnPreOpenAsync(param, ct);
        }

        void IUIDialog.OnOpened()
        {
            OnOpened();
        }

        async UniTask IUIDialog.OnPreCloseAsync(CancellationToken ct)
        {
            await OnPreCloseAsync(ct);
        }

        void IUIDialog.OnClosed()
        {
            _dialogCts?.Cancel();
            _dialogCts?.Dispose();
            _disposables?.Dispose();
            OnClosed();
        }
        #endregion

        #region Protected Virtual Methods
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

        protected virtual void OnBackgroundClicked()
        {
            CloseWithResult(default);
        }
        #endregion

        #region Public Methods
        public async UniTask<TResult> WaitForResultAsync(CancellationToken ct = default)
        {
            if (_completionSource == null)
            {
                return default;
            }

            return await _completionSource.Task.AttachExternalCancellation(ct);
        }

        public void CloseWithResult(TResult result)
        {
            _completionSource?.TrySetResult(result);
        }

        public void SetVisible(bool visible)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
        #endregion
    }
}
