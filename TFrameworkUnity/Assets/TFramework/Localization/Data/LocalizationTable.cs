using System.Collections.Generic;

namespace TFramework.Localization
{
    /// <summary>
    /// ローカライズテーブル（メモリ内）
    /// </summary>
    public sealed class LocalizationTable
    {
        #region Private Fields
        private readonly Dictionary<string, string> _entries;
        #endregion

        #region Properties
        /// <summary>
        /// 言語コード
        /// </summary>
        public LanguageCode Language { get; }

        /// <summary>
        /// エントリー数
        /// </summary>
        public int Count => _entries.Count;
        #endregion

        #region Constructor
        public LocalizationTable(LanguageCode language)
        {
            Language = language;
            _entries = new Dictionary<string, string>();
        }

        public LocalizationTable(LanguageCode language, int capacity)
        {
            Language = language;
            _entries = new Dictionary<string, string>(capacity);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// エントリーを追加
        /// </summary>
        public void Add(string key, string value)
        {
            _entries[key] = value;
        }

        /// <summary>
        /// テキストを取得
        /// </summary>
        public string Get(string key)
        {
            return _entries.TryGetValue(key, out var value) ? value : key;
        }

        /// <summary>
        /// キーが存在するか確認
        /// </summary>
        public bool ContainsKey(string key)
        {
            return _entries.ContainsKey(key);
        }

        /// <summary>
        /// テーブルをクリア
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
        }

        /// <summary>
        /// すべてのキーを取得
        /// </summary>
        public IEnumerable<string> GetAllKeys()
        {
            return _entries.Keys;
        }
        #endregion
    }
}
