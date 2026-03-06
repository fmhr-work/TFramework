using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Debug;
using TFramework.Resource;

namespace TFramework.Localization
{
    /// <summary>
    /// CSVローカライズデータプロバイダー
    /// </summary>
    public sealed class CsvLocalizationProvider : ILocalizationProvider
    {
        #region Private Fields
        private readonly IResourceService _resourceService;
        private readonly LocalizationSettings _settings;
        #endregion

        #region Constructor
        public CsvLocalizationProvider(IResourceService resourceService, LocalizationSettings settings)
        {
            _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
        #endregion

        #region ILocalizationProvider
        public async UniTask<LocalizationTable> LoadTableAsync(LanguageCode language, CancellationToken ct)
        {
            try
            {
                // 共通CSVファイルのアドレス
                var address = _settings.CsvAddressFormat;

                TLogger.Info($"[CsvLocalizationProvider] Loading CSV from: {address}");

                // CSVファイルをロード
                var textAsset = await _resourceService.LoadAsync<UnityEngine.TextAsset>(address, ct);
                if (textAsset == null)
                {
                    throw new Exception($"Failed to load CSV file: {address}");
                }

                // CSVをパース
                var table = ParseCsv(textAsset.text, language);

                TLogger.Info($"[CsvLocalizationProvider] Loaded {table.Count} entries for {language}");

                return table;
            }
            catch (Exception ex)
            {
                TLogger.Error($"[CsvLocalizationProvider] Failed to load table for {language}: {ex.Message}", ex);
                throw;
            }
        }
        #endregion

        #region Private Methods
        private LocalizationTable ParseCsv(string csvText, LanguageCode language)
        {
            var lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
            {
                throw new Exception("CSV file must have at least a header row and one data row");
            }

            // ヘッダー行を解析
            var headers = ParseCsvLine(lines[0]);
            var languageCode = language.ToCode();
            var languageColumnIndex = -1;

            // 言語列のインデックスを検索
            for (int i = 0; i < headers.Count; i++)
            {
                if (headers[i].Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                {
                    languageColumnIndex = i;
                    break;
                }
            }

            if (languageColumnIndex == -1)
            {
                throw new Exception($"Language column '{languageCode}' not found in CSV header");
            }

            // テーブル作成
            var table = new LocalizationTable(language, lines.Length - 1);

            // データ行を解析
            for (int i = 1; i < lines.Length; i++)
            {
                var columns = ParseCsvLine(lines[i]);
                if (columns.Count < 2)
                {
                    continue; // 空行やコメント行をスキップ
                }

                var key = columns[0];
                if (string.IsNullOrEmpty(key) || key.StartsWith("#") || key.StartsWith("//"))
                {
                    continue; // コメント行をスキップ
                }

                if (languageColumnIndex < columns.Count)
                {
                    var value = columns[languageColumnIndex];
                    table.Add(key, value);
                }
            }

            return table;
        }

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = string.Empty;
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.Trim());
                    current = string.Empty;
                }
                else
                {
                    current += c;
                }
            }

            result.Add(current.Trim());
            return result;
        }
        #endregion
    }
}
