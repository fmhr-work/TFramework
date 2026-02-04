using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace TFramework.Core
{
    /// <summary>
    /// TFrameworkの起動処理を管理するMonoBehaviour
    /// シーンに配置してフレームワークを初期化する
    /// </summary>
    public class TFrameworkBootstrap : MonoBehaviour
    {
        [SerializeField] private TFrameworkSettings _settings;

        private IObjectResolver _container;
        private CancellationTokenSource _cts;
        private readonly List<ITickable> _tickables = new();
        private readonly List<IFixedTickable> _fixedTickables = new();
        private readonly List<ILateTickable> _lateTickables = new();
        private bool _isInitialized;

        /// <summary>
        /// フレームワークが初期化済みかどうか
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 初期化完了時に発火するイベント
        /// </summary>
        public event Action OnInitialized;

        /// <summary>
        /// VContainerからコンテナを注入
        /// </summary>
        [Inject]
        public void Construct(IObjectResolver container)
        {
            _container = container;
        }

        private async void Start()
        {
            _cts = new CancellationTokenSource();

            if (_settings == null)
            {
                _settings = TFrameworkSettings.Instance;
            }

            try
            {
                await InitializeFrameworkAsync(_cts.Token);
                _isInitialized = true;
                OnInitialized?.Invoke();

#if DEBUG
                Debug.Log("[TFramework] Framework initialized successfully.");
#endif
            }
            catch (OperationCanceledException)
            {
                // キャンセルされた場合は何もしない
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TFramework] Framework initialization failed: {ex}");
            }
        }

        /// <summary>
        /// フレームワークの初期化処理
        /// </summary>
        private async UniTask InitializeFrameworkAsync(CancellationToken ct)
        {
            if (_container == null)
            {
                Debug.LogWarning("[TFramework] Container not injected. Skipping service initialization.");
                return;
            }

            // IInitializableを実装しているサービスを取得し、優先順位順に初期化
            var initializables = _container.Resolve<IEnumerable<IInitializable>>()?.ToList() ?? new List<IInitializable>();

            // 優先順位でソート（IInitializablePriorityを実装している場合）
            var sortedInitializables = initializables
                .OrderBy(i => i is IInitializablePriority p ? p.Priority : int.MaxValue)
                .ToList();

            foreach (var initializable in sortedInitializables)
            {
                ct.ThrowIfCancellationRequested();
                await initializable.InitializeAsync(ct);

#if DEBUG
                Debug.Log($"[TFramework] Initialized: {initializable.GetType().Name}");
#endif
            }

            // Tickableインターフェースを実装しているサービスを収集
            CollectTickables();
        }

        /// <summary>
        /// Tickableインターフェースを実装しているサービスを収集
        /// </summary>
        private void CollectTickables()
        {
            if (_container == null) return;

            var tickables = _container.Resolve<IEnumerable<ITickable>>();
            if (tickables != null)
            {
                _tickables.AddRange(tickables);
            }

            var fixedTickables = _container.Resolve<IEnumerable<IFixedTickable>>();
            if (fixedTickables != null)
            {
                _fixedTickables.AddRange(fixedTickables);
            }

            var lateTickables = _container.Resolve<IEnumerable<ILateTickable>>();
            if (lateTickables != null)
            {
                _lateTickables.AddRange(lateTickables);
            }
        }

        private void Update()
        {
            if (!_isInitialized) return;

            var deltaTime = Time.deltaTime;
            foreach (var tickable in _tickables)
            {
                tickable.Tick(deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (!_isInitialized) return;

            var fixedDeltaTime = Time.fixedDeltaTime;
            foreach (var tickable in _fixedTickables)
            {
                tickable.FixedTick(fixedDeltaTime);
            }
        }

        private void LateUpdate()
        {
            if (!_isInitialized) return;

            var deltaTime = Time.deltaTime;
            foreach (var tickable in _lateTickables)
            {
                tickable.LateTick(deltaTime);
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _tickables.Clear();
            _fixedTickables.Clear();
            _lateTickables.Clear();
        }
    }
}
