using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using VContainer;

namespace TFramework.Network
{
    /// <summary>
    /// UnityWebRequestを使用したHTTPクライアント実装クラス
    /// </summary>
    public class UnityHttpClient : IHttpClient
    {
        private readonly NetworkSettings _settings;

        [Inject]
        public UnityHttpClient(NetworkSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// HTTPリクエストの送信
        /// </summary>
        public async UniTask<HttpResponse> SendAsync(HttpRequest request, CancellationToken token = default)
        {
            using (var uwr = CreateWebRequest(request))
            {
                uwr.timeout = _settings.RequestTimeout;

                try
                {
                    await uwr.SendWebRequest().ToUniTask(cancellationToken: token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    // UnityWebRequestのエラーはここでキャッチせず、レスポンスとして処理する
                }

                return CreateResponse(uwr);
            }
        }

        private UnityWebRequest CreateWebRequest(HttpRequest request)
        {
            var uwr = new UnityWebRequest(request.Url, request.Method.ToString());
            
            if (request.Body != null)
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(request.Body);
                uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
                uwr.SetRequestHeader("Content-Type", "application/json");
            }
            
            uwr.downloadHandler = new DownloadHandlerBuffer();

            foreach (var header in request.Headers)
            {
                uwr.SetRequestHeader(header.Key, header.Value);
            }

            return uwr;
        }

        private HttpResponse CreateResponse(UnityWebRequest uwr)
        {
            var headers = uwr.GetResponseHeaders() ?? new Dictionary<string, string>();
            return new HttpResponse(
                uwr.responseCode,
                uwr.downloadHandler.text,
                uwr.error,
                headers
            );
        }
    }
}
