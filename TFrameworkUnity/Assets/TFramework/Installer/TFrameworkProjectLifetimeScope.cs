using TFramework.Core;
using TFramework.Installer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TFramework.Installer
{
    /// <summary>
    /// プロジェクト全体のLifeTimeScope (Root Lifetime Scope)
    /// TFrameworkのコアサービスを一括登録します。
    /// </summary>
    public class TFrameworkProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private TFrameworkSettings _settings;

        protected override void Configure(IContainerBuilder builder)
        {
            // TFrameworkの設定とコアサービスの登録（シングルトン）
            builder.UseTFramework(_settings);

            // Bootstrap（初期化プロセス）を登録
            builder.RegisterComponentOnNewGameObject<TFrameworkBootstrap>(Lifetime.Singleton, "TFrameworkBootstrap");
        }
    }
}
