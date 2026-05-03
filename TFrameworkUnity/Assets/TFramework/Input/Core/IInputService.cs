using System;
using R3;
using TFramework.Core;

namespace TFramework.Input
{
    /// <summary>
    /// 入力サービスのインターフェース
    /// 入力イベントをObservableとして公開する
    /// </summary>
    public interface IInputService : IService, IDisposable
    {
        /// <summary>
        /// 入力イベントストリーム
        /// </summary>
        Observable<InputEvent> OnInputEvent { get; }

        /// <summary>
        /// 入力が有効かどうか
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 入力を有効化
        /// </summary>
        void SetEnabled(bool enabled);

        /// <summary>
        /// すべての入力を一時的に無効化（演出中など）
        /// </summary>
        IDisposable LockInput();
    }
}