using System.Collections.Generic;

namespace TFramework.Network
{
    /// <summary>
    /// HTTPレスポンス内容を保持する構造体
    /// </summary>
    public struct HttpResponse
    {
        /// <summary>
        /// HTTPステータスコード
        /// </summary>
        public long StatusCode { get; }

        /// <summary>
        /// レスポンスボディ
        /// </summary>
        public string Body { get; }

        /// <summary>
        /// エラーメッセージ（失敗時）
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// レスポンスヘッダー
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// リクエストが成功したかどうか
        /// </summary>
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300 && string.IsNullOrEmpty(ErrorMessage);

        public HttpResponse(long statusCode, string body, string errorMessage, Dictionary<string, string> headers)
        {
            StatusCode = statusCode;
            Body = body;
            ErrorMessage = errorMessage;
            Headers = headers;
        }
    }
}
