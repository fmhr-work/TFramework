using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Debug;
using TFramework.Resource;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace TFramework.UI
{
    /// <summary>
    /// page制御サービス
    /// </summary>
    internal sealed class UIPageService
    {
        private readonly IResourceService _resourceService;
        private readonly IObjectResolver _container;
        private readonly UIRootService _rootService;
        private readonly IUIAnimation _transition;
        private readonly Func<CancellationToken> _managerTokenProvider;

        private readonly Dictionary<string, UIPageBase> _pageCache = new();
        private readonly Dictionary<UIPageBase, string> _pageOwnerSceneMap = new();
        private readonly Stack<UIPageBase> _pageStack = new();

        public UIPageService(
            IResourceService resourceService,
            IObjectResolver container,
            UIRootService rootService,
            IUIAnimation transition,
            Func<CancellationToken> managerTokenProvider)
        {
            _resourceService = resourceService;
            _container = container;
            _rootService = rootService;
            _transition = transition;
            _managerTokenProvider = managerTokenProvider;
        }

        /// <summary>
        /// 現在表示page
        /// </summary>
        public UIPageBase CurrentPage => _pageStack.Count > 0 ? _pageStack.Peek() : null;

        /// <summary>
        /// スタック件数
        /// </summary>
        public int PageStackCount => _pageStack.Count;

        /// <summary>
        /// page表示処理
        /// </summary>
        public async UniTask ShowPageAsync(string address, object param, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(address))
            {
                TLogger.Error("[UIPageService] Page address is null or empty");
                return;
            }

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _managerTokenProvider());

            try
            {
                PruneInvalidReferences();

                UIPageBase currentPage = CurrentPage;
                if (currentPage != null)
                {
                    await ClosePageAsync(currentPage, true, linkedCts.Token);
                }

                UIPageBase page = await GetOrCreatePageAsync(address, linkedCts.Token);
                IUIPageLifecycle lifecycle = (IUIPageLifecycle)page;
                if (!page.IsInitialized)
                {
                    await lifecycle.OnInitializeAsync(linkedCts.Token);
                }

                page.SetVisible(false);
                page.gameObject.SetActive(true);

                await lifecycle.OnPreOpenAsync(param, linkedCts.Token);
                await _transition.PlayShowAsync(page.CanvasGroup, linkedCts.Token);
                lifecycle.OnOpened();

                RemovePageFromStack(page);
                _pageStack.Push(page);
            }
            catch (OperationCanceledException)
            {
                TLogger.Debug($"[UIPageService] ShowPageAsync cancelled: {address}");
                throw;
            }
            catch (Exception ex)
            {
                TLogger.Error($"[UIPageService] Failed to show page {address}: {ex.Message}");
            }
        }

        /// <summary>
        /// 前page復帰
        /// </summary>
        public async UniTask<bool> GoBackAsync(CancellationToken ct)
        {
            PruneInvalidReferences();
            if (_pageStack.Count <= 1)
            {
                return false;
            }

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _managerTokenProvider());

            UIPageBase currentPage = _pageStack.Pop();
            if (_pageStack.Count == 0)
            {
                _pageStack.Push(currentPage);
                return false;
            }

            try
            {
                await ClosePageAsync(currentPage, true, linkedCts.Token);

                PruneInvalidReferences();
                if (_pageStack.Count == 0)
                {
                    return false;
                }

                UIPageBase previousPage = _pageStack.Peek();
                IUIPageLifecycle prevLifecycle = (IUIPageLifecycle)previousPage;

                previousPage.SetVisible(false);
                previousPage.gameObject.SetActive(true);

                await prevLifecycle.OnPreOpenAsync(null, linkedCts.Token);
                await _transition.PlayShowAsync(previousPage.CanvasGroup, linkedCts.Token);
                prevLifecycle.OnOpened();

                return true;
            }
            catch (OperationCanceledException)
            {
                TLogger.Debug("[UIPageService] GoBackAsync cancelled");
                throw;
            }
            catch (Exception ex)
            {
                TLogger.Error($"[UIPageService] Failed to go back: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ページスタック消去
        /// </summary>
        public UniTask ClearStackAsync(CancellationToken ct)
        {
            PruneInvalidReferences();

            HashSet<UIPageBase> visitedPages = new();
            while (_pageStack.Count > 0)
            {
                UIPageBase page = _pageStack.Pop();
                if (page == null || !visitedPages.Add(page))
                {
                    continue;
                }

                ReleaseOrHidePage(page, true);
            }

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// scene専用pageのクリーンアップ
        /// </summary>
        public void CleanupSceneScopedPages(Scene activeScene)
        {
            string activeSceneKey = GetSceneKey(activeScene);
            HashSet<UIPageBase> uniquePages = CollectUniquePages();
            foreach (UIPageBase page in uniquePages)
            {
                if (ShouldReleasePageOnSceneChange(page, activeSceneKey))
                {
                    ReleasePageInstance(page);
                }
            }

            PruneInvalidReferences();
        }

        /// <summary>
        /// 管理中page全破棄
        /// </summary>
        public void DisposeAll()
        {
            HashSet<UIPageBase> uniquePages = CollectUniquePages();
            foreach (UIPageBase page in uniquePages)
            {
                ReleasePageInstance(page);
            }

            _pageCache.Clear();
            _pageOwnerSceneMap.Clear();
            _pageStack.Clear();
        }

        /// <summary>
        /// page取得または新規生成
        /// </summary>
        private async UniTask<UIPageBase> GetOrCreatePageAsync(string address, CancellationToken ct)
        {
            if (_pageCache.TryGetValue(address, out UIPageBase cachedPage) && IsPageAlive(cachedPage))
            {
                TrackPageOwnership(cachedPage);
                return cachedPage;
            }

            _pageCache.Remove(address);

            UIRoot root = _rootService.Root;
            if (root == null)
            {
                throw new InvalidOperationException("UIRoot is not ready");
            }

            UIPageBase page = await _resourceService.InstantiateAsync<UIPageBase>(address, root.GetLayerContainer(UILayer.Page), ct);
            SetupRectTransform(page.transform as RectTransform);
            _container.InjectGameObject(page.gameObject);

            if (page.CacheOnClose)
            {
                _pageCache[address] = page;
            }

            TrackPageOwnership(page);
            return page;
        }

        /// <summary>
        /// pageの閉じる処理
        /// </summary>
        private async UniTask ClosePageAsync(UIPageBase page, bool keepCachedInstance, CancellationToken ct)
        {
            if (!IsPageAlive(page))
            {
                RemovePageFromCache(page);
                _pageOwnerSceneMap.Remove(page);
                return;
            }

            IUIPageLifecycle lifecycle = (IUIPageLifecycle)page;
            await lifecycle.OnPreCloseAsync(ct);
            await _transition.PlayHideAsync(page.CanvasGroup, ct);
            lifecycle.OnClosed();
            ReleaseOrHidePage(page, keepCachedInstance);
        }

        /// <summary>
        /// page非表示または解放
        /// </summary>
        private void ReleaseOrHidePage(UIPageBase page, bool keepCachedInstance)
        {
            if (!IsPageAlive(page))
            {
                RemovePageFromCache(page);
                _pageOwnerSceneMap.Remove(page);
                return;
            }

            if (page.CacheOnClose && keepCachedInstance)
            {
                page.gameObject.SetActive(false);
                return;
            }

            ReleasePageInstance(page);
        }

        /// <summary>
        /// page最終解放
        /// </summary>
        private void ReleasePageInstance(UIPageBase page)
        {
            if (page == null)
            {
                return;
            }

            RemovePageFromCache(page);
            _pageOwnerSceneMap.Remove(page);
            RemovePageFromStack(page);

            if (!IsPageAlive(page))
            {
                return;
            }

            ((IUIPageLifecycle)page).OnTerminate();
            _resourceService.ReleaseInstance(page.gameObject);
        }

        /// <summary>
        /// scene切り替え時の解放判定
        /// </summary>
        private bool ShouldReleasePageOnSceneChange(UIPageBase page, string activeSceneKey)
        {
            if (page == null)
            {
                return false;
            }

            if (page is IScenePersistentUI persistent && persistent.PersistAcrossScenes)
            {
                return false;
            }

            if (_pageOwnerSceneMap.TryGetValue(page, out string ownerSceneKey))
            {
                return !string.Equals(ownerSceneKey, activeSceneKey, StringComparison.Ordinal);
            }

            return true;
        }

        /// <summary>
        /// scene所有記録
        /// </summary>
        private void TrackPageOwnership(UIPageBase page)
        {
            if (page == null)
            {
                return;
            }

            _pageOwnerSceneMap[page] = GetSceneKey(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// stack内重複除去
        /// </summary>
        private void RemovePageFromStack(UIPageBase targetPage)
        {
            if (targetPage == null || _pageStack.Count == 0)
            {
                return;
            }

            UIPageBase[] pages = _pageStack.ToArray();
            _pageStack.Clear();

            for (int i = pages.Length - 1; i >= 0; i--)
            {
                UIPageBase page = pages[i];
                if (page == null || page == targetPage)
                {
                    continue;
                }

                _pageStack.Push(page);
            }
        }

        /// <summary>
        /// キャッシュ参照除去
        /// </summary>
        private void RemovePageFromCache(UIPageBase page)
        {
            if (page == null)
            {
                return;
            }

            string removeKey = null;
            foreach (KeyValuePair<string, UIPageBase> pair in _pageCache)
            {
                if (pair.Value == page)
                {
                    removeKey = pair.Key;
                    break;
                }
            }

            if (removeKey != null)
            {
                _pageCache.Remove(removeKey);
            }
        }

        /// <summary>
        /// 無効参照整理
        /// </summary>
        private void PruneInvalidReferences()
        {
            List<string> invalidCacheKeys = new();
            foreach (KeyValuePair<string, UIPageBase> pair in _pageCache)
            {
                if (!IsPageAlive(pair.Value))
                {
                    invalidCacheKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < invalidCacheKeys.Count; i++)
            {
                _pageCache.Remove(invalidCacheKeys[i]);
            }

            List<UIPageBase> invalidOwnerPages = new();
            foreach (KeyValuePair<UIPageBase, string> pair in _pageOwnerSceneMap)
            {
                if (!IsPageAlive(pair.Key))
                {
                    invalidOwnerPages.Add(pair.Key);
                }
            }

            for (int i = 0; i < invalidOwnerPages.Count; i++)
            {
                _pageOwnerSceneMap.Remove(invalidOwnerPages[i]);
            }

            UIPageBase[] pages = _pageStack.ToArray();
            HashSet<UIPageBase> seenPages = new();
            _pageStack.Clear();

            for (int i = pages.Length - 1; i >= 0; i--)
            {
                UIPageBase page = pages[i];
                if (!IsPageAlive(page) || !seenPages.Add(page))
                {
                    continue;
                }

                _pageStack.Push(page);
            }
        }

        /// <summary>
        /// 一意page集合生成
        /// </summary>
        private HashSet<UIPageBase> CollectUniquePages()
        {
            HashSet<UIPageBase> uniquePages = new();

            foreach (UIPageBase page in _pageCache.Values)
            {
                if (page != null)
                {
                    uniquePages.Add(page);
                }
            }

            foreach (UIPageBase page in _pageStack)
            {
                if (page != null)
                {
                    uniquePages.Add(page);
                }
            }

            return uniquePages;
        }

        /// <summary>
        /// page生存判定
        /// </summary>
        private static bool IsPageAlive(UIPageBase page)
        {
            return page != null && page.gameObject != null;
        }

        /// <summary>
        /// scene識別子生成
        /// </summary>
        private static string GetSceneKey(Scene scene)
        {
            return string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
        }

        /// <summary>
        /// 全画面RectTransform設定
        /// </summary>
        private static void SetupRectTransform(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
