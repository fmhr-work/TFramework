using System;

namespace TFramework.FSM
{
    /// <summary>
    /// ステートの基底クラス
    /// 派生クラスで必要なメソッドのみをオーバーライドして使用
    /// </summary>
    /// <typeparam name="TOwner">ステートマシンを所有するクラスの型</typeparam>
    public abstract class StateBase<TOwner> : IState<TOwner>
    {
        /// <summary>
        /// 自身を管理するステートマシン
        /// </summary>
        protected StateMachine<TOwner> StateMachine { get; private set; }

        /// <summary>
        /// ステートマシンのセット（内部用）
        /// </summary>
        internal void SetStateMachine(StateMachine<TOwner> stateMachine)
        {
            StateMachine = stateMachine;
        }

        public virtual void OnEnter(TOwner owner) { }
        public virtual void OnUpdate(TOwner owner, float deltaTime) { }
        public virtual void OnFixedUpdate(TOwner owner, float fixedDeltaTime) { }
        public virtual void OnExit(TOwner owner) { }
    }
}
