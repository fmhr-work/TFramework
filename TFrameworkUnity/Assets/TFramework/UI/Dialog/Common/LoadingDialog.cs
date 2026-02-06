using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace TFramework.UI
{
    /// <summary>
    /// ローディングダイアログ
    /// </summary>
    public sealed class LoadingDialog : UIDialogBase
    {
        #region Serialized Fields
        [Header("UI References")]
        [SerializeField]
        private TextMeshProUGUI _messageText;

        [SerializeField]
        private RectTransform _loadingIcon;

        [SerializeField]
        private float _rotationSpeed = 360f;
        #endregion

        #region Private Fields
        private CancellationTokenSource _rotationCts;
        #endregion

        #region Lifecycle Override
        protected override UniTask OnPreOpenAsync(object param, CancellationToken ct)
        {
            if (param is string message && _messageText != null)
            {
                _messageText.text = message;
            }

            return UniTask.CompletedTask;
        }

        protected override void OnOpened()
        {
            // アイコン回転アニメーション（UpdateではなくUniTaskで実装）
            _rotationCts = new CancellationTokenSource();
            RotateIconAsync(_rotationCts.Token).Forget();
        }

        protected override void OnClosed()
        {
            _rotationCts?.Cancel();
            _rotationCts?.Dispose();
            base.OnClosed();
        }
        #endregion

        #region Private Methods
        private async UniTaskVoid RotateIconAsync(CancellationToken ct)
        {
            if (_loadingIcon == null)
                return;

            while (!ct.IsCancellationRequested)
            {
                var rotation = _loadingIcon.localEulerAngles;
                rotation.z -= _rotationSpeed * Time.deltaTime;
                _loadingIcon.localEulerAngles = rotation;
                await UniTask.Yield(ct);
            }
        }
        #endregion

        #region Public Methods
        public void UpdateMessage(string message)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
            }
        }
        #endregion
    }
}
