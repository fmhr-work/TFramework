using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TFramework.UI
{
    /// <summary>
    /// メッセージダイアログ
    /// </summary>
    public sealed class MessageDialog : UIDialogBase
    {
        #region Nested Types
        public sealed class Param
        {
            public string Title { get; set; }
            public string Message { get; set; }
            public string ButtonText { get; set; } = "OK";
        }
        #endregion

        #region Serialized Fields
        [Header("UI References")]
        [SerializeField]
        private TextMeshProUGUI _titleText;

        [SerializeField]
        private TextMeshProUGUI _messageText;

        [SerializeField]
        private Button _okButton;

        [SerializeField]
        private TextMeshProUGUI _okButtonText;
        #endregion

        #region Private Fields
        private CompositeDisposable _buttonDisposables;
        #endregion

        #region Lifecycle Override
        protected override UniTask OnPreOpenAsync(object param, CancellationToken ct)
        {
            _buttonDisposables = new CompositeDisposable();

            if (param is Param dialogParam)
            {
                if (_titleText != null)
                    _titleText.text = dialogParam.Title ?? string.Empty;

                if (_messageText != null)
                    _messageText.text = dialogParam.Message ?? string.Empty;

                if (_okButtonText != null)
                    _okButtonText.text = dialogParam.ButtonText;
            }

            // ボタンイベント登録
            if (_okButton != null)
            {
                _okButton.OnClickAsObservable()
                    .Subscribe(_ => Close())
                    .AddTo(_buttonDisposables);
            }

            return UniTask.CompletedTask;
        }

        protected override void OnClosed()
        {
            _buttonDisposables?.Dispose();
            base.OnClosed();
        }
        #endregion
    }
}
