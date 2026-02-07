using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Scene;
using TFramework.UI;
using TFramework.Debug;
using VContainer;
using R3;

namespace TFramework.Sample
{
    /// <summary>
    /// NextSampleSceneのルートクラス
    /// データを受け取り、NextSamplePageを表示する
    /// </summary>
    public class NextSampleSceneController : SceneControllerBase
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

            // データキャスト
            var nextSceneData = bridgeData as NextSampleSceneBridgeData;
            
            if (nextSceneData != null)
            {
                TLogger.Info($"[NextSampleScene] Received Message: {nextSceneData.Message}");
            }
            else
            {
                TLogger.Warning("[NextSampleScene] Bridge data is null or invalid type.");
            }

            // SamplePageを表示し、データを渡す
            await _uiService.ShowPageAsync<SamplePage>();
            var page = _uiService.CurrentPage as SamplePage;
            
            if (page != null)
            {
                page.SetupFromSceneBridgeData(nextSceneData);
            }
            else
            {
                TLogger.Error("[NextSampleSceneController] Failed to cast current page to SamplePage.");
            }
        }
    }
}
