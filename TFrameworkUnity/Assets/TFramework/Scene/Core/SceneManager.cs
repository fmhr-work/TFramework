using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TFramework.Core;
using TFramework.Debug;
using TFramework.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using VContainer;

namespace TFramework.Scene
{
    /// <summary>
    /// シーン遷移管理の実装
    /// </summary>
    public sealed class SceneManager : ISceneService, IInitializable, IDisposable
    {
        private readonly IUIService _uiService;
        private readonly SceneSettings _settings;
        private readonly ReactiveProperty<string> _currentScene = new();
        private readonly Dictionary<string, ISceneBridgeData> _paramCache = new();
        private readonly Dictionary<string, SceneInstance> _loadedScenes = new();

        private CancellationTokenSource _cts;
        private bool _isDisposed;

        public ReadOnlyReactiveProperty<string> CurrentScene => _currentScene;

        [Inject]
        public SceneManager(IUIService uiService)
        {
            _uiService = uiService;
            _settings = SceneSettings.Instance;
            _cts = new CancellationTokenSource();
        }

        public async UniTask InitializeAsync(CancellationToken ct)
        {
            // Addressables初期化
            await Addressables.InitializeAsync().ToUniTask(cancellationToken: ct);
            
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            _currentScene.Value = activeScene.name;
            TLogger.Info($"[SceneManager] Initialized. Current Scene: {activeScene.name}");
        }

        public async UniTask LoadSceneAsync(string sceneAddress, ISceneBridgeData param = null, bool useLoading = true, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(sceneAddress)) return;
            if (_currentScene.Value == sceneAddress)
            {
                TLogger.Warning($"[SceneManager] Scene {sceneAddress} is already active.");
                return;
            }

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
            var token = linkedCts.Token;

            // ローディング開始
            IDisposable loadingHandle = null;
            if (useLoading)
            {
                loadingHandle = _uiService.ShowLoading();
            }

            try
            {
                TLogger.Info($"[SceneManager] Loading scene: {sceneAddress}");

                // LoadSceneMode.Single の場合、現在のシーンを正しくアンロードする
                if (!string.IsNullOrEmpty(_currentScene.Value) && _loadedScenes.TryGetValue(_currentScene.Value, out var oldSceneInstance))
                {
                    TLogger.Info($"[SceneManager] Unloading previous scene: {_currentScene.Value}");
                    await Addressables.UnloadSceneAsync(oldSceneInstance).ToUniTask(cancellationToken: token);
                    _loadedScenes.Remove(_currentScene.Value);
                }

                // パラメータをキャッシュ（ライフサイクルで使用）
                if (param != null)
                {
                    _paramCache[sceneAddress] = param;
                }
                
                // Addressablesでシーンロード (Single モード)
                var handle = Addressables.LoadSceneAsync(sceneAddress);
                var sceneInstance = await handle.ToUniTask(cancellationToken: token);

                // SceneInstance を保存（アンロード時に使用）
                var sceneName = sceneInstance.Scene.name;
                _loadedScenes[sceneName] = sceneInstance;

                // アクティブシーン更新
                _currentScene.Value = sceneName;
                
                // 最小ローディング時間待機（オプション）
                if (useLoading)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_settings.MinLoadingDisplayTime), cancellationToken: token);
                }
            }
            catch (OperationCanceledException)
            {
                TLogger.Info($"[SceneManager] Load scene cancelled: {sceneAddress}");
                throw;
            }
            catch (Exception ex)
            {
                TLogger.Error($"[SceneManager] Failed to load scene {sceneAddress}: {ex}");
                throw;
            }
            finally
            {
                loadingHandle?.Dispose();
            }
        }

        public async UniTask LoadSceneAdditiveAsync(string sceneAddress, ISceneBridgeData param = null, CancellationToken ct = default)
        {
             if (param != null) _paramCache[sceneAddress] = param;
             
             var handle = Addressables.LoadSceneAsync(sceneAddress, LoadSceneMode.Additive);
             var sceneInstance = await handle.ToUniTask(cancellationToken: ct);
             
             // SceneInstance を保存
             var sceneName = sceneInstance.Scene.name;
             _loadedScenes[sceneName] = sceneInstance;
             
             TLogger.Info($"[SceneManager] Scene '{sceneName}' loaded additively.");
        }

        public async UniTask UnloadSceneAsync(string sceneName, CancellationToken ct = default)
        {
            if (!_loadedScenes.TryGetValue(sceneName, out var sceneInstance))
            {
                TLogger.Warning($"[SceneManager] Scene '{sceneName}' not found in loaded scenes. Cannot unload.");
                return;
            }

            // Addressables の正しい方法でアンロード（参照カウント管理）
            await Addressables.UnloadSceneAsync(sceneInstance).ToUniTask(cancellationToken: ct);
            _loadedScenes.Remove(sceneName);
            _paramCache.Remove(sceneName);
            
            TLogger.Info($"[SceneManager] Scene '{sceneName}' unloaded successfully.");
        }
        
        /// <summary>
        /// シーンパラメータを取得（ライフサイクルコンポーネント用）
        /// </summary>
        public ISceneBridgeData GetSceneBridgeData(string sceneName)
        {
            return _paramCache.Remove(sceneName, out var param) ? param : null;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            // すべてのロード済みシーンをアンロード
            foreach (var kvp in _loadedScenes)
            {
                try
                {
                    Addressables.UnloadSceneAsync(kvp.Value);
                    TLogger.Info($"[SceneManager] Unloaded scene '{kvp.Key}' during disposal.");
                }
                catch (Exception ex)
                {
                    TLogger.Error($"[SceneManager] Failed to unload scene '{kvp.Key}': {ex}");
                }
            }
            _loadedScenes.Clear();
            
            _cts?.Cancel();
            _cts?.Dispose();
            _currentScene.Dispose();
            _paramCache.Clear();
        }
    }
}
