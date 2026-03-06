using R3;
using TMPro;
using UnityEngine;
using VContainer;

namespace TFramework.Localization
{
    /// <summary>
    /// TMP_Text自動ローカライズコンポーネント
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("TFramework/Localization/Localized Text")]
    public sealed class LocalizedText : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField]
        [Tooltip("ローカライズキー")]
        private string _key;

        [SerializeField]
        [Tooltip("フォーマットパラメーター")]
        private string[] _parameters;

        [SerializeField]
        [Tooltip("自動更新を有効化")]
        private bool _autoUpdate = true;
        #endregion

        #region Private Fields
        private TMP_Text _text;
        private ILocalizationService _localization;
        private System.IDisposable _languageChangedSubscription;
        #endregion

        #region Properties
        public string Key
        {
            get => _key;
            set
            {
                _key = value;
                UpdateText();
            }
        }

        public string[] Parameters
        {
            get => _parameters;
            set
            {
                _parameters = value;
                UpdateText();
            }
        }
        #endregion

        #region Lifecycle
        [Inject]
        public void Construct(ILocalizationService localization)
        {
            _localization = localization;
        }

        public void Initialize()
        {
            if (!TryGetComponent<TMP_Text>(out _text))
            {
                UnityEngine.Debug.LogError($"[LocalizedText] TMP_Text not found on {gameObject.name}", this);
                return;
            }

            // 初期更新
            UpdateText();

            // 言語変更時自動更新
            if (_autoUpdate && _localization != null)
            {
                _languageChangedSubscription = _localization.OnLanguageChanged
                    .Subscribe(_ => UpdateText());
            }
        }

        private void OnDestroy()
        {
            _languageChangedSubscription?.Dispose();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// キーとパラメーターを設定
        /// </summary>
        public void SetKey(string key, params string[] parameters)
        {
            _key = key;
            _parameters = parameters;
            UpdateText();
        }

        /// <summary>
        /// テキストを手動更新
        /// </summary>
        public void UpdateText()
        {
            if (_localization == null || _text == null || string.IsNullOrEmpty(_key))
            {
                return;
            }

            if (_parameters != null && _parameters.Length > 0)
            {
                _text.text = _localization.Get(_key, _parameters);
            }
            else
            {
                _text.text = _localization.Get(_key);
            }
        }
        #endregion
    }
}
