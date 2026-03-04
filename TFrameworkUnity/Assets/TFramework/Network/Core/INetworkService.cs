using System.Threading;
using Cysharp.Threading.Tasks;

namespace TFramework.Network
{
    /// <summary>
    /// ネットワーク通信サービスを定義するインターフェース
    /// </summary>
    public interface INetworkService
    {
        /// <summary>
        /// APIリクエストの送信
        /// </summary>
        UniTask<TResponse> SendAsync<TApi, TRequest, TResponse>(
            ApiBase<TApi, TRequest, TResponse> api,
            TRequest request,
            CancellationToken token = default)
            where TApi : ApiBase<TApi, TRequest, TResponse>
            where TRequest : RequestBase;
    }
}
