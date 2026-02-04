using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Core;
using TFramework.Debug;
using UnityEngine;
using VContainer;

namespace TFramework.Pool
{
    /// <summary>
    /// プール管理サービスの基本実装
    /// 複数のGameObjectPoolを管理する
    /// </summary>
    public class PoolManager : IPoolManager, IDisposableService
    {
        private readonly Dictionary<string, GameObjectPool> _pools = new();
        private readonly Dictionary<GameObject, string> _objectToPoolKey = new();
        private readonly TFrameworkSettings _settings;
        private Transform _poolRoot;
        private bool _isDisposed;

        [Inject]
        public PoolManager(TFrameworkSettings settings = null)
        {
            _settings = settings ?? TFrameworkSettings.Instance;
            InitializePoolRoot();
        }

        private void InitializePoolRoot()
        {
            var rootGO = new GameObject("[TFramework] Pool Root");
            UnityEngine.Object.DontDestroyOnLoad(rootGO);
            _poolRoot = rootGO.transform;
        }

        /// <inheritdoc/>
        public void CreatePool(string key, GameObject prefab, int initialSize = 0, int maxSize = 100)
        {
            if (_isDisposed)
            {
                TLogger.Error("Attempting to create pool on disposed PoolManager.", "Pool");
                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                TLogger.Error("Pool key cannot be null or empty.", "Pool");
                return;
            }

            if (prefab == null)
            {
                TLogger.Error($"Prefab for pool '{key}' cannot be null.", "Pool");
                return;
            }

            if (_pools.ContainsKey(key))
            {
                TLogger.Warning($"Pool '{key}' already exists. Skipping creation.", "Pool");
                return;
            }

            // デフォルト値を設定から取得
            if (initialSize <= 0)
            {
                initialSize = _settings.DefaultPoolInitialSize;
            }

            if (maxSize <= 0)
            {
                maxSize = _settings.DefaultPoolMaxSize;
            }

            // プール用のルートオブジェクトを作成
            var poolRootGO = new GameObject($"Pool_{key}");
            poolRootGO.transform.SetParent(_poolRoot);

            var pool = new GameObjectPool(key, prefab, poolRootGO.transform, initialSize, maxSize);
            _pools[key] = pool;

            TLogger.Debug($"Created pool '{key}' (initial: {initialSize}, max: {maxSize})", "Pool");
        }

        /// <inheritdoc/>
        public GameObject Spawn(string key, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            if (_isDisposed)
            {
                TLogger.Error("Attempting to spawn from disposed PoolManager.", "Pool");
                return null;
            }

            if (!_pools.TryGetValue(key, out var pool))
            {
                TLogger.Error($"Pool '{key}' not found. Create the pool first.", "Pool");
                return null;
            }

            var obj = pool.Spawn(position, rotation, parent);
            if (obj != null)
            {
                _objectToPoolKey[obj] = key;
            }

            return obj;
        }

        /// <inheritdoc/>
        public T Spawn<T>(string key, Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : Component
        {
            var obj = Spawn(key, position, rotation, parent);
            return obj != null ? obj.GetComponent<T>() : null;
        }

        /// <inheritdoc/>
        public void Despawn(GameObject obj, float delay = 0f)
        {
            if (_isDisposed || obj == null)
                return;

            if (delay > 0f)
            {
                DespawnDelayedAsync(obj, delay, default).Forget();
                return;
            }

            DespawnImmediate(obj);
        }

        private async UniTaskVoid DespawnDelayedAsync(GameObject obj, float delay, CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);

            if (obj != null && !_isDisposed)
            {
                DespawnImmediate(obj);
            }
        }

        private void DespawnImmediate(GameObject obj)
        {
            if (!_objectToPoolKey.TryGetValue(obj, out var key))
            {
                TLogger.Warning($"Object '{obj.name}' is not managed by any pool. Destroying instead.", "Pool");
                UnityEngine.Object.Destroy(obj);
                return;
            }

            if (_pools.TryGetValue(key, out var pool))
            {
                pool.Despawn(obj);
            }

            _objectToPoolKey.Remove(obj);
        }

        /// <inheritdoc/>
        public async UniTask PrewarmAsync(string key, int count, CancellationToken ct)
        {
            if (!_pools.TryGetValue(key, out var pool))
            {
                TLogger.Error($"Pool '{key}' not found.", "Pool");
                return;
            }

            await pool.PrewarmAsync(count, ct);
        }

        /// <inheritdoc/>
        public void ClearPool(string key)
        {
            if (_pools.TryGetValue(key, out var pool))
            {
                pool.Clear();

                // オブジェクトからプールキーへのマッピングもクリア
                var keysToRemove = new List<GameObject>();
                foreach (var kvp in _objectToPoolKey)
                {
                    if (kvp.Value == key)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var objKey in keysToRemove)
                {
                    _objectToPoolKey.Remove(objKey);
                }

                TLogger.Debug($"Cleared pool '{key}'", "Pool");
            }
        }

        /// <inheritdoc/>
        public void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }

            _objectToPoolKey.Clear();
            TLogger.Debug("Cleared all pools", "Pool");
        }

        /// <inheritdoc/>
        public PoolStats GetStats(string key)
        {
            if (_pools.TryGetValue(key, out var pool))
            {
                return pool.GetStats();
            }

            return default;
        }

        /// <inheritdoc/>
        public bool HasPool(string key)
        {
            return _pools.ContainsKey(key);
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            foreach (var pool in _pools.Values)
            {
                pool.Dispose();
            }

            _pools.Clear();
            _objectToPoolKey.Clear();

            if (_poolRoot != null)
            {
                UnityEngine.Object.Destroy(_poolRoot.gameObject);
            }

            _isDisposed = true;
            TLogger.Debug("PoolManager disposed", "Pool");
        }
    }
}
