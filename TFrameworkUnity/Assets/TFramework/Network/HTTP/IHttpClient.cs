using System.Threading;
using Cysharp.Threading.Tasks;

namespace TFramework.Network
{
    /// <summary>
    /// HTTP通信クライアントを定義するインターフェース
    /// </summary>
    public interface IHttpClient
    {
        /// <summary>
        /// HTTPリクエストの非同期送信
        /// </summary>
        UniTask<HttpResponse> SendAsync(HttpRequest request, CancellationToken token = default);
    }
}
