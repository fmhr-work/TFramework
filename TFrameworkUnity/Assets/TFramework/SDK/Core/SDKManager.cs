using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Core;
using UnityEngine;
using TFramework.Debug;

namespace TFramework.SDK
{
    /// <summary>
    /// TFrameworkのSDKモジュールエントリポイント
    /// </summary>
    public class SDKManager : IInitializable, IDisposable
    {
        /// <summary>
        /// 広告サービスへのアクセス
        /// </summary>
        public IAdsService Ads { get; }

        /// <summary>
        /// アナリティクスサービスへのアクセス
        /// </summary>
        public IAnalyticsService Analytics { get; }

        /// <summary>
        /// ストア（課金）サービスへのアクセス
        /// </summary>
        public IStoreService Store { get; }

        /// <summary>
        /// グローバルアクセス用シングルトン（VContainerでセットされる）
        /// </summary>
        public static SDKManager Instance { get; private set; }

        public SDKManager(IAdsService adsService, IAnalyticsService analyticsService, IStoreService storeService)
        {
            Ads = adsService;
            Analytics = analyticsService;
            Store = storeService;
        }

        /// <summary>
        /// IInitializableに基づく非同期初期化処理
        /// </summary>
        public async UniTask InitializeAsync(CancellationToken ct)
        {
            Instance = this;

            TLogger.Info("SDKManager Initializing...", "SDK");
            
            // 各種サービスの初期化を並列実行
            var initAdsTask = Ads != null ? Ads.InitializeAsync(ct) : UniTask.CompletedTask;
            var initStoreTask = Store != null ? Store.InitializeAsync(ct) : UniTask.CompletedTask;

            TLogger.Info("SDKManager Initialized successfully.", "SDK");
        }

        public void Dispose()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
