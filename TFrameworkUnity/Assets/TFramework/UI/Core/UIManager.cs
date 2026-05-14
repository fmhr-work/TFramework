using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Debug;
using TFramework.Resource;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace TFramework.UI
{
    /// <summary>
    /// UIサービスの実装
    /// ページ、ダイアログ、トースト、ローディングを管理する
    /// </summary>
    public sealed class UIManager : IUIService, Core.IInitializable
    {
        private enum UIRootOrigin
        {
            None,
            ExistingSceneObject,
            AddressableInstance,
            DefaultCreated
        }

        #region Dependencies
        private readonly IResourceService _resourceService;
        private readonly UISettings _settings;
        private readonly IObjectResolver _container;
        #endregion

        #region Private Fields
        private UIRoot _uiRoot;
        private UIRootOrigin _uiRootOrigin = UIRootOrigin.None;
        private IUIAnimation _defaultTransition;
        private int _loadingRefCount;
        private CancellationTokenSource _cts;
        private bool _isDisposed;
        private bool _sceneLoadedHooked;

        private readonly Dictionary<Type, string> _pageAddressMap = new();
        private readonly Dictionary<Type, string> _dialogAddressMap = new();
        private readonly Dictionary<string, UIPageBase> _pageCache = new();
        private readonly Dictionary<string, IUIDialog> _dialogCache = new();
        private readonly Stack<UIPageBase> _pageStack = new();
        #endregion

        #region Constants
        private const string DontDestroyOnLoadSceneName = "DontDestroyOnLoad";
        #endregion

        #region Properties
        public UIPageBase CurrentPage => _pageStack.Count > 0 ? _pageStack.Peek() : null;
        public int PageStackCount => _pageStack.Count;
        public bool IsLoading => _loadingRefCount > 0;
        #endregion

        #region Constructor
        [Inject]
        public UIManager(IResourceService resourceService, IObjectResolver container, UISettings settings = null)
        {
            _resourceService = resourceService;
            _container = container;
            _settings = settings ?? UISettings.Instance;
            _cts = new CancellationTokenSource();
        }
        #endregion

        #region IInitializable
        public async UniTask InitializeAsync(CancellationToken ct)
        {
            _defaultTransition = new FadeTransition(_settings.DefaultTransitionDuration);

            await ResolveUIRootAsync(ct);
            HookSceneLoaded();

            TLogger.Info("[UIManager] Initialized");
        }
        #endregion

        #region Page Navigation
        public async UniTask ShowPageAsync<TPage>(object param = null, CancellationToken ct = default)
            where TPage : UIPageBase
        {
            var address = GetPageAddress<TPage>();
            await ShowPageAsync(address, param, ct);
        }

        public async UniTask ShowPageAsync(string address, object param = null, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(address))
            {
                TLogger.Error("[UIManager] Page address is null or empty");
                return;
            }

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);

            try
            {
                // 現在のページを閉じる
                if (CurrentPage != null)
                {
                    await ((IUIPageLifecycle)CurrentPage).OnPreCloseAsync(linkedCts.Token);
                    await _defaultTransition.PlayHideAsync(CurrentPage.CanvasGroup, linkedCts.Token);
                    ((IUIPageLifecycle)CurrentPage).OnClosed();
                }

                // ページをロードまたはキャッシュから取得
                UIPageBase page;
                if (_pageCache.TryGetValue(address, out var cachedPage))
                {
                    page = cachedPage;
                }
                else
                {
                    page = await _resourceService.InstantiateAsync<UIPageBase>(address, _uiRoot.GetLayerContainer(UILayer.Page), linkedCts.Token);

                    // RectTransformを全画面に設定
                    SetupRectTransform(page.transform as RectTransform);

                    // VContainerで依存性を注入（階層全体）
                    _container.InjectGameObject(page.gameObject);
                }

                // ページを初期化・表示
                var lifecycle = (IUIPageLifecycle)page;
                if (!page.IsInitialized)
                {
                    await lifecycle.OnInitializeAsync(linkedCts.Token);
                }

                page.SetVisible(false);
                page.gameObject.SetActive(true);

                await lifecycle.OnPreOpenAsync(param, linkedCts.Token);
                await _defaultTransition.PlayShowAsync(page.CanvasGroup, linkedCts.Token);
                lifecycle.OnOpened();

                // スタックに追加
                _pageStack.Push(page);

                // キャッシュ登録
                if (page.CacheOnClose && !_pageCache.ContainsKey(address))
                {
                    _pageCache[address] = page;
                }
            }
            catch (OperationCanceledException)
            {
                TLogger.Debug($"[UIManager] ShowPageAsync cancelled: {address}");
                throw;
            }
            catch (Exception ex)
            {
                TLogger.Error($"[UIManager] Failed to show page {address}: {ex.Message}");
            }
        }

        public async UniTask<bool> GoBackAsync(CancellationToken ct = default)
        {
            if (_pageStack.Count <= 1)
                return false;

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);

            var currentPage = _pageStack.Pop();
            var lifecycle = (IUIPageLifecycle)currentPage;

            // 前のページがあるか確認
            if (_pageStack.Count == 0)
            {
                _pageStack.Push(currentPage);
                return false;
            }

            try
            {
                // 現在のページを閉じる
                await lifecycle.OnPreCloseAsync(linkedCts.Token);
                await _defaultTransition.PlayHideAsync(currentPage.CanvasGroup, linkedCts.Token);
                lifecycle.OnClosed();

                if (!currentPage.CacheOnClose)
                {
                    lifecycle.OnTerminate();
                    _resourceService.ReleaseInstance(currentPage.gameObject);
                }
                else
                {
                    currentPage.gameObject.SetActive(false);
                }

                // 前のページを表示
                var previousPage = _pageStack.Peek();
                var prevLifecycle = (IUIPageLifecycle)previousPage;

                previousPage.SetVisible(false);
                previousPage.gameObject.SetActive(true);

                await prevLifecycle.OnPreOpenAsync(null, linkedCts.Token);
                await _defaultTransition.PlayShowAsync(previousPage.CanvasGroup, linkedCts.Token);
                prevLifecycle.OnOpened();

                return true;
            }
            catch (Exception ex)
            {
                TLogger.Error($"[UIManager] Failed to go back: {ex.Message}");
                return false;
            }
        }

        public UniTask ClearStackAsync(CancellationToken ct = default)
        {
            while (_pageStack.Count > 0)
            {
                var page = _pageStack.Pop();
                var lifecycle = (IUIPageLifecycle)page;

                if (!page.CacheOnClose)
                {
                    lifecycle.OnTerminate();
                    _resourceService.ReleaseInstance(page.gameObject);
                }
                else
                {
                    page.gameObject.SetActive(false);
                }
            }

            _pageCache.Clear();
            return UniTask.CompletedTask;
        }
        #endregion

        #region Dialog
        public async UniTask ShowDialogAsync<TDialog>(object param = null, CancellationToken ct = default)
            where TDialog : UIDialogBase
        {
            var address = GetDialogAddress<TDialog>();
            await ShowDialogAsync(address, param, ct);
        }

        public async UniTask<TResult> ShowDialogAsync<TDialog, TResult>(object param = null, CancellationToken ct = default)
            where TDialog : UIDialogBase<TResult>
        {
            var address = GetDialogAddress<TDialog>();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);

            var openParam = ResolveDialogOpenParam(param);
            var cacheOnClose = openParam.CacheOnClose;
            TDialog dialog = null;
            bool completed = false;
            try
            {
                dialog = await GetOrCreateDialogAsync<TDialog>(address, cacheOnClose, linkedCts.Token);

                var lifecycle = (IUIDialog)dialog;
                dialog.SetVisible(false);
                dialog.gameObject.SetActive(true);

                await lifecycle.OnPreOpenAsync(openParam.Payload, linkedCts.Token);
                await _defaultTransition.PlayShowAsync(dialog.CanvasGroup, linkedCts.Token);
                lifecycle.OnOpened();

                var result = await dialog.WaitForResultAsync(linkedCts.Token);

                await lifecycle.OnPreCloseAsync(linkedCts.Token);
                await _defaultTransition.PlayHideAsync(dialog.CanvasGroup, linkedCts.Token);
                lifecycle.OnClosed();
                completed = true;

                if (cacheOnClose)
                {
                    dialog.gameObject.SetActive(false);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                TLogger.Debug($"[UIManager] ShowDialogAsync cancelled: {address}");
                throw;
            }
            catch (Exception ex)
            {
                TLogger.Error($"[UIManager] Failed to show dialog {address}: {ex.Message}");
                return default;
            }
            finally
            {
                if (dialog == null || dialog.gameObject == null)
                {
                    return;
                }

                if (!completed)
                {
                    RemoveCachedDialog(address, dialog);
                    ((IUIDialog)dialog).OnTerminate();
                    _resourceService.ReleaseInstance(dialog.gameObject);
                    return;
                }

                if (!cacheOnClose)
                {
                    ((IUIDialog)dialog).OnTerminate();
                    _resourceService.ReleaseInstance(dialog.gameObject);
                }
            }
        }

        public async UniTask ShowDialogAsync(string address, object param = null, CancellationToken ct = default)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);

            var openParam = ResolveDialogOpenParam(param);
            var cacheOnClose = openParam.CacheOnClose;
            UIDialogBase dialog = null;
            bool completed = false;
            try
            {
                dialog = await GetOrCreateDialogAsync<UIDialogBase>(address, cacheOnClose, linkedCts.Token);

                var lifecycle = (IUIDialog)dialog;
                dialog.SetVisible(false);
                dialog.gameObject.SetActive(true);

                await lifecycle.OnPreOpenAsync(openParam.Payload, linkedCts.Token);
                await _defaultTransition.PlayShowAsync(dialog.CanvasGroup, linkedCts.Token);
                lifecycle.OnOpened();

                await dialog.WaitUntilClosedAsync(linkedCts.Token);

                await lifecycle.OnPreCloseAsync(linkedCts.Token);
                await _defaultTransition.PlayHideAsync(dialog.CanvasGroup, linkedCts.Token);
                lifecycle.OnClosed();
                completed = true;

                if (cacheOnClose)
                {
                    dialog.gameObject.SetActive(false);
                }
            }
            catch (OperationCanceledException)
            {
                TLogger.Debug($"[UIManager] ShowDialogAsync cancelled: {address}");
                throw;
            }
            catch (Exception ex)
            {
                TLogger.Error($"[UIManager] Failed to show dialog {address}: {ex.Message}");
            }
            finally
            {
                if (dialog == null || dialog.gameObject == null)
                {
                    return;
                }

                if (!completed)
                {
                    RemoveCachedDialog(address, dialog);
                    ((IUIDialog)dialog).OnTerminate();
                    _resourceService.ReleaseInstance(dialog.gameObject);
                    return;
                }

                if (!cacheOnClose)
                {
                    ((IUIDialog)dialog).OnTerminate();
                    _resourceService.ReleaseInstance(dialog.gameObject);
                }
            }
        }
        #endregion

        #region Toast
        public void ShowToast(string message, float duration = 0)
        {
            var actualDuration = duration > 0 ? duration : _settings.ToastDefaultDuration;
            TLogger.Info($"[UIManager] Toast: {message} (duration: {actualDuration}s)");
            // TODO: トーストUI実装
        }
        #endregion

        #region Loading
        public IDisposable ShowLoading(string message = null)
        {
            _loadingRefCount++;
            if (_loadingRefCount == 1)
            {
                ShowLoadingInternal(message).Forget();
            }
            return new LoadingScope(this);
        }

        public void HideLoading()
        {
            _loadingRefCount = Mathf.Max(0, _loadingRefCount - 1);
            if (_loadingRefCount == 0)
            {
                HideLoadingInternal().Forget();
            }
        }
        #endregion

        #region Address Registration
        public void RegisterPageAddress<TPage>(string address) where TPage : UIPageBase
        {
            _pageAddressMap[typeof(TPage)] = address;
        }

        public void RegisterDialogAddress<TDialog>(string address) where TDialog : UIDialogBase
        {
            _dialogAddressMap[typeof(TDialog)] = address;
        }
        #endregion

        #region IDisposableService
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _cts?.Cancel();
            _cts?.Dispose();

            if (_sceneLoadedHooked)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _sceneLoadedHooked = false;
            }

            foreach (var page in _pageCache.Values)
            {
                if (page != null && page.gameObject != null)
                {
                    ((IUIPageLifecycle)page).OnTerminate();
                    _resourceService.ReleaseInstance(page.gameObject);
                }
            }

            _pageCache.Clear();
            _pageStack.Clear();

            foreach (var dialog in _dialogCache.Values)
            {
                if (dialog is not Component component || component.gameObject == null)
                {
                    continue;
                }

                dialog.OnTerminate();
                _resourceService.ReleaseInstance(component.gameObject);
            }

            _dialogCache.Clear();

            if (_uiRoot != null && _uiRoot.gameObject != null)
            {
                ReleaseUIRoot();
            }

            TLogger.Info("[UIManager] Disposed");
        }
        #endregion

        #region Private Methods
        private async UniTask ResolveUIRootAsync(CancellationToken ct)
        {
            _uiRoot = FindExistingUIRoot(out var existingCount);
            if (_uiRoot != null)
            {
                _uiRootOrigin = UIRootOrigin.ExistingSceneObject;
                if (existingCount > 1)
                {
                    TLogger.Warning($"[UIManager] Multiple UIRoot objects were found. Using '{_uiRoot.gameObject.name}' from scene '{_uiRoot.gameObject.scene.name}'.");
                }

                FinalizeResolvedUIRoot(_uiRoot);
                return;
            }

            if (!string.IsNullOrEmpty(_settings.UIRootAddress))
            {
                try
                {
                    _uiRoot = await _resourceService.InstantiateAsync<UIRoot>(_settings.UIRootAddress, null, ct);
                    _uiRootOrigin = UIRootOrigin.AddressableInstance;

                    FinalizeResolvedUIRoot(_uiRoot);
                    return;
                }
                catch (Exception ex)
                {
                    TLogger.Warning($"[UIManager] Failed to load UIRoot: {ex.Message}. Creating default UIRoot");
                }
            }

            CreateDefaultUIRoot();
        }

        private void InitializeResolvedUIRoot(UIRoot uiRoot)
        {
            if (uiRoot == null)
            {
                return;
            }

            if (!uiRoot.TryGetComponent<Canvas>(out var canvas))
            {
                TLogger.Warning("[UIManager] Canvas component not found on UIRoot, using defaults");
            }

            if (!uiRoot.TryGetComponent<CanvasScaler>(out var canvasScaler))
            {
                TLogger.Warning("[UIManager] CanvasScaler component not found on UIRoot, using defaults");
            }

            uiRoot.Initialize(canvas, canvasScaler);
        }

        private void CreateDefaultUIRoot()
        {
            var go = new GameObject("UIRoot");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            _uiRoot = go.AddComponent<UIRoot>();
            _uiRootOrigin = UIRootOrigin.DefaultCreated;
            FinalizeResolvedUIRoot(_uiRoot);
        }

        private void HookSceneLoaded()
        {
            if (_sceneLoadedHooked)
            {
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            _sceneLoadedHooked = true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_isDisposed || _uiRoot == null)
            {
                return;
            }

            CleanupDuplicateUIRoots(_uiRoot);
            EnsureSingleEventSystem(_uiRoot);
        }

        private void FinalizeResolvedUIRoot(UIRoot uiRoot)
        {
            if (uiRoot == null || uiRoot.gameObject == null)
            {
                return;
            }

            InitializeResolvedUIRoot(uiRoot);
            Object.DontDestroyOnLoad(uiRoot.gameObject);
            CleanupDuplicateUIRoots(uiRoot);
            EnsureSingleEventSystem(uiRoot);
        }

        private void CleanupDuplicateUIRoots(UIRoot keeper)
        {
            if (keeper == null || keeper.gameObject == null)
            {
                return;
            }

            var roots = Resources.FindObjectsOfTypeAll<UIRoot>();
            var destroyedCount = 0;

            for (int i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                var go = root.gameObject;
                if (go == null || !go.scene.IsValid())
                {
                    continue;
                }

                if (root == keeper)
                {
                    continue;
                }

                Object.Destroy(go);
                destroyedCount++;
            }

            if (destroyedCount > 0)
            {
                TLogger.Warning($"[UIManager] Destroyed {destroyedCount} duplicate UIRoot object(s).");
            }
        }

        private void EnsureSingleEventSystem(UIRoot root)
        {
            if (root == null)
            {
                return;
            }

            var keepEventSystem = FindEventSystemUnderRoot(root.transform);
            if (keepEventSystem == null)
            {
                keepEventSystem = CreateEventSystem(root.transform);
                TLogger.Warning($"[UIManager] EventSystem was missing on '{root.gameObject.name}'. A new one was created.");
            }
            else if (!keepEventSystem.gameObject.activeSelf)
            {
                keepEventSystem.gameObject.SetActive(true);
            }

            if (!HasInputSystemUIInputModule(keepEventSystem.gameObject))
            {
                CreateInputSystemUIInputModule(keepEventSystem.gameObject);
            }

            var allEventSystems = Resources.FindObjectsOfTypeAll<EventSystem>();
            int disabledCount = 0;
            for (int i = 0; i < allEventSystems.Length; i++)
            {
                var eventSystem = allEventSystems[i];
                if (eventSystem == null)
                {
                    continue;
                }

                var go = eventSystem.gameObject;
                if (go == null || !go.scene.IsValid())
                {
                    continue;
                }

                if (eventSystem == keepEventSystem)
                {
                    continue;
                }

                if (go.activeSelf)
                {
                    go.SetActive(false);
                    disabledCount++;
                }
            }

            if (disabledCount > 0)
            {
                TLogger.Warning($"[UIManager] Disabled {disabledCount} duplicate EventSystem object(s).");
            }
        }

        private EventSystem CreateEventSystem(Transform parent)
        {
            var go = new GameObject("EventSystem");
            go.transform.SetParent(parent, false);

            var eventSystem = go.AddComponent<EventSystem>();
            CreateInputSystemUIInputModule(go);
            Object.DontDestroyOnLoad(go);

            return eventSystem;
        }

        private static bool HasInputSystemUIInputModule(GameObject go)
        {
            if (go == null)
            {
                return false;
            }

            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null)
                {
                    continue;
                }

                if (component.GetType().FullName == "UnityEngine.InputSystem.UI.InputSystemUIInputModule")
                {
                    return true;
                }
            }

            return false;
        }

        private static Component CreateInputSystemUIInputModule(GameObject go)
        {
            if (go == null)
            {
                return null;
            }

            var type = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (type == null)
            {
                TLogger.Warning("[UIManager] Unity.InputSystem is not available, cannot create InputSystemUIInputModule.");
                return null;
            }

            return go.AddComponent(type);
        }

        private EventSystem FindEventSystemUnderRoot(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            var eventSystems = Resources.FindObjectsOfTypeAll<EventSystem>();
            for (int i = 0; i < eventSystems.Length; i++)
            {
                var eventSystem = eventSystems[i];
                if (eventSystem == null)
                {
                    continue;
                }

                var go = eventSystem.gameObject;
                if (go == null || !go.scene.IsValid())
                {
                    continue;
                }

                if (go.transform.IsChildOf(root))
                {
                    return eventSystem;
                }
            }

            return null;
        }

        private UIRoot FindExistingUIRoot(out int count)
        {
            count = 0;
            UIRoot ddolRoot = null;
            UIRoot activeRoot = null;
            UIRoot fallbackRoot = null;

            var roots = Resources.FindObjectsOfTypeAll<UIRoot>();
            for (int i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                var go = root.gameObject;
                if (go == null || !go.scene.IsValid())
                {
                    continue;
                }

                count++;

                if (string.Equals(go.scene.name, DontDestroyOnLoadSceneName, StringComparison.Ordinal))
                {
                    ddolRoot = root;
                    continue;
                }

                if (activeRoot == null && go.activeInHierarchy)
                {
                    activeRoot = root;
                }

                if (fallbackRoot == null)
                {
                    fallbackRoot = root;
                }
            }

            return ddolRoot ?? activeRoot ?? fallbackRoot;
        }

        private void ReleaseUIRoot()
        {
            switch (_uiRootOrigin)
            {
                case UIRootOrigin.AddressableInstance:
                    _resourceService.ReleaseInstance(_uiRoot.gameObject);
                    break;
                case UIRootOrigin.ExistingSceneObject:
                case UIRootOrigin.DefaultCreated:
                    Object.Destroy(_uiRoot.gameObject);
                    break;
            }
        }

        private string GetPageAddress<TPage>() where TPage : UIPageBase
        {
            return _pageAddressMap.TryGetValue(typeof(TPage), out var address)
                ? address
                : typeof(TPage).Name;
        }

        private string GetDialogAddress<TDialog>()
        {
            return _dialogAddressMap.TryGetValue(typeof(TDialog), out var address)
                ? address
                : typeof(TDialog).Name;
        }

        private static UIDialogOpenParam ResolveDialogOpenParam(object param)
        {
            return param as UIDialogOpenParam ?? new UIDialogOpenParam(param);
        }

        private async UniTask<TDialog> GetOrCreateDialogAsync<TDialog>(string address, bool cacheOnClose, CancellationToken ct) where TDialog : Component
        {
            if (cacheOnClose &&
                _dialogCache.TryGetValue(address, out var cachedDialog) &&
                cachedDialog is TDialog cachedComponent &&
                cachedComponent != null &&
                cachedComponent.gameObject != null)
            {
                return cachedComponent;
            }

            TDialog dialog = await _resourceService.InstantiateAsync<TDialog>(address, _uiRoot.GetLayerContainer(UILayer.Dialog), ct);
            SetupRectTransform(dialog.transform as RectTransform);
            _container.InjectGameObject(dialog.gameObject);

            if (cacheOnClose && dialog is IUIDialog lifecycle)
            {
                _dialogCache[address] = lifecycle;
            }

            return dialog;
        }

        private void RemoveCachedDialog(string address, Component dialog)
        {
            if (dialog == null)
            {
                return;
            }

            if (_dialogCache.TryGetValue(address, out var cachedDialog) &&
                cachedDialog is Component cachedComponent &&
                cachedComponent == dialog)
            {
                _dialogCache.Remove(address);
            }
        }

        private async UniTaskVoid ShowLoadingInternal(string message)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_settings.LoadingDelay), cancellationToken: _cts.Token);
            if (_loadingRefCount > 0)
            {
                TLogger.Debug($"[UIManager] Loading shown: {message}");
            }
        }

        private async UniTaskVoid HideLoadingInternal()
        {
            await UniTask.Yield();
            TLogger.Debug("[UIManager] Loading hidden");
        }

        private void SetupRectTransform(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return;

            rectTransform.offsetMin = Vector2.zero;  // Left, Bottom = 0
            rectTransform.offsetMax = Vector2.zero;  // Right, Top = 0
        }
        #endregion

        #region Nested Types
        private sealed class LoadingScope : IDisposable
        {
            #region Private Fields
            private readonly UIManager _manager;
            private bool _disposed;
            #endregion

            #region Constructor
            public LoadingScope(UIManager manager)
            {
                _manager = manager;
            }
            #endregion

            #region IDisposable
            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _manager.HideLoading();
            }
            #endregion
        }
        #endregion
    }
}
