using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Debug;
using TFramework.Resource;
using VContainer;

namespace TFramework.UI
{
    /// <summary>
    /// UI管理の公開サービス
    /// </summary>
    public sealed class UIManager : IUIService, Core.IInitializable, IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly UIRootService _rootService;
        private readonly UIAddressService _addressService;
        private readonly UIPageService _pageService;
        private readonly UIDialogService _dialogService;
        private readonly UISceneCleanupService _sceneCleanupService;
        private readonly UILoadingService _loadingService;

        private bool _isDisposed;
        private bool _isSceneLoadedSubscribed;

        [Inject]
        public UIManager(IResourceService resourceService, IObjectResolver container, UISettings settings = null)
        {
            UISettings resolvedSettings = settings ?? UISettings.Instance;
            _cts = new CancellationTokenSource();

            IUIAnimation transition = new FadeTransition(resolvedSettings.DefaultTransitionDuration);

            _rootService = new UIRootService(resourceService, resolvedSettings);
            _addressService = new UIAddressService();
            _pageService = new UIPageService(resourceService, container, _rootService, transition, GetManagerToken);
            _dialogService = new UIDialogService(resourceService, container, _rootService, transition, GetManagerToken);
            _sceneCleanupService = new UISceneCleanupService(_pageService, _dialogService);
            _loadingService = new UILoadingService(resolvedSettings, GetManagerToken);
        }

        public UIPageBase CurrentPage => _pageService.CurrentPage;
        public int PageStackCount => _pageService.PageStackCount;
        public bool IsLoading => _loadingService.IsLoading;

        /// <summary>
        /// 内部サービス初期化
        /// </summary>
        public async UniTask InitializeAsync(CancellationToken ct)
        {
            await _rootService.InitializeAsync(ct);
            SubscribeSceneLoaded();
            _rootService.NormalizeAfterSceneLoad();
            TLogger.Info("[UIManager] Initialized");
        }

        /// <summary>
        /// 型指定page表示
        /// </summary>
        public UniTask ShowPageAsync<TPage>(object param = null, CancellationToken ct = default)
            where TPage : UIPageBase
        {
            string address = _addressService.GetPageAddress<TPage>();
            return _pageService.ShowPageAsync(address, param, ct);
        }

        /// <summary>
        /// 直接指定page表示
        /// </summary>
        public UniTask ShowPageAsync(string address, object param = null, CancellationToken ct = default)
        {
            return _pageService.ShowPageAsync(address, param, ct);
        }

        /// <summary>
        /// 前page復帰
        /// </summary>
        public UniTask<bool> GoBackAsync(CancellationToken ct = default)
        {
            return _pageService.GoBackAsync(ct);
        }

        /// <summary>
        /// pageスタック全消去
        /// </summary>
        public UniTask ClearStackAsync(CancellationToken ct = default)
        {
            return _pageService.ClearStackAsync(ct);
        }

        /// <summary>
        /// 型指定dialog表示
        /// </summary>
        public UniTask ShowDialogAsync<TDialog>(object param = null, CancellationToken ct = default)
            where TDialog : UIDialogBase
        {
            string address = _addressService.GetDialogAddress<TDialog>();
            return _dialogService.ShowDialogAsync<TDialog>(address, param, ct);
        }

        /// <summary>
        /// 戻り値付きdialog表示
        /// </summary>
        public UniTask<TResult> ShowDialogAsync<TDialog, TResult>(object param = null, CancellationToken ct = default)
            where TDialog : UIDialogBase<TResult>
        {
            string address = _addressService.GetDialogAddress<TDialog>();
            return _dialogService.ShowDialogAsync<TDialog, TResult>(address, param, ct);
        }

        /// <summary>
        /// 直接指定dialog表示
        /// </summary>
        public UniTask ShowDialogAsync(string address, object param = null, CancellationToken ct = default)
        {
            return _dialogService.ShowDialogAsync<UIDialogBase>(address, param, ct);
        }

        /// <summary>
        /// toast表示
        /// </summary>
        public void ShowToast(string message, float duration = 0f)
        {
            _loadingService.ShowToast(message, duration);
        }

        /// <summary>
        /// loading表示開始
        /// </summary>
        public IDisposable ShowLoading(string message = null)
        {
            return _loadingService.ShowLoading(message);
        }

        /// <summary>
        /// loading表示終了
        /// </summary>
        public void HideLoading()
        {
            _loadingService.HideLoading();
        }

        /// <summary>
        /// page用address登録
        /// </summary>
        public void RegisterPageAddress<TPage>(string address) where TPage : UIPageBase
        {
            _addressService.RegisterPageAddress<TPage>(address);
        }

        /// <summary>
        /// dialog用address登録
        /// </summary>
        public void RegisterDialogAddress<TDialog>(string address) where TDialog : UIDialogBase
        {
            _addressService.RegisterDialogAddress<TDialog>(address);
        }

        /// <summary>
        /// 全サービス破棄
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            UnsubscribeSceneLoaded();

            _cts.Cancel();
            _cts.Dispose();

            _dialogService.DisposeAll();
            _pageService.DisposeAll();
            _rootService.Dispose();

            TLogger.Info("[UIManager] Disposed");
        }

        /// <summary>
        /// manager共通キャンセルトークン取得
        /// </summary>
        private CancellationToken GetManagerToken()
        {
            return _cts.Token;
        }

        /// <summary>
        /// scene読込イベント購読登録
        /// </summary>
        private void SubscribeSceneLoaded()
        {
            if (_isSceneLoadedSubscribed)
            {
                return;
            }

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            _isSceneLoadedSubscribed = true;
        }

        /// <summary>
        /// scene読込イベント購読解除
        /// </summary>
        private void UnsubscribeSceneLoaded()
        {
            if (!_isSceneLoadedSubscribed)
            {
                return;
            }

            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            _isSceneLoadedSubscribed = false;
        }

        /// <summary>
        /// scene読込後の状態再正規化
        /// </summary>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (_isDisposed)
            {
                return;
            }

            _sceneCleanupService.HandleSceneLoaded(scene, mode);
            _rootService.NormalizeAfterSceneLoad();
        }
    }
}
