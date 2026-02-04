using TFramework.Core;
using TFramework.Installer;
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

            // Bootstrapを登録
            builder.RegisterComponentInHierarchy<TFrameworkBootstrap>();

            // アプリケーション固有のサービスをここに追加
            // builder.Register<MyGameService>(Lifetime.Singleton);
        }
    }
}
