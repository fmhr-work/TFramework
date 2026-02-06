namespace TFramework.Localization
{
    /// <summary>
    /// 言語コード（ISO 639-1準拠）
    /// </summary>
    public enum LanguageCode
    {
        /// <summary>日本語</summary>
        Japanese,

        /// <summary>英語</summary>
        English,

        /// <summary>簡体中文</summary>
        ChineseSimplified,

        /// <summary>繁体中文</summary>
        ChineseTraditional,

        /// <summary>韓国語</summary>
        Korean
    }

    /// <summary>
    /// LanguageCode拡張メソッド
    /// </summary>
    public static class LanguageCodeExtensions
    {
        #region Public Methods
        /// <summary>
        /// 言語コードをISO 639-1コード文字列に変換
        /// </summary>
        public static string ToCode(this LanguageCode language)
        {
            return language switch
            {
                LanguageCode.Japanese => "ja",
                LanguageCode.English => "en",
                LanguageCode.ChineseSimplified => "zh-CN",
                LanguageCode.ChineseTraditional => "zh-TW",
                LanguageCode.Korean => "ko",
                _ => "en"
            };
        }

        /// <summary>
        /// ISO 639-1コード文字列を言語コードに変換
        /// </summary>
        public static LanguageCode FromCode(string code)
        {
            return code switch
            {
                "ja" => LanguageCode.Japanese,
                "en" => LanguageCode.English,
                "zh-CN" => LanguageCode.ChineseSimplified,
                "zh-TW" => LanguageCode.ChineseTraditional,
                "ko" => LanguageCode.Korean,
                _ => LanguageCode.English
            };
        }

        /// <summary>
        /// 言語の表示名を取得
        /// </summary>
        public static string GetDisplayName(this LanguageCode language)
        {
            return language switch
            {
                LanguageCode.Japanese => "日本語",
                LanguageCode.English => "English",
                LanguageCode.ChineseSimplified => "简体中文",
                LanguageCode.ChineseTraditional => "繁體中文",
                LanguageCode.Korean => "한국어",
                _ => "English"
            };
        }
        #endregion
    }
}
