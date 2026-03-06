using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Core;
using TFramework.Debug;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;
using VContainer;

namespace TFramework.Resource
{
    /// <summary>
    /// Addressablesを使用したリソースサービス実装
    /// </summary>
    public class AddressableResourceService : IResourceService, IDisposableService, IInitializable
    {
        private readonly TFrameworkSettings _settings;
        private readonly Dictionary<string, AsyncOperationHandle> _loadedAssets = new();
        private readonly Dictionary<string, int> _referenceCount = new();
        private readonly object _lock = new();
        private bool _isInitialized;
        private bool _isDisposed;

        [Inject]
        public AddressableResourceService(TFrameworkSettings settings = null)
        {
            _settings = settings ?? TFrameworkSettings.Instance;
        }

        /// <inheritdoc/>
        public async UniTask InitializeAsync(CancellationToken ct)
        {
            if (_isInitialized) return;

            try
            {
                // Addressablesの初期化
                var handle = Addressables.InitializeAsync();
                await handle.ToUniTask(cancellationToken: ct);

                _isInitialized = true;
                TLogger.Info("AddressableResourceService initialized.", "Resource");
            }
            catch (Exception ex)
            {
                TLogger.Error("Failed to initialize Addressables.", ex, "Resource");
                throw;
            }
        }

        /// <inheritdoc/>
        public bool IsLoaded(string address)
        {
            lock (_lock)
            {
                return _loadedAssets.ContainsKey(address);
            }
        }

        /// <inheritdoc/>
        public async UniTask<bool> ExistsAsync(string address, CancellationToken ct)
        {
            try
            {
                var handle = Addressables.LoadResourceLocationsAsync(address);
                await handle.ToUniTask(cancellationToken: ct);
                IList<IResourceLocation> locations = handle.Result;
                return locations != null && locations.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public async UniTask<T> LoadAsync<T>(string address, CancellationToken ct) where T : UnityEngine.Object
        {
            var handle = await LoadWithHandleAsync<T>(address, ct);
            return handle.Asset;
        }

        /// <inheritdoc/>
        public async UniTask<IAssetHandle<T>> LoadWithHandleAsync<T>(string address, CancellationToken ct) where T : UnityEngine.Object
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AddressableResourceService));
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<T>(address);
                await handle.ToUniTask(cancellationToken: ct);

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to load asset: {address}");
                }

                // 参照カウントを増加
                lock (_lock)
                {
                    if (!_loadedAssets.ContainsKey(address))
                    {
                        _loadedAssets[address] = handle;
                        _referenceCount[address] = 0;
                    }
                    _referenceCount[address]++;
                }

                TLogger.Debug($"Loaded asset: {address} (ref: {_referenceCount[address]})", "Resource");

