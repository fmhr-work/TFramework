using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TFramework.Core;

namespace TFramework.Localization
{
    /// <summary>
    /// 多言語サービスインターフェース
    /// </summary>
    public interface ILocalizationService : IService
    {
        #region Properties
        /// <summary>
        /// 現在の言語
        /// </summary>
        LanguageCode CurrentLanguage { get; set; }

        /// <summary>
        /// サポートされている言語一覧
        /// </summary>
        LanguageCode[] SupportedLanguages { get; }

        /// <summary>
        /// 言語変更イベント
        /// </summary>
        Observable<LanguageCode> OnLanguageChanged { get; }
        #endregion

        #region Methods
        /// <summary>
        /// テキスト取得
        /// </summary>
        string Get(string key);

        /// <summary>
        /// パラメーター付きテキスト取得
        /// </summary>
        string Get(string key, params object[] args);

        /// <summary>
        /// キー存在確認
        /// </summary>
        bool HasKey(string key);

        /// <summary>
        /// 言語データロード
        /// </summary>
        UniTask LoadLanguageAsync(LanguageCode language, CancellationToken ct);
        #endregion
    }
}
