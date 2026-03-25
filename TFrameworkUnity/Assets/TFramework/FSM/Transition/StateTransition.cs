using System;

namespace TFramework.FSM
{
    /// <summary>
    /// ステート間の遷移情報を保持するクラス
    /// </summary>
    public class StateTransition
    {
        /// <summary>
        /// 遷移先のステートの型情報
        /// </summary>
        public Type ToState { get; }

        /// <summary>
        /// 遷移条件
        /// </summary>
        public ITransitionCondition Condition { get; }

        public StateTransition(Type toState, ITransitionCondition condition)
        {
            ToState = toState;
            Condition = condition;
        }

        public StateTransition(Type toState, Func<bool> conditionFunc)
        {
            ToState = toState;
            Condition = new FuncTransitionCondition(conditionFunc);
        }

        private class FuncTransitionCondition : ITransitionCondition
        {
            private readonly Func<bool> _conditionFunc;

            public FuncTransitionCondition(Func<bool> conditionFunc)
            {
                _conditionFunc = conditionFunc;
            }

            public bool Evaluate()
            {
                if (_conditionFunc == null) return false;
                return _conditionFunc();
            }
        }
    }
}
