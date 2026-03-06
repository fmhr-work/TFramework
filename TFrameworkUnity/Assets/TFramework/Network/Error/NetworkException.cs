using System;

namespace TFramework.Network
{
    /// <summary>
    /// ネットワーク通信に関連するエラーを表す例外クラス
    /// </summary>
    public class NetworkException : Exception
    {
        /// <summary>
        /// HTTPステータスコード（存在する場合）
        /// </summary>
        public long StatusCode { get; }

        /// <summary>
        /// レスポンスボディ（存在する場合）
        /// </summary>
        public string ResponseBody { get; }

        public NetworkException(string message) : base(message)
        {
        }

        public NetworkException(string message, long statusCode, string responseBody) : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }

        public NetworkException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
