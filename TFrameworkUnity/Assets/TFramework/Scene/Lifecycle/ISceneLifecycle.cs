using System.Threading;
using Cysharp.Threading.Tasks;

namespace TFramework.Scene
{
    /// <summary>
    /// シーンのライフサイクルインターフェース
    /// シーン内のルートオブジェクトに実装する
    /// </summary>
    public interface ISceneLifecycle
    {
        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <param name="param">シーン遷移パラメータ</param>
        UniTask OnInitializeAsync(ISceneBridgeData param, CancellationToken ct);

        /// <summary>
        /// シーンがアクティブになった（トランジション終了後）
        /// </summary>
        void OnActivate();

        /// <summary>
        /// シーンが非アクティブになる（遷移開始前）
        /// </summary>
        void OnDeactivate();
        
        /// <summary>
        /// 終了処理
        /// </summary>
        void OnTerminate();
    }
}
