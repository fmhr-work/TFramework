using TFramework.Core;
using TFramework.Debug;
using TFramework.Localization;
using TFramework.Pool;
using TFramework.Resource;
using TFramework.Time;
using TFramework.UI;
using VContainer;

namespace TFramework.Installer
{
    /// <summary>
    /// TFrameworkの全サービスをVContainerに登録するインストーラー
    /// LifetimeScopeで使用する
    /// </summary>
    public static class TFrameworkInstaller
    {
        /// <summary>
        /// TFrameworkの全サービスを登録する
        /// </summary>
        /// <param name="builder">ContainerBuilder</param>
        /// <param name="settings">フレームワーク設定（省略時はデフォルト）</param>
        public static void Install(IContainerBuilder builder, TFrameworkSettings settings = null)
        {
            settings ??= TFrameworkSettings.Instance;

            // 設定を登録
            builder.RegisterInstance(settings);

            // Loggerを初期化
            TLogger.Initialize(settings);

            // Pool
            builder.Register<PoolManager>(Lifetime.Singleton).As<IPoolManager>();

            // Resource
            builder.Register<AddressableResourceService>(Lifetime.Singleton)
                .As<IResourceService>()
                .As<IInitializable>();

            // UI Settings
            builder.RegisterInstance(UISettings.Instance);

            // UI
            builder.Register<UIManager>(Lifetime.Singleton)
                .As<IUIService>()
                .As<IInitializable>();

            // Localization Settings
            builder.RegisterInstance(LocalizationSettings.Instance);

            // Localization Provider
            builder.Register<CsvLocalizationProvider>(Lifetime.Singleton).As<ILocalizationProvider>();

            // Localization
            builder.Register<LocalizationManager>(Lifetime.Singleton)
                .As<ILocalizationService>()
                .As<IInitializable>();

            // Time Settings
            builder.RegisterInstance(TimeSettings.Instance);
            
            // Time
            builder.Register<TimeManager>(Lifetime.Singleton)
                .As<ITimeService>()
                .As<IInitializable>();

            // 登録完了ログ
            TLogger.Info("TFramework services installed", "Installer");
        }

        /// <summary>
        /// IContainerBuilderの拡張メソッドとして使用可能
        /// </summary>
        public static IContainerBuilder UseTFramework(this IContainerBuilder builder, TFrameworkSettings settings = null)
        {
            Install(builder, settings);
            return builder;
        }
    }
}
