using System;
using System.Collections.Generic;
using R3;
using TFramework.Debug;

namespace TFramework.FSM
{
    /// <summary>
    /// ステート変更イベントの情報
    /// </summary>
    /// <typeparam name="TOwner">所有者の型</typeparam>
    public struct StateChangedEvent<TOwner>
    {
        public TOwner Owner { get; }
        public IState<TOwner> PreviousState { get; }
        public IState<TOwner> CurrentState { get; }

        public StateChangedEvent(TOwner owner, IState<TOwner> previousState, IState<TOwner> currentState)
        {
            Owner = owner;
            PreviousState = previousState;
            CurrentState = currentState;
        }
    }

    /// <summary>
    /// 有限ステートマシン (FSM)
    /// </summary>
    /// <typeparam name="TOwner">ステートマシンを所有するクラスの型</typeparam>
    public class StateMachine<TOwner> : IDisposable
    {
        /// <summary>
        /// ステートマシンの所有者
        /// </summary>
        public TOwner Owner { get; }

        /// <summary>
        /// 現在のステート
        /// </summary>
        public IState<TOwner> CurrentState { get; private set; }

        /// <summary>
        /// 1つ前のステート
        /// </summary>
        public IState<TOwner> PreviousState { get; private set; }

        /// <summary>
        /// ステート変更時に発行されるイベント
        /// </summary>
        public Observable<StateChangedEvent<TOwner>> OnStateChanged => _onStateChanged;
        private readonly Subject<StateChangedEvent<TOwner>> _onStateChanged = new Subject<StateChangedEvent<TOwner>>();

        private readonly Dictionary<Type, IState<TOwner>> _states = new Dictionary<Type, IState<TOwner>>();
        private readonly Dictionary<Type, List<StateTransition>> _transitions = new Dictionary<Type, List<StateTransition>>();
        private readonly List<StateTransition> _anyTransitions = new List<StateTransition>();

        public StateMachine(TOwner owner)
        {
            Owner = owner;
        }

        public void Dispose()
        {
            _onStateChanged.Dispose();
            _states.Clear();
            _transitions.Clear();
            _anyTransitions.Clear();
        }

        /// <summary>
        /// ステートを追加
        /// </summary>
        /// <typeparam name="TState">追加するステートの型</typeparam>
        public void AddState<TState>() where TState : IState<TOwner>, new()
        {
            var state = new TState();
            AddState(state);
        }

        /// <summary>
        /// インスタンス化済みのステートを追加
        /// </summary>
        /// <param name="state">追加するステートインスタンス</param>
        public void AddState(IState<TOwner> state)
        {
            var type = state.GetType();
            if (_states.ContainsKey(type))
            {
                throw new ArgumentException($"State of type {type.Name} is already added.");
            }

            if (state is StateBase<TOwner> stateBase)
            {
                stateBase.SetStateMachine(this);
            }

            _states.Add(type, state);
        }

        /// <summary>
        /// 特定のステートからの遷移条件を追加
        /// </summary>
        /// <typeparam name="TFrom">遷移元のステート型</typeparam>
        /// <typeparam name="TTo">遷移先のステート型</typeparam>
        /// <param name="condition">遷移条件</param>
        public void AddTransition<TFrom, TTo>(Func<bool> condition)
            where TFrom : IState<TOwner>
            where TTo : IState<TOwner>
        {
            var fromType = typeof(TFrom);
            var toType = typeof(TTo);

            if (!_transitions.TryGetValue(fromType, out var transitions))
            {
                transitions = new List<StateTransition>();
                _transitions.Add(fromType, transitions);
            }

            transitions.Add(new StateTransition(toType, condition));
        }

        /// <summary>
        /// 任意のステートからの遷移条件を追加
        /// </summary>
        /// <typeparam name="TTo">遷移先のステート型</typeparam>
        /// <param name="condition">遷移条件</param>
        public void AddTransitionFromAny<TTo>(Func<bool> condition) where TTo : IState<TOwner>
        {
            _anyTransitions.Add(new StateTransition(typeof(TTo), condition));
        }

        /// <summary>
        /// 指定した型のステートへ直ちに遷移
        /// </summary>
        /// <typeparam name="TState">遷移先のステート型</typeparam>
        public void ChangeState<TState>() where TState : IState<TOwner>
        {
            ChangeState(typeof(TState));
        }

        /// <summary>
        /// 指定した型のステートへ直ちに遷移
        /// </summary>
        /// <param name="stateType">遷移先のステート型</param>
        public void ChangeState(Type stateType)
        {
            if (!_states.TryGetValue(stateType, out var nextState))
            {
                throw new ArgumentException($"State of type {stateType.Name} is not registered in the StateMachine.");
            }

            if (CurrentState != null)
            {
                CurrentState.OnExit(Owner);
                PreviousState = CurrentState;
            }

            CurrentState = nextState;
            CurrentState.OnEnter(Owner);

            TLogger.Debug($"FSM [{Owner.GetType().Name}] State Changed: {PreviousState?.GetType().Name} -> {CurrentState.GetType().Name}", "FSM");

            _onStateChanged.OnNext(new StateChangedEvent<TOwner>(Owner, PreviousState, CurrentState));
        }

        /// <summary>
        /// 毎フレーム呼び出す更新処理。遷移条件の評価とステートのUpdateを実行
        /// </summary>
        /// <param name="deltaTime">デルタタイム</param>
        public void Update(float deltaTime)
        {
            // まず Any からの遷移をチェック
            foreach (var transition in _anyTransitions)
            {
                // 自分自身のステートへの遷移は無視
                if (CurrentState != null && transition.ToState == CurrentState.GetType()) continue;

                if (transition.Condition.Evaluate())
                {
                    ChangeState(transition.ToState);
                    return; // 遷移が発生したフレームは今のステートの更新を行わない
                }
            }

            // 次に現在のステートからの遷移をチェック
            if (CurrentState != null)
            {
                var currentType = CurrentState.GetType();
                if (_transitions.TryGetValue(currentType, out var currentTransitions))
                {
                    foreach (var transition in currentTransitions)
                    {
                        if (transition.Condition.Evaluate())
                        {
                            ChangeState(transition.ToState);
                            return; // 遷移が発生したフレームは今のステートの更新を行わない
                        }
                    }
                }

                CurrentState.OnUpdate(Owner, deltaTime);
            }
        }

        /// <summary>
        /// 固定フレーム呼び出す更新処理。ステートのFixedUpdateを実行
        /// </summary>
        /// <param name="fixedDeltaTime">固定デルタタイム</param>
        public void FixedUpdate(float fixedDeltaTime)
        {
            CurrentState?.OnFixedUpdate(Owner, fixedDeltaTime);
        }
    }
}
