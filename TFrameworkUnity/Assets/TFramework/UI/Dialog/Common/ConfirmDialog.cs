using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace TFramework.UI
{
    /// <summary>
    /// 確認ダイアログ
    /// </summary>
    public sealed class ConfirmDialog : UIDialogBase<bool>
    {
        #region Nested Types
        public sealed class Param
        {
            public string Title { get; set; }
            public string Message { get; set; }
            public string ConfirmText { get; set; } = "OK";
            public string CancelText { get; set; } = "Cancel";
        }
        #endregion

        #region Serialized Fields
        [Header("UI References")]
        [SerializeField]
        private TFTextUGUI _titleText;

        [SerializeField]
        private TFTextUGUI _messageText;

        [SerializeField]
        private Button _confirmButton;

        [SerializeField]
        private TFTextUGUI _confirmButtonText;

        [SerializeField]
        private Button _cancelButton;

        [SerializeField]
        private TFTextUGUI _cancelButtonText;
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
                    _titleText.SetTextContent(dialogParam.Title ?? string.Empty);

                if (_messageText != null)
                    _messageText.SetTextContent(dialogParam.Message ?? string.Empty);

                if (_confirmButtonText != null)
                    _confirmButtonText.SetTextContent(dialogParam.ConfirmText);

                if (_cancelButtonText != null)
                    _cancelButtonText.SetTextContent(dialogParam.CancelText);
            }

            // ボタンイベント登録（Unity event methodsではなくR3使用）
            if (_confirmButton != null)
            {
                _confirmButton.OnClickAsObservable()
                    .Subscribe(_ => OnConfirmClicked())
                    .AddTo(_buttonDisposables);
            }

            if (_cancelButton != null)
            {
                _cancelButton.OnClickAsObservable()
                    .Subscribe(_ => OnCancelClicked())
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

        #region Private Methods
        private void OnConfirmClicked()
        {
            CloseWithResult(true);
        }

        private void OnCancelClicked()
        {
            CloseWithResult(false);
        }

        protected override void OnBackgroundClicked()
        {
            CloseWithResult(false);
        }
        #endregion
    }
}
