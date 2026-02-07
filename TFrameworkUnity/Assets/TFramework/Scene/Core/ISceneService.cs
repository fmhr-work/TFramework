using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TFramework.Core;

namespace TFramework.Scene
{
    /// <summary>
    /// シーン遷移サービスのインターフェース
    /// </summary>
    public interface ISceneService : IService
    {
        /// <summary>
        /// 現在のアクティブなシーン名
        /// </summary>
        ReadOnlyReactiveProperty<string> CurrentScene { get; }

        /// <summary>
        /// シーンをロードする
        /// </summary>
        /// <param name="sceneName">シーン名（Addressablesアドレスまたはビルドインデックス名）</param>
        /// <param name="bridgeData">シーンに渡すパラメータ</param>
        /// <param name="useLoading">ローディング画面を表示するか</param>
        UniTask LoadSceneAsync(string sceneName, ISceneBridgeData bridgeData = null, bool useLoading = true, CancellationToken ct = default);

        /// <summary>
        /// シーンを追加ロードする (Additive)
        /// </summary>
        UniTask LoadSceneAdditiveAsync(string sceneName, ISceneBridgeData bridgeData = null, CancellationToken ct = default);

        /// <summary>
        /// シーンパラメータを取得する
        /// </summary>
        ISceneBridgeData GetSceneBridgeData(string sceneName);

        /// <summary>
        /// シーンをアンロードする
        /// </summary>
        UniTask UnloadSceneAsync(string sceneName, CancellationToken ct = default);
    }
}
