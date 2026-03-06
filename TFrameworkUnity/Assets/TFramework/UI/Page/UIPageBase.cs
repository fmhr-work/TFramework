using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace TFramework.UI
{
    /// <summary>
    /// ページの基底クラス
    /// すべてのUIページはこのクラスを継承する
    /// </summary>
    public abstract class UIPageBase : MonoBehaviour, IUIPageLifecycle
    {
        #region Serialized Fields
        [Header("Page Settings")]
        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private string _pageAddress;

        [SerializeField]
        private bool _cacheOnClose = true;
        #endregion

        #region Private Fields
        private bool _isInitialized;
        private CancellationTokenSource _pageCts;

        private readonly Subject<Unit> _onOpenedSubject = new();
        private readonly Subject<Unit> _onClosedSubject = new();
        #endregion

        #region Properties
        /// <summary>
        /// ページのAddressableキー
        /// </summary>
        public string PageAddress => _pageAddress;

        /// <summary>
        /// ページを閉じた時にキャッシュするかどうか
        /// </summary>
        public bool CacheOnClose => _cacheOnClose;

        /// <summary>
        /// ページが初期化済みかどうか
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// ページが表示中かどうか
        /// </summary>
        public bool IsVisible => _canvasGroup != null && _canvasGroup.alpha > 0;

        /// <summary>
        /// CanvasGroup
        /// </summary>
        public CanvasGroup CanvasGroup => _canvasGroup;

        /// <summary>
        /// ページ固有のCancellationToken。ページが破棄されるまで有効
        /// </summary>
        protected CancellationToken PageToken => _pageCts?.Token ?? CancellationToken.None;
        #endregion

        #region Observable Methods
        /// <summary>
        /// ページが開かれた時に発行されるObservable
        /// </summary>
        public Observable<Unit> OnOpenedAsObservable() => _onOpenedSubject;

        /// <summary>
        /// ページが閉じられた時に発行されるObservable
        /// </summary>
        public Observable<Unit> OnClosedAsObservable() => _onClosedSubject;
        #endregion

        #region Lifecycle (IUIPageLifecycle)
        async UniTask IUIPageLifecycle.OnInitializeAsync(CancellationToken ct)
        {
            if (_isInitialized)
                return;

            _pageCts = new CancellationTokenSource();
            await OnInitializeAsync(ct);
            _isInitialized = true;
        }

        async UniTask IUIPageLifecycle.OnPreOpenAsync(object param, CancellationToken ct)
        {
            await OnPreOpenAsync(param, ct);
        }

        void IUIPageLifecycle.OnOpened()
        {
            // ページが再表示される際に新しいCancellationTokenSourceを作成
            _pageCts?.Cancel();
            _pageCts?.Dispose();
            _pageCts = new CancellationTokenSource();
            
            OnOpened();
            _onOpenedSubject.OnNext(Unit.Default);
        }

        async UniTask IUIPageLifecycle.OnPreCloseAsync(CancellationToken ct)
        {
            await OnPreCloseAsync(ct);
        }

        void IUIPageLifecycle.OnClosed()
        {
            // ページが閉じられた時に全ての購読をキャンセル
            // PageTokenを使った購読は自動的にクリーンアップされる
            _pageCts?.Cancel();
            
            OnClosed();
            _onClosedSubject.OnNext(Unit.Default);
        }

        void IUIPageLifecycle.OnTerminate()
        {
            _pageCts?.Cancel();
            _pageCts?.Dispose();
            _pageCts = null;
            _onOpenedSubject.Dispose();
            _onClosedSubject.Dispose();
            OnTerminate();
        }

        bool IUIPageLifecycle.OnBackPressed()
        {
            return OnBackPressed();
        }
        #endregion

        #region Protected Virtual Methods
        /// <summary>
        /// ページの初期化処理。オーバーライドして使用する
        /// </summary>
        protected virtual UniTask OnInitializeAsync(CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// ページが表示される直前の処理。オーバーライドして使用する
        /// </summary>
        protected virtual UniTask OnPreOpenAsync(object param, CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// ページが表示された直後の処理。オーバーライドして使用する
        /// </summary>
        protected virtual void OnOpened()
        {
        }

        /// <summary>
        /// ページが非表示になる直前の処理。オーバーライドして使用する
        /// </summary>
        protected virtual UniTask OnPreCloseAsync(CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// ページが非表示になった直後の処理。オーバーライドして使用する
        /// </summary>
        protected virtual void OnClosed()
        {
        }

        /// <summary>
        /// ページが破棄される時の処理。オーバーライドして使用する
        /// </summary>
        protected virtual void OnTerminate()
        {
        }

        /// <summary>
        /// 戻るボタンが押された時の処理。オーバーライドして使用する
        /// </summary>
        /// <returns>trueを返すとデフォルトの戻る処理をキャンセル</returns>
        protected virtual bool OnBackPressed()
        {
            return false;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// ページの表示状態を設定する
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        /// <summary>
        /// ページのインタラクション可否を設定する
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
        }
        #endregion
    }
}
