// ============================================================================
// TFramework - UI Animation Interface
// ============================================================================

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TFramework.UI
{
    /// <summary>
    /// UIアニメーションのインターフェース
    /// </summary>
    public interface IUIAnimation
    {
        /// <summary>
        /// 表示アニメーションを再生する
        /// </summary>
        /// <param name="target">対象のCanvasGroup</param>
        /// <param name="ct">キャンセルトークン</param>
        UniTask PlayShowAsync(CanvasGroup target, CancellationToken ct);

        /// <summary>
        /// 非表示アニメーションを再生する
        /// </summary>
        /// <param name="target">対象のCanvasGroup</param>
        /// <param name="ct">キャンセルトークン</param>
        UniTask PlayHideAsync(CanvasGroup target, CancellationToken ct);
    }
}
