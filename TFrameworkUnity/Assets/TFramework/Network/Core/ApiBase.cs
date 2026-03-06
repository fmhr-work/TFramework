using System.Threading;
using Cysharp.Threading.Tasks;

namespace TFramework.Network
{
    /// <summary>
    /// API定義の基底クラス
    /// </summary>
    /// <typeparam name="TApi">このAPIクラス自体の型</typeparam>
    /// <typeparam name="TRequest">リクエスト型</typeparam>
    /// <typeparam name="TResponse">レスポンス型</typeparam>
    public abstract class ApiBase<TApi, TRequest, TResponse>
        where TApi : ApiBase<TApi, TRequest, TResponse>
        where TRequest : RequestBase
    {
        /// <summary>
        /// このAPIリクエストを送信
        /// </summary>
        public async UniTask<TResponse> SendAsync(TRequest request, CancellationToken token = default)
        {
            if (NetworkManager.Instance == null)
            {
                throw new NetworkException("NetworkManager is not initialized.");
            }
            return await NetworkManager.Instance.SendAsync<TApi, TRequest, TResponse>(this, request, token);
        }
    }
}
