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
    /// dialog制御サービス
    /// </summary>
    internal sealed class UIDialogService
    {
        private readonly IResourceService _resourceService;
        private readonly IObjectResolver _container;
        private readonly UIRootService _rootService;
        private readonly IUIAnimation _transition;
        private readonly Func<CancellationToken> _managerTokenProvider;

        private readonly Dictionary<string, IUIDialog> _dialogCache = new();
        private readonly Dictionary<IUIDialog, string> _dialogOwnerSceneMap = new();

        public UIDialogService(
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
        /// 戻り値なしdialog表示
        /// </summary>
        public async UniTask ShowDialogAsync<TDialog>(string address, object param, CancellationToken ct)
            where TDialog : UIDialogBase
        {
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _managerTokenProvider());

            UIDialogOpenParam openParam = ResolveDialogOpenParam(param);
            bool cacheOnClose = openParam.CacheOnClose;
            TDialog dialog = null;
            bool completed = false;

            try
            {
                dialog = await GetOrCreateDialogAsync<TDialog>(address, cacheOnClose, linkedCts.Token);
                IUIDialog lifecycle = dialog;

                dialog.SetVisible(false);
                dialog.gameObject.SetActive(true);

                await lifecycle.OnPreOpenAsync(openParam.Payload, linkedCts.Token);
                await _transition.PlayShowAsync(dialog.CanvasGroup, linkedCts.Token);
                lifecycle.OnOpened();

                await dialog.WaitUntilClosedAsync(linkedCts.Token);

                await lifecycle.OnPreCloseAsync(linkedCts.Token);
                await _transition.PlayHideAsync(dialog.CanvasGroup, linkedCts.Token);
                lifecycle.OnClosed();
                completed = true;

                if (cacheOnClose)
                {
                    dialog.gameObject.SetActive(false);
                }
            }
            catch (OperationCanceledException)
            {
                TLogger.Debug($"[UIDialogService] ShowDialogAsync cancelled: {address}");
                throw;
            }
            catch (Exception ex)
            {
                TLogger.Error($"[UIDialogService] Failed to show dialog {address}: {ex.Message}");
            }
            finally
            {
                FinalizeDialog(address, dialog, cacheOnClose, completed);
            }
        }

        /// <summary>
        /// 戻り値ありdialog表示
        /// </summary>
        public async UniTask<TResult> ShowDialogAsync<TDialog, TResult>(string address, object param, CancellationToken ct)
            where TDialog : UIDialogBase<TResult>
        {
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _managerTokenProvider());

            UIDialogOpenParam openParam = ResolveDialogOpenParam(param);
            bool cacheOnClose = openParam.CacheOnClose;
            TDialog dialog = null;
            bool completed = false;

            try
            {
                dialog = await GetOrCreateDialogAsync<TDialog>(address, cacheOnClose, linkedCts.Token);
                IUIDialog lifecycle = dialog;

                dialog.SetVisible(false);
                dialog.gameObject.SetActive(true);

                await lifecycle.OnPreOpenAsync(openParam.Payload, linkedCts.Token);
                await _transition.PlayShowAsync(dialog.CanvasGroup, linkedCts.Token);
                lifecycle.OnOpened();

                TResult result = await dialog.WaitForResultAsync(linkedCts.Token);

                await lifecycle.OnPreCloseAsync(linkedCts.Token);
                await _transition.PlayHideAsync(dialog.CanvasGroup, linkedCts.Token);
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
                TLogger.Debug($"[UIDialogService] ShowDialogAsync cancelled: {address}");
                throw;
            }
            catch (Exception ex)
            {
                TLogger.Error($"[UIDialogService] Failed to show dialog {address}: {ex.Message}");
                return default;
            }
            finally
            {
                FinalizeDialog(address, dialog, cacheOnClose, completed);
            }
        }

        /// <summary>
        /// scene専用dialogのクリーンアップ
        /// </summary>
        public void CleanupSceneScopedDialogs(Scene activeScene)
        {
            string activeSceneKey = GetSceneKey(activeScene);
            HashSet<IUIDialog> uniqueDialogs = CollectUniqueDialogs();
            foreach (IUIDialog dialog in uniqueDialogs)
            {
                if (ShouldReleaseDialogOnSceneChange(dialog, activeSceneKey))
                {
                    ReleaseDialogInstance(dialog);
                }
            }

            PruneInvalidReferences();
        }

        /// <summary>
        /// 管理中dialog全破棄
        /// </summary>
        public void DisposeAll()
        {
            HashSet<IUIDialog> uniqueDialogs = CollectUniqueDialogs();
            foreach (IUIDialog dialog in uniqueDialogs)
            {
                ReleaseDialogInstance(dialog);
            }

            _dialogCache.Clear();
            _dialogOwnerSceneMap.Clear();
        }

        /// <summary>
        /// dialog取得または新規生成
        /// </summary>
        private async UniTask<TDialog> GetOrCreateDialogAsync<TDialog>(string address, bool cacheOnClose, CancellationToken ct) where TDialog : Component
        {
            if (cacheOnClose &&
                _dialogCache.TryGetValue(address, out IUIDialog cachedDialog) &&
                cachedDialog is TDialog cachedComponent &&
                IsDialogAlive(cachedDialog))
            {
                TrackDialogOwnership(cachedDialog);
                return cachedComponent;
            }

            if (cacheOnClose)
            {
                _dialogCache.Remove(address);
            }

            UIRoot root = _rootService.Root;
            if (root == null)
            {
                throw new InvalidOperationException("UIRoot is not ready");
            }

            TDialog dialog = await _resourceService.InstantiateAsync<TDialog>(address, root.GetLayerContainer(UILayer.Dialog), ct);
            SetupRectTransform(dialog.transform as RectTransform);
            _container.InjectGameObject(dialog.gameObject);

            if (cacheOnClose && dialog is IUIDialog lifecycle)
            {
                _dialogCache[address] = lifecycle;
                TrackDialogOwnership(lifecycle);
            }

            return dialog;
        }

        /// <summary>
        /// dialog終了整理
        /// </summary>
        private void FinalizeDialog<TDialog>(string address, TDialog dialog, bool cacheOnClose, bool completed)
            where TDialog : Component
        {
            if (dialog == null || dialog.gameObject == null)
            {
                return;
            }

            IUIDialog lifecycle = dialog as IUIDialog;
            if (lifecycle == null)
            {
                return;
            }

            if (!completed)
            {
                RemoveCachedDialog(address, dialog);
                lifecycle.OnTerminate();
                _resourceService.ReleaseInstance(dialog.gameObject);
                return;
            }

            if (!cacheOnClose)
            {
                lifecycle.OnTerminate();
                _resourceService.ReleaseInstance(dialog.gameObject);
            }
        }

        /// <summary>
        /// scene切り替え時の解放判定
        /// </summary>
        private bool ShouldReleaseDialogOnSceneChange(IUIDialog dialog, string activeSceneKey)
        {
            if (dialog == null)
            {
                return false;
            }

            if (dialog is IScenePersistentUI persistent && persistent.PersistAcrossScenes)
            {
                return false;
            }

            if (_dialogOwnerSceneMap.TryGetValue(dialog, out string ownerSceneKey))
            {
                return !string.Equals(ownerSceneKey, activeSceneKey, StringComparison.Ordinal);
            }

            return true;
        }

        /// <summary>
        /// scene所有記録
        /// </summary>
        private void TrackDialogOwnership(IUIDialog dialog)
        {
            if (dialog == null)
            {
                return;
            }

            _dialogOwnerSceneMap[dialog] = GetSceneKey(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// dialog最終解放
        /// </summary>
        private void ReleaseDialogInstance(IUIDialog dialog)
        {
            if (dialog == null)
            {
                return;
            }

            RemoveCachedDialog(dialog);
            _dialogOwnerSceneMap.Remove(dialog);

            if (!IsDialogAlive(dialog))
            {
                return;
            }

            Component component = dialog as Component;
            GameObject gameObject = component.gameObject;
            dialog.OnTerminate();
            _resourceService.ReleaseInstance(gameObject);
        }

        /// <summary>
        /// address指定キャッシュ参照の除去
        /// </summary>
        private void RemoveCachedDialog(string address, Component dialog)
        {
            if (dialog == null)
            {
                return;
            }

            if (_dialogCache.TryGetValue(address, out IUIDialog cachedDialog) &&
                cachedDialog is Component cachedComponent &&
                cachedComponent == dialog)
            {
                _dialogCache.Remove(address);
                _dialogOwnerSceneMap.Remove(cachedDialog);
            }
        }

        /// <summary>
        /// instance指定キャッシュ参照の除去
        /// </summary>
        private void RemoveCachedDialog(IUIDialog dialog)
        {
            if (dialog == null)
            {
                return;
            }

            string removeKey = null;
            foreach (KeyValuePair<string, IUIDialog> pair in _dialogCache)
            {
                if (pair.Value == dialog)
                {
                    removeKey = pair.Key;
                    break;
                }
            }

            if (removeKey != null)
            {
                _dialogCache.Remove(removeKey);
            }
        }

        /// <summary>
        /// 無効参照の整理
        /// </summary>
        private void PruneInvalidReferences()
        {
            List<string> invalidCacheKeys = new();
            foreach (KeyValuePair<string, IUIDialog> pair in _dialogCache)
            {
                if (!IsDialogAlive(pair.Value))
                {
                    invalidCacheKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < invalidCacheKeys.Count; i++)
            {
                _dialogCache.Remove(invalidCacheKeys[i]);
            }

            List<IUIDialog> invalidOwnerDialogs = new();
            foreach (KeyValuePair<IUIDialog, string> pair in _dialogOwnerSceneMap)
            {
                if (!IsDialogAlive(pair.Key))
                {
                    invalidOwnerDialogs.Add(pair.Key);
                }
            }

            for (int i = 0; i < invalidOwnerDialogs.Count; i++)
            {
                _dialogOwnerSceneMap.Remove(invalidOwnerDialogs[i]);
            }
        }

        /// <summary>
        /// 一意dialog集合生成
        /// </summary>
        private HashSet<IUIDialog> CollectUniqueDialogs()
        {
            HashSet<IUIDialog> uniqueDialogs = new();
            foreach (IUIDialog dialog in _dialogCache.Values)
            {
                if (dialog != null)
                {
                    uniqueDialogs.Add(dialog);
                }
            }

            return uniqueDialogs;
        }

        /// <summary>
        /// 表示引数解決
        /// </summary>
        private static UIDialogOpenParam ResolveDialogOpenParam(object param)
        {
            return param as UIDialogOpenParam ?? new UIDialogOpenParam(param);
        }

        /// <summary>
        /// dialog生存判定
        /// </summary>
        private static bool IsDialogAlive(IUIDialog dialog)
        {
            return dialog is Component component && component != null && component.gameObject != null;
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
