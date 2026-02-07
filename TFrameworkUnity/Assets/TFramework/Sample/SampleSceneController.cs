using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Scene;
using TFramework.UI;
using VContainer;

namespace TFramework.Sample
{
    /// <summary>
    /// SampleSceneのルートクラス
    /// </summary>
    public class SampleSceneController : SceneControllerBase
    {
        private IUIService _uiService;

        [Inject]
        public void Construct(IUIService uiService)
        {
            _uiService = uiService;
        }

        protected override async UniTask OnInitializeInternalAsync(ISceneBridgeData bridgeData, CancellationToken ct)
        {
            await base.OnInitializeInternalAsync(bridgeData, ct);
            await _uiService.ShowPageAsync<SamplePage>();
        }
    }
}
