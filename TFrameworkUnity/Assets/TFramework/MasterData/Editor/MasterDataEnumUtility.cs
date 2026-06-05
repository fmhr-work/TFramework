using System;
using System.Text.RegularExpressions;

namespace TFramework.MasterData.Editor
{
    /// <summary>
    /// MasterDataのEnum定義を扱うユーティリティクラス
    /// </summary>
    internal static class MasterDataEnumUtility
    {
        private static readonly Regex InvalidIdentifierRegex = new Regex(@"[^a-zA-Z0-9_]", RegexOptions.Compiled);

        /// <summary>
        /// Enum型指定か判定
        /// </summary>
        public static bool IsEnumType(string csvType)
        {
            return !string.IsNullOrEmpty(csvType) &&
                   (string.Equals(csvType, "enum", StringComparison.OrdinalIgnoreCase) ||
                    csvType.StartsWith("enum:", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// C#識別子として安全な形式へ正規化
        /// </summary>
        public static string NormalizeIdentifier(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return "_";
            }

            string trimmed = rawValue.Trim();
            string normalized = InvalidIdentifierRegex.Replace(trimmed, "_");
            if (char.IsDigit(normalized[0]))
            {
                normalized = $"_{normalized}";
            }

            return normalized;
        }
    }
}
