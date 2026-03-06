using System.Threading;
using Cysharp.Threading.Tasks;

namespace TFramework.Localization
{
    /// <summary>
    /// ローカライズデータプロバイダーインターフェース
    /// </summary>
    public interface ILocalizationProvider
    {
        /// <summary>
        /// 指定言語のテーブルをロード
        /// </summary>
        UniTask<LocalizationTable> LoadTableAsync(LanguageCode language, CancellationToken ct);
    }
}
