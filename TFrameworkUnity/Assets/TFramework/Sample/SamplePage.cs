using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TFramework.UI;
using TFramework.Debug;
using TMPro;
using R3;
using VContainer;

namespace TFramework.Sample
{
    public class SamplePage : UIPageBase
    {
        #region Serialized Fields

        [SerializeField] 
        private TextMeshProUGUI _titleText;
        
        [SerializeField] 
        private UIButton _backButton;

        #endregion

        #region Dependencies

        private IUIService _uiService;

        [Inject]
        public void Construct(IUIService uiService)
        {
            _uiService = uiService;
        }

        #endregion

        #region Lifecycle Override

        protected override UniTask OnInitializeAsync(CancellationToken ct)
        {
            _backButton.Initialize();
            return UniTask.CompletedTask;
        }

        protected override void OnOpened()
        {
            _backButton.OnClickAsObservable()
                .Subscribe(_ => OnBackClicked())
                .AddTo(PageToken);
        }

        #endregion

        #region Private Methods

        private async void OnBackClicked()
        {
            // ConfirmDialogを表示
            var result = await _uiService.ShowDialogAsync<ConfirmDialog, bool>(
                new ConfirmDialog.Param
                {
                    Title = "Confirmation",
                    Message = "Are you sure you want to go back?",
                    ConfirmText = "Yes",
                    CancelText = "No"
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

        #endregion
    }
}
