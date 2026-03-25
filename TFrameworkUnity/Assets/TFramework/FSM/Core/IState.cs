using System;

namespace TFramework.FSM
{
    /// <summary>
    /// ステートのインターフェース
    /// </summary>
    /// <typeparam name="TOwner">ステートマシンを所有するクラスの型</typeparam>
    public interface IState<TOwner>
    {
        /// <summary>
        /// ステート開始時に呼び出される
        /// </summary>
        /// <param name="owner">所有者</param>
        void OnEnter(TOwner owner);

        /// <summary>
        /// 毎フレーム呼び出される（Update相当）
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="deltaTime">デルタタイム</param>
        void OnUpdate(TOwner owner, float deltaTime);

        /// <summary>
        /// 固定フレーム呼び出される（FixedUpdate相当）
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="fixedDeltaTime">固定デルタタイム</param>
        void OnFixedUpdate(TOwner owner, float fixedDeltaTime);

        /// <summary>
        /// ステート終了時に呼び出される
        /// </summary>
        /// <param name="owner">所有者</param>
        void OnExit(TOwner owner);
    }
}
