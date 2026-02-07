using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TFramework.UI;
using TFramework.Debug;
using TFramework.Localization;
using R3;
using TFramework.Scene;
using VContainer;

namespace TFramework.Sample
{
    public class SamplePage : UIPageBase
    {
        #region Serialized Fields
        [Header("UI Components")]
        [SerializeField] 
        private TFTextUGUI _titleText;
        
        [SerializeField] 
        private TFTextUGUI _currentLanguageText;
        
        [SerializeField]
        private TFTextUGUI _welcomeText;
        
        [SerializeField] 
        private UIButton _backButton;

        [SerializeField]
        private UIButton _timeSampleButton;
        
        [SerializeField]
        private UIButton _nextSceneButton;

        [Header("Language Switcher")]
        [SerializeField]
        private UIButton _japaneseButton;
        
        [SerializeField]
        private UIButton _englishButton;
        
        [SerializeField]
        private UIButton _chineseButton;
        #endregion

        #region Dependencies
        private IUIService _uiService;
        private ILocalizationService _localization;
        private ISceneService _sceneService;

        [Inject]
        public void Construct(IUIService uiService, ILocalizationService localization, ISceneService sceneService)
        {
            _uiService = uiService;
            _localization = localization;
            _sceneService = sceneService;
        }
        #endregion

        #region Lifecycle Override
        protected override UniTask OnInitializeAsync(CancellationToken ct)
        {
            // ボタン初期化
            _backButton?.Initialize();
            _timeSampleButton?.Initialize();
            _nextSceneButton?.Initialize();
            _japaneseButton?.Initialize();
            _englishButton?.Initialize();
            _chineseButton?.Initialize();
            
            // ローカライズテキスト設定
            _titleText?.SetLocalizationKey("ui.title.main");
            _welcomeText?.SetLocalizationKey("ui.message.welcome", "Player");
            
            return UniTask.CompletedTask;
        }

        protected override void OnOpened()
        {
            // 現在の言語表示を更新
            UpdateCurrentLanguageDisplay();
            
            // 言語変更イベント購読
            _localization.OnLanguageChanged
                .Subscribe(_ => UpdateCurrentLanguageDisplay())
                .AddTo(PageToken);
            
            // 戻るボタン
            _backButton?.OnClickAsObservable()
                .Subscribe(_ => OnBackClicked().Forget())
                .AddTo(PageToken);

            // Time Sampleへ遷移
            _timeSampleButton?.OnClickAsObservable()
                .Subscribe(_ => OnTimeSampleClicked().Forget())
                .AddTo(PageToken);
            _nextSceneButton?.OnClickAsObservable()
                .Subscribe(_ => OnNextSceneClicked().Forget())
                .AddTo(PageToken);
            
            // 言語切り替えボタン
            _japaneseButton?.OnClickAsObservable()
                .Subscribe(_ => OnLanguageChanged(LanguageCode.Japanese))
                .AddTo(PageToken);
            
            _englishButton?.OnClickAsObservable()
                .Subscribe(_ => OnLanguageChanged(LanguageCode.English))
                .AddTo(PageToken);
            
            _chineseButton?.OnClickAsObservable()
                .Subscribe(_ => OnLanguageChanged(LanguageCode.ChineseSimplified))
                .AddTo(PageToken);
        }
        #endregion

        #region Private Methods
        private void UpdateCurrentLanguageDisplay()
        {
            if (_currentLanguageText != null)
            {
                var langName = _localization.CurrentLanguage.GetDisplayName();
                _currentLanguageText.SetTextContent($"Current Language: {langName}");
            }
        }

        private void OnLanguageChanged(LanguageCode newLanguage)
        {
            TLogger.Info($"[SamplePage] Changing language to: {newLanguage}");
            _localization.CurrentLanguage = newLanguage;
        }

        private async UniTask OnTimeSampleClicked()
        {
            await _uiService.ShowPageAsync<TimeSamplePage>();
        }

        private async UniTask OnNextSceneClicked()
        {
            var data = new NextSampleSceneBridgeData("Hello from SamplePage!", 123);
            await _sceneService.LoadSceneAsync("NextSampleScene", data, ct: this.GetCancellationTokenOnDestroy());
        }

        private async UniTask OnBackClicked()
        {
            // ConfirmDialogを表示（ローカライズキーを使用）
            var result = await _uiService.ShowDialogAsync<ConfirmDialog, bool>(
                new ConfirmDialog.Param
                {
                    Title = _localization.Get("ui.title.settings"),
                    Message = _localization.Get("ui.message.complete"),
                    ConfirmText = _localization.Get("ui.button.confirm"),
                    CancelText = _localization.Get("ui.button.cancel")
                });

            if (result)
            {
                TLogger.Info("User confirmed - going back");
                await _uiService.GoBackAsync();
            }
            else
            {
                TLogger.Info("User cancelled");
            }
        }
        
        /// <summary>
        /// シーンから渡されたBridgeDataを受け取って表示する（テスト用）
        /// </summary>
        public void SetupFromSceneBridgeData(NextSampleSceneBridgeData data)
        {
            if (data == null)
            {
                TLogger.Warning("[SamplePage] Received null bridge data");
                return;
            }
            
            TLogger.Info($"[SamplePage] Received bridge data - Message: {data.Message}, SourceId: {data.SourceId}");
            
            // タイトルテキストに受け取ったデータを表示
            if (_titleText != null)
            {
                _titleText.SetTextContent($"Scene Data: {data.Message} (ID: {data.SourceId})");
            }
        }
        #endregion
    }
}
