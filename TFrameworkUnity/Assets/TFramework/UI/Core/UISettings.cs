using UnityEngine;
using TFramework.Debug;

namespace TFramework.UI
{
    /// <summary>
    /// UI設定を管理するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "UISettings", menuName = "TFramework/UI Settings")]
    public sealed class UISettings : ScriptableObject
    {
        #region General Settings
        [Header("General")]
        [Tooltip("UIRootプレハブのAddressableキー")]
        [SerializeField]
        private string _uiRootAddress = "UIRoot";

        [Tooltip("デフォルトのページ遷移時間（秒）")]
        [SerializeField]
        [Range(0f, 2f)]
        private float _defaultTransitionDuration = 0.3f;
        #endregion

        #region Dialog Settings
        [Header("Dialog")]
        [Tooltip("ダイアログ背景の色")]
        [SerializeField]
        private Color _dialogBackgroundColor = new(0f, 0f, 0f, 0.5f);

        [Tooltip("ダイアログ表示時のフェード時間（秒）")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _dialogFadeDuration = 0.2f;
        #endregion

        #region Toast Settings
        [Header("Toast")]
        [Tooltip("トーストのデフォルト表示時間（秒）")]
        [SerializeField]
        [Range(1f, 10f)]
        private float _toastDefaultDuration = 3f;
        #endregion

        #region Loading Settings
        [Header("Loading")]
        [Tooltip("ローディング表示前の遅延時間（秒）")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _loadingDelay = 0.5f;
        #endregion

        #region Properties
        public string UIRootAddress => _uiRootAddress;
        public float DefaultTransitionDuration => _defaultTransitionDuration;
        public Color DialogBackgroundColor => _dialogBackgroundColor;
        public float DialogFadeDuration => _dialogFadeDuration;
        public float ToastDefaultDuration => _toastDefaultDuration;
        public float LoadingDelay => _loadingDelay;
        #endregion

        #region Singleton Instance
        private static UISettings _instance;

        public static UISettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<UISettings>("UISettings");
                    if (_instance == null)
                    {
                        TLogger.Warning("[TFramework] UISettings not found in Resources. Using default values");
                        _instance = CreateInstance<UISettings>();
                    }
                }
                return _instance;
            }
        }
        #endregion
    }
}
