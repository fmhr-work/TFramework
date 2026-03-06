using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace TFramework.MasterData.Editor
{
    /// <summary>
    /// CSVファイルを解析するクラス
    /// 引用符で囲まれたフィールドや改行に対応する
    /// </summary>
    public static class CsvParser
    {
        // CSVの各行を分割する正規表現（引用符内のカンマを無視）
        private static readonly Regex CsvSplitter = new Regex(@",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))");

        /// <summary>
        /// CSV形式の文字列を解析し、文字列配列のリストを返す
        /// </summary>
        public static List<string[]> Parse(string csvContent)
        {
            var result = new List<string[]>();
            if (string.IsNullOrEmpty(csvContent)) return result;

            // 改行コードの正規化
            csvContent = csvContent.Replace("\r\n", "\n").Replace("\r", "\n");

            var lines = csvContent.Split('\n');
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = CsvSplitter.Split(line);
                for (int i = 0; i < fields.Length; i++)
                {
                    fields[i] = fields[i].Trim('"'); // 引用符を除去
                }
                result.Add(fields);
            }

            return result;
        }
    }
}
