using TFramework.Debug;
using UnityEngine;

namespace TFramework.Localization
{
    /// <summary>
    /// ローカライズ設定
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationSettings", menuName = "TFramework/Localization/Settings")]
    public sealed class LocalizationSettings : ScriptableObject
    {
        #region Serialized Fields
        [Header("Language Settings")]
        [SerializeField]
        [Tooltip("デフォルト言語")]
        private LanguageCode _defaultLanguage = LanguageCode.English;

        [SerializeField]
        [Tooltip("フォールバック言語")]
        private LanguageCode _fallbackLanguage = LanguageCode.English;

        [SerializeField]
        [Tooltip("サポートする言語")]
        private LanguageCode[] _supportedLanguages = new[]
        {
            LanguageCode.Japanese,
            LanguageCode.English,
            LanguageCode.ChineseSimplified
        };

        [Header("Provider Settings")]
        [SerializeField]
        [Tooltip("CSVファイルのAddressableアドレス（共通ファイル）")]
        private string _csvAddressFormat = "Localization/localization.csv";

        [SerializeField]
        [Tooltip("JSONファイルのAddressableアドレス（例: Localization/{language}.json）")]
        private string _jsonAddressFormat = "Localization/{0}.json";

        [Header("Debug")]
        [SerializeField]
        [Tooltip("キーが見つからない場合にキーを表示")]
        private bool _showMissingKeyWarning = true;
        #endregion

        #region Properties
        public LanguageCode DefaultLanguage => _defaultLanguage;
        public LanguageCode FallbackLanguage => _fallbackLanguage;
        public LanguageCode[] SupportedLanguages => _supportedLanguages;
        public string CsvAddressFormat => _csvAddressFormat;
        public string JsonAddressFormat => _jsonAddressFormat;
        public bool ShowMissingKeyWarning => _showMissingKeyWarning;
        #endregion

        #region Singleton
        private static LocalizationSettings _instance;

        public static LocalizationSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<LocalizationSettings>("LocalizationSettings");
                    if (_instance == null)
                    {
                        TLogger.Warning("[LocalizationSettings] Settings asset not found in Resources folder");
                    }
                }
                return _instance;
            }
        }
        #endregion
    }
}
