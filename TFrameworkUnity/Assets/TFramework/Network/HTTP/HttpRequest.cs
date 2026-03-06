using System.Collections.Generic;

namespace TFramework.Network
{
    /// <summary>
    /// HTTPリクエスト内容を保持するクラス
    /// </summary>
    public class HttpRequest
    {
        /// <summary>
        /// リクエストURL
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// HTTPメソッド
        /// </summary>
        public ApiType Method { get; }

        /// <summary>
        /// リクエストヘッダー
        /// </summary>
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        /// <summary>
        /// リクエストボディ（JSON文字列など）
        /// </summary>
        public string Body { get; set; }

        public HttpRequest(string url, ApiType method)
        {
            Url = url;
            Method = method;
        }
    }
}
