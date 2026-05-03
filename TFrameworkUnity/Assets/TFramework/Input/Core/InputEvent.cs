using System;
using UnityEngine;

namespace TFramework.Input
{
    /// <summary>
    /// 入力アクションの識別子
    /// </summary>
    public enum GameInputAction
    {
        Move,
        Look,
        Submit,
        Cancel,
        Jump,
        Dash,
        Attack,
        Menu,
        Pause,
        Interact,
        Point,
        Hold,
    }

    /// <summary>
    /// 入力フェーズ
    /// </summary>
    public enum InputPhase
    {
        Started,
        Performed,
        Canceled,
    }

    /// <summary>
    /// 入力イベントの構造体
    /// </summary>
    public readonly struct InputEvent : IEquatable<InputEvent>
    {
        public GameInputAction Action { get; }
        public InputPhase Phase { get; }
        public Vector2 Value { get; }
        public float Time { get; }

        public InputEvent(GameInputAction action, InputPhase phase, Vector2 value, float time)
        {
            Action = action;
            Phase = phase;
            Value = value;
            Time = time;
        }

        public bool Equals(InputEvent other)
        {
            return Action == other.Action && Phase == other.Phase && Value.Equals(other.Value);
        }

        public override bool Equals(object obj) => obj is InputEvent other && Equals(other);

        public override int GetHashCode()
        {
            return HashCode.Combine((int)Action, (int)Phase, Value.GetHashCode());
        }

        public override string ToString()
        {
            return $"[{Phase}] {Action}: {Value}";
        }
    }
}