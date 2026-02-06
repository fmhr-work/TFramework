using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Core;
using TFramework.Installer;
using TFramework.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TFramework.Sample
{
    /// <summary>
    /// TFrameworkのサンプルシーン用LifetimeScope
    /// フレームワークのセットアップ方法を示す
    /// </summary>
    public class SampleLifetimeScope : LifetimeScope
    {
        [SerializeField] private TFrameworkSettings _settings;
        
        protected override void Configure(IContainerBuilder builder)
        {
            // TFrameworkのサービスを登録
            builder.UseTFramework(_settings);

            // アプリケーション固有のサービスをここに追加
            // builder.Register<MyGameService>(Lifetime.Singleton);
            
            // TFrameworkBootstrapを登録（TFrameworkのIInitializableサービスを初期化）
            builder.RegisterComponentInHierarchy<TFrameworkBootstrap>();
            
            // Entry Pointを登録
            builder.RegisterEntryPoint<SampleEntryPoint>();
        }
    }
    
    /// <summary>
    /// サンプルアプリケーションのエントリーポイント
    /// TFrameworkBootstrap初期化完了後に実行される
    /// </summary>
    public class SampleEntryPoint : IAsyncStartable
    {
        private readonly IUIService _uiService;
        private readonly TFrameworkBootstrap _bootstrap;
        
        [Inject]
        public SampleEntryPoint(IUIService uiService, TFrameworkBootstrap bootstrap)
        {
            _uiService = uiService;
            _bootstrap = bootstrap;
        }
        
        public async UniTask StartAsync(CancellationToken cancellation)
        {
            // TFrameworkの初期化完了を待つ
            await UniTask.WaitUntil(() => _bootstrap.IsInitialized, cancellationToken: cancellation);
            
            // UIServiceを使ってページを表示
            await _uiService.ShowPageAsync<SamplePage>();
        }
    }
}
