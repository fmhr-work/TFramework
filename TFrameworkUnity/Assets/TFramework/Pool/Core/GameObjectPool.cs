using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Debug;
using UnityEngine;

namespace TFramework.Pool
{
    /// <summary>
    /// 単一のGameObjectプール
    /// プレハブごとにインスタンスを管理する
    /// </summary>
    public class GameObjectPool : IDisposable
    {
        private readonly GameObject _prefab;
        private readonly Transform _poolRoot;
        private readonly int _maxSize;
        private readonly Stack<GameObject> _available;
        private readonly HashSet<GameObject> _active;

        private int _totalCreated;
        private int _totalSpawned;
        private int _totalDespawned;
        private bool _isDisposed;

        /// <summary>
        /// プールのキー
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="key">プールのキー</param>
        /// <param name="prefab">プレハブ</param>
        /// <param name="poolRoot">プール用のルートTransform</param>
        /// <param name="initialSize">初期サイズ</param>
        /// <param name="maxSize">最大サイズ</param>
        public GameObjectPool(string key, GameObject prefab, Transform poolRoot, int initialSize = 0, int maxSize = 100)
        {
            Key = key;
            _prefab = prefab;
            _poolRoot = poolRoot;
            _maxSize = maxSize;
            _available = new Stack<GameObject>(initialSize);
            _active = new HashSet<GameObject>();

            // 初期サイズ分のオブジェクトを生成
            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateObject();
                obj.SetActive(false);
                _available.Push(obj);
            }
        }

        /// <summary>
        /// プールからオブジェクトを取得する
        /// </summary>
        public GameObject Spawn(Vector3 position, Quaternion rotation, Transform parent)
        {
            if (_isDisposed)
            {
                TLogger.Error($"Attempting to spawn from disposed pool: {Key}", "Pool");
                return null;
            }

            GameObject obj;

            if (_available.Count > 0)
            {
                obj = _available.Pop();
            }
            else if (_active.Count < _maxSize)
            {
                obj = CreateObject();
            }
            else
            {
                TLogger.Warning($"Pool '{Key}' reached max size ({_maxSize}). Consider increasing pool size.", "Pool");
                obj = CreateObject();
            }

            // 位置と回転を設定
            var transform = obj.transform;
            transform.SetParent(parent);
            transform.position = position;
            transform.rotation = rotation;

            obj.SetActive(true);
            _active.Add(obj);
            _totalSpawned++;

            // IPoolableコールバックを呼び出す
            NotifySpawn(obj);

            return obj;
        }

        /// <summary>
        /// オブジェクトをプールに返却する
        /// </summary>
        public void Despawn(GameObject obj)
        {
            if (_isDisposed || obj == null)
                return;

            if (!_active.Contains(obj))
            {
                TLogger.Warning($"Attempting to despawn object not from this pool: {obj.name}", "Pool");
                return;
            }

            // IPoolableコールバックを呼び出す
            NotifyDespawn(obj);

            obj.SetActive(false);
            obj.transform.SetParent(_poolRoot);

            _active.Remove(obj);

            // 最大サイズを超えている場合は破棄
            if (_available.Count >= _maxSize)
            {
                UnityEngine.Object.Destroy(obj);
            }
            else
            {
                _available.Push(obj);
            }

            _totalDespawned++;
        }

        /// <summary>
        /// 指定した数のオブジェクトを事前に生成する
        /// </summary>
        public async UniTask PrewarmAsync(int count, CancellationToken ct)
        {
            int toCreate = Math.Min(count, _maxSize - _available.Count - _active.Count);

            for (int i = 0; i < toCreate; i++)
            {
                ct.ThrowIfCancellationRequested();

                var obj = CreateObject();
                obj.SetActive(false);
                _available.Push(obj);

                // フレームをまたいで負荷を分散
                if (i % 10 == 9)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }

            TLogger.Debug($"Pool '{Key}' prewarmed with {toCreate} objects.", "Pool");
        }

        /// <summary>
        /// プールの統計情報を取得する
        /// </summary>
        public PoolStats GetStats()
        {
            return new PoolStats
            {
                AvailableCount = _available.Count,
                ActiveCount = _active.Count,
                MaxSize = _maxSize,
                TotalCreated = _totalCreated,
                TotalSpawned = _totalSpawned,
                TotalDespawned = _totalDespawned
            };
        }

        /// <summary>
        /// プールをクリアする
        /// </summary>
        public void Clear()
        {
            // アクティブなオブジェクトを破棄
            foreach (var obj in _active)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
            _active.Clear();

            // 利用可能なオブジェクトを破棄
            while (_available.Count > 0)
            {
                var obj = _available.Pop();
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            Clear();
            _isDisposed = true;
        }

        private GameObject CreateObject()
        {
            var obj = UnityEngine.Object.Instantiate(_prefab, _poolRoot);
            obj.name = $"{_prefab.name}_{_totalCreated}";
            _totalCreated++;
            return obj;
        }

        private static void NotifySpawn(GameObject obj)
        {
            var poolables = obj.GetComponents<IPoolable>();
            foreach (var poolable in poolables)
            {
                poolable.OnSpawn();
            }
        }

        private static void NotifyDespawn(GameObject obj)
        {
            var poolables = obj.GetComponents<IPoolable>();
            foreach (var poolable in poolables)
            {
                poolable.OnDespawn();
            }
        }
    }
}
