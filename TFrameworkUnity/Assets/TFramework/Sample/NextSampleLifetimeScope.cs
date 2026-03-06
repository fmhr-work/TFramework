using VContainer;
using VContainer.Unity;

namespace TFramework.Sample
{
    public class NextSampleLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<NextSampleSceneController>().AsImplementedInterfaces();
        }
    }
}
