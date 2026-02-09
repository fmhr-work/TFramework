using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Core;
using TFramework.Debug;
using UnityEngine;

namespace TFramework.MasterData
{
    /// <summary>
    /// MasterDataサービスの標準実装
    /// </summary>
    public partial class MasterDataManager : IMasterDataService, IInitializable
    {
        private readonly MasterDataSettings _settings;
        private bool _isInitialized;

        // 生成されたコンテナを保持する辞書
        // Key: コンテナの型, Value: ScriptableObjectのコンテナ
        private readonly Dictionary<Type, ScriptableObject> _containers = new Dictionary<Type, ScriptableObject>();
        
        // データ型からコンテナへのマッピング（GetAll用）
        // Key: MasterDataの型(T), Value: ScriptableObject
        private readonly Dictionary<Type, ScriptableObject> _dataToContainerMap = new Dictionary<Type, ScriptableObject>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="settings">MasterData設定</param>
        public MasterDataManager(MasterDataSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// TFrameworkの初期化フェーズで呼び出される
        /// </summary>
        public async UniTask InitializeAsync(CancellationToken ct)
        {
            if (_isInitialized) return;

            if (_settings.AutoLoadOnStartup)
            {
                await LoadAllAsync(ct);
            }

            _isInitialized = true;
            TLogger.Info("[MasterDataManager] 初期化完了");
        }

        /// <summary>
        /// 全てのデータをロードする
        /// 設定のアセットリストからコンテナを登録する
        /// </summary>
        private async UniTask LoadAllAsync(CancellationToken ct)
        {
            _containers.Clear();
            _dataToContainerMap.Clear();

            if (_settings.Containers != null)
            {
                foreach (var container in _settings.Containers)
                {
                    if (container == null) continue;
                    RegisterContainer(container);
                }
            }
            
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// コンテナを内部辞書に登録する
        /// </summary>
        private void RegisterContainer(ScriptableObject container)
        {
            var containerType = container.GetType();
            _containers[containerType] = container;
            
            // コンテナが持つデータ型を特定し、マッピングを作成する
            // コンテナは "All" プロパティを持ち、その戻り値が IReadOnlyList<T> であると想定
            var allProp = containerType.GetProperty("All");
            if (allProp != null)
            {
                var returnType = allProp.PropertyType;
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
                {
                    var dataType = returnType.GetGenericArguments()[0];
                    _dataToContainerMap[dataType] = container;
                }
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<T> GetAll<T>() where T : class, IMasterDataObject
        {
            if (_dataToContainerMap.TryGetValue(typeof(T), out var container))
            {
                var property = container.GetType().GetProperty("All");
                if (property != null)
                {
                    return property.GetValue(container) as IReadOnlyList<T>;
                }
            }
            
            TLogger.Warning($"[MasterDataManager] データが見つからない: {typeof(T).Name}");
            return Array.Empty<T>();
        }

        /// <inheritdoc />
        public T Get<T, TKey>(TKey key) where T : class, IMasterDataObject<TKey>
        {
            var all = GetAll<T>();
            if (all == null) return null;

            // 線形探索（コンテナ側で最適化されたFindを使いたい場合はGetContainer経由で呼ぶこと）
            foreach (var item in all)
            {
                if (item.GetKey().Equals(key))
                {
                    return item;
                }
            }
            return null;
        }

        /// <inheritdoc />
        public T GetContainer<T>() where T : class
        {
            if (_containers.TryGetValue(typeof(T), out var container))
            {
                return container as T;
            }
            return null;
        }

        /// <inheritdoc />
        public async UniTask DownloadFromServerAsync(CancellationToken ct)
        {
            if (!_settings.EnableServerSync)
            {
                TLogger.Warning("[MasterDataManager] サーバー同期が無効である");
                return;
            }

            TLogger.Info($"[MasterDataManager] サーバーからダウンロード開始: {_settings.ServerUrl}");
            // TODO: 実装はサーバー仕様決定後に行う
            await UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public async UniTask ReloadAsync(CancellationToken ct)
        {
            await LoadAllAsync(ct);
            TLogger.Info("[MasterDataManager] 再ロード完了");
        }
    }
}
