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
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<SampleSceneController>().AsImplementedInterfaces();
        }
    }
}
