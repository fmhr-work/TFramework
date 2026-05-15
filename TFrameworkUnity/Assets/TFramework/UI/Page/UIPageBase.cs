using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace TFramework.UI
{
    /// <summary>
    /// ページ基底クラス
    /// </summary>
    public abstract class UIPageBase : MonoBehaviour, IUIPageLifecycle, IScenePersistentUI
    {
        [Header("Page Settings")]
        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private string _pageAddress;

        [SerializeField]
        private bool _cacheOnClose = true;

        [SerializeField]
        private bool _persistAcrossScenes;

        private bool _isInitialized;
        private CancellationTokenSource _pageCts;

        private readonly Subject<Unit> _onOpenedSubject = new();
        private readonly Subject<Unit> _onClosedSubject = new();

        /// <summary>
        /// Addressableキー参照
        /// </summary>
        public string PageAddress => _pageAddress;

        /// <summary>
        /// Close後の再利用可否
        /// </summary>
        public bool CacheOnClose => _cacheOnClose;

        /// <summary>
        /// scene跨ぎ保持可否
        /// </summary>
        public bool PersistAcrossScenes => _persistAcrossScenes;

        /// <summary>
        /// 初期化完了状態
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 表示状態参照
        /// </summary>
        public bool IsVisible => _canvasGroup != null && _canvasGroup.alpha > 0f;

        /// <summary>
        /// CanvasGroup参照
        /// </summary>
        public CanvasGroup CanvasGroup => _canvasGroup;

        /// <summary>
        /// page表示中キャンセルトークン
        /// </summary>
        protected CancellationToken PageToken => _pageCts?.Token ?? CancellationToken.None;

        /// <summary>
        /// Open通知
        /// </summary>
        public Observable<Unit> OnOpenedAsObservable() => _onOpenedSubject;

        /// <summary>
        /// Close通知
        /// </summary>
        public Observable<Unit> OnClosedAsObservable() => _onClosedSubject;

        /// <summary>
        /// 初回初期化経路
        /// </summary>
        async UniTask IUIPageLifecycle.OnInitializeAsync(CancellationToken ct)
        {
            if (_isInitialized)
            {
                return;
            }

            _pageCts = new CancellationTokenSource();
            await OnInitializeAsync(ct);
            _isInitialized = true;
        }

        /// <summary>
        /// 表示前処理経路
        /// </summary>
        async UniTask IUIPageLifecycle.OnPreOpenAsync(object param, CancellationToken ct)
        {
            await OnPreOpenAsync(param, ct);
        }

        /// <summary>
        /// 表示確定経路
        /// </summary>
        void IUIPageLifecycle.OnOpened()
        {
            RenewPageToken();
            OnOpened();
            _onOpenedSubject.OnNext(Unit.Default);
        }

        /// <summary>
        /// 非表示前処理経路
        /// </summary>
        async UniTask IUIPageLifecycle.OnPreCloseAsync(CancellationToken ct)
        {
            await OnPreCloseAsync(ct);
        }

        /// <summary>
        /// 非表示確定経路
        /// </summary>
        void IUIPageLifecycle.OnClosed()
        {
            _pageCts?.Cancel();
            OnClosed();
            _onClosedSubject.OnNext(Unit.Default);
        }

        /// <summary>
        /// 最終破棄経路
        /// </summary>
        void IUIPageLifecycle.OnTerminate()
        {
            _pageCts?.Cancel();
            _pageCts?.Dispose();
            _pageCts = null;
            _onOpenedSubject.Dispose();
            _onClosedSubject.Dispose();
            OnTerminate();
        }

        /// <summary>
        /// 戻る操作委譲
        /// </summary>
        bool IUIPageLifecycle.OnBackPressed()
        {
            return OnBackPressed();
        }

        protected virtual UniTask OnInitializeAsync(CancellationToken ct)
        {
            return UniTask.CompletedTask;
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

        protected virtual bool OnBackPressed()
        {
            return false;
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
        /// 入力可否反映
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
        }

        /// <summary>
        /// 再表示前のトークン更新
        /// </summary>
        private void RenewPageToken()
        {
            _pageCts?.Cancel();
            _pageCts?.Dispose();
            _pageCts = new CancellationTokenSource();
        }
    }
}