                return new AssetHandle<T>(handle, address, OnHandleReleased);
            }
            catch (Exception ex)
            {
                TLogger.Error($"Failed to load asset: {address}", ex, "Resource");
                throw;
            }
        }

        private void OnHandleReleased<T>(AssetHandle<T> handle) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(handle.Address)) return;

            lock (_lock)
            {
                if (_referenceCount.TryGetValue(handle.Address, out var count))
                {
                    count--;
                    _referenceCount[handle.Address] = count;

                    TLogger.Trace($"Asset reference decreased: {handle.Address} (ref: {count})", "Resource");

                    // 参照がなくなったら解放対象としてマーク
                    // 即座に解放せず、UnloadUnusedAssetsで解放する
                }
            }
        }

        /// <inheritdoc/>
        public async UniTask<GameObject> InstantiateAsync(string address, Transform parent, CancellationToken ct)
        {
            return await InstantiateAsync(address, Vector3.zero, Quaternion.identity, parent, ct);
        }

        /// <inheritdoc/>
        public async UniTask<GameObject> InstantiateAsync(string address, Vector3 position, Quaternion rotation, Transform parent, CancellationToken ct)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AddressableResourceService));
            }

            try
            {
                var handle = Addressables.InstantiateAsync(address, position, rotation, parent);
                var instance = await handle.ToUniTask(cancellationToken: ct);

                if (instance == null)
                {
                    throw new InvalidOperationException($"Failed to instantiate: {address}");
                }

                TLogger.Debug($"Instantiated: {address}", "Resource");

                return instance;
            }
            catch (Exception ex)
            {
                TLogger.Error($"Failed to instantiate: {address}", ex, "Resource");
                throw;
            }
        }

        /// <inheritdoc/>
        public async UniTask<T> InstantiateAsync<T>(string address, Transform parent, CancellationToken ct) where T : Component
        {
            return await InstantiateAsync<T>(address, Vector3.zero, Quaternion.identity, parent, ct);
        }

        /// <inheritdoc/>
        public async UniTask<T> InstantiateAsync<T>(string address, Vector3 position, Quaternion rotation, Transform parent, CancellationToken ct) where T : Component
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AddressableResourceService));
            }

            try
            {
                var handle = Addressables.InstantiateAsync(address, position, rotation, parent);
                var instance = await handle.ToUniTask(cancellationToken: ct);

                if (instance == null)
                {
                    throw new InvalidOperationException($"Failed to instantiate: {address}");
                }

                if (!instance.TryGetComponent<T>(out var component))
                {
                    Addressables.ReleaseInstance(instance);
                    throw new InvalidOperationException($"Component {typeof(T).Name} not found on prefab: {address}");
                }

                TLogger.Debug($"Instantiated with component: {address} ({typeof(T).Name})", "Resource");

                return component;
            }
            catch (Exception ex)
            {
                TLogger.Error($"Failed to instantiate with component {typeof(T).Name}: {address}", ex, "Resource");
                throw;
            }
        }

        /// <inheritdoc/>
        public async UniTask LoadSceneAsync(string address, LoadSceneMode mode, CancellationToken ct)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AddressableResourceService));
            }

            try
            {
                var handle = Addressables.LoadSceneAsync(address, mode);
                await handle.ToUniTask(cancellationToken: ct);

                TLogger.Info($"Loaded scene: {address}", "Resource");
            }
            catch (Exception ex)
            {
                TLogger.Error($"Failed to load scene: {address}", ex, "Resource");
                throw;
            }
        }

        /// <inheritdoc/>
        public void Release(object handle)
        {
            if (handle is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <inheritdoc/>
        public void ReleaseByAddress(string address)
        {
            lock (_lock)
            {
                if (_loadedAssets.TryGetValue(address, out var handle))
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }

                    _loadedAssets.Remove(address);
                    _referenceCount.Remove(address);

                    TLogger.Debug($"Released by address: {address}", "Resource");
                }
            }
        }

        /// <inheritdoc/>
        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null)
            {
                TLogger.Warning("Attempted to release null GameObject instance", "Resource");
                return;
            }

            try
            {
                Addressables.ReleaseInstance(instance);
                TLogger.Debug($"Released instance: {instance.name}", "Resource");
            }
            catch (Exception ex)
            {
                TLogger.Error($"Failed to release instance: {instance.name}", ex, "Resource");
            }
        }

        /// <inheritdoc/>
        public async UniTask<long> GetDownloadSizeAsync(string address, CancellationToken ct)
        {
            try
            {
                var handle = Addressables.GetDownloadSizeAsync(address);
                return await handle.ToUniTask(cancellationToken: ct);
            }
            catch (Exception ex)
            {
                TLogger.Error($"Failed to get download size: {address}", ex, "Resource");
                return 0;
            }
        }

        /// <inheritdoc/>
        public async UniTask DownloadAsync(string address, IProgress<float> progress, CancellationToken ct)
        {
            try
            {
                var handle = Addressables.DownloadDependenciesAsync(address);

                while (!handle.IsDone)
                {
                    ct.ThrowIfCancellationRequested();
                    progress?.Report(handle.PercentComplete);
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                progress?.Report(1f);

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to download: {address}");
                }

                Addressables.Release(handle);

                TLogger.Info($"Downloaded: {address}", "Resource");
            }
            catch (Exception ex)
            {
                TLogger.Error($"Failed to download: {address}", ex, "Resource");
                throw;
            }
        }

        /// <inheritdoc/>
        public async UniTask DownloadByLabelAsync(string label, IProgress<float> progress, CancellationToken ct)
        {
            await DownloadAsync(label, progress, ct);
        }

        /// <inheritdoc/>
        public void UnloadUnusedAssets()
        {
            lock (_lock)
            {
                var toRemove = new List<string>();

                foreach (var kvp in _referenceCount)
                {
                    if (kvp.Value <= 0)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (var address in toRemove)
                {
                    if (_loadedAssets.TryGetValue(address, out var handle))
                    {
                        if (handle.IsValid())
                        {
                            Addressables.Release(handle);
                        }

                        _loadedAssets.Remove(address);
                        _referenceCount.Remove(address);

                        TLogger.Trace($"Unloaded unused asset: {address}", "Resource");
                    }
                }

                if (toRemove.Count > 0)
                {
                    TLogger.Debug($"Unloaded {toRemove.Count} unused assets.", "Resource");
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            lock (_lock)
            {
                foreach (var handle in _loadedAssets.Values)
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                }

                _loadedAssets.Clear();
                _referenceCount.Clear();
            }

            _isDisposed = true;
            TLogger.Debug("AddressableResourceService disposed.", "Resource");
        }
    }
}
