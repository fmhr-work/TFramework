using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Core;
using VContainer;

namespace TFramework.Network
{
    /// <summary>
    /// ネットワーク通信全体を管理するクラス
    /// </summary>
    public class NetworkManager : INetworkService, IInitializable, IDisposable
    {
        private readonly IHttpClient _httpClient;
        private readonly INetworkSerializer _serializer;
        private readonly NetworkSettings _settings;

        /// <summary>
        /// シングルトンインスタンス（ApiBaseからのアクセス用）
        /// </summary>
        public static NetworkManager Instance { get; private set; }

        [Inject]
        public NetworkManager(
            IHttpClient httpClient,
            INetworkSerializer serializer,
            NetworkSettings settings)
        {
            _httpClient = httpClient;
            _serializer = serializer;
            _settings = settings;
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        public async UniTask InitializeAsync(CancellationToken token)
        {
            Instance = this;
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// リソースの破棄
        /// </summary>
        public void Dispose()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// APIリクエストの送信
        /// </summary>
        public async UniTask<TResponse> SendAsync<TApi, TRequest, TResponse>(
            ApiBase<TApi, TRequest, TResponse> api,
            TRequest request,
            CancellationToken token = default)
            where TApi : ApiBase<TApi, TRequest, TResponse>
            where TRequest : RequestBase
        {
            if (!request.Check())
            {
                throw new NetworkException("Request validation failed.");
            }

            var baseUrl = _settings.BaseUrl;
            // 末尾の/を削除し、先頭の/を削除して結合
            var url = $"{baseUrl.TrimEnd('/')}/{request.Name.TrimStart('/')}";

            var httpRequest = new HttpRequest(url, request.Type);
            
            // GET以外の場合はBodyをセット
            if (request.Type != ApiType.GET)
            {
                httpRequest.Body = _serializer.Serialize(request);
            }

            // TODO: AuthInterceptorなどが実装されたらここでヘッダー付与などを呼ぶ

            var httpResponse = await _httpClient.SendAsync(httpRequest, token);

            if (!httpResponse.IsSuccess)
            {
                throw new NetworkException(
                    httpResponse.ErrorMessage ?? "Request failed.", 
                    httpResponse.StatusCode, 
                    httpResponse.Body
                );
            }

            try
            {
                return _serializer.Deserialize<TResponse>(httpResponse.Body);
            }
            catch (Exception ex)
            {
                throw new NetworkException("Failed to deserialize response.", ex);
            }
        }
    }
}
