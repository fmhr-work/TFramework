using System.Threading;
using Cysharp.Threading.Tasks;

namespace TFramework.Core
{
    /// <summary>
    /// Tick更新を受け取るインターフェース
    /// </summary>
    public interface ITickable
    {
        /// <summary>
        /// 毎フレーム呼び出される更新処理
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間</param>
        void Tick(float deltaTime);
    }

    /// <summary>
    /// 固定タイムステップでの更新を受け取るインターフェース
    /// 物理演算などに使用
    /// </summary>
    public interface IFixedTickable
    {
        /// <summary>
        /// 固定タイムステップで呼び出される更新処理
        /// </summary>
        /// <param name="fixedDeltaTime">固定タイムステップ</param>
        void FixedTick(float fixedDeltaTime);
    }

    /// <summary>
    /// 遅延更新LateTickを受け取るインターフェース
    /// カメラ追従などに使用
    /// </summary>
    public interface ILateTickable
    {
        /// <summary>
        /// LateUpdateで呼び出される更新処理
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間</param>
        void LateTick(float deltaTime);
    }
}
