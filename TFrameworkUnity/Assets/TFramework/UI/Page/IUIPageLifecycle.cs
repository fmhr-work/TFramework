// ============================================================================
// TFramework - UI Page Lifecycle Interface
// ============================================================================

using System.Threading;
using Cysharp.Threading.Tasks;

namespace TFramework.UI
{
    /// <summary>
    /// ページのライフサイクルインターフェース
    /// 各メソッドはページの状態遷移時に呼び出される
    /// </summary>
    public interface IUIPageLifecycle
    {
        /// <summary>
        /// 初期化処理。ページが最初に表示される前に一度だけ呼び出される
        /// </summary>
        /// <param name="ct">キャンセルトークン</param>
        UniTask OnInitializeAsync(CancellationToken ct);

        /// <summary>
        /// ページが表示される直前に呼び出される
        /// </summary>
        /// <param name="param">前のページから渡されたパラメータ</param>
        /// <param name="ct">キャンセルトークン</param>
        UniTask OnPreOpenAsync(object param, CancellationToken ct);

        /// <summary>
        /// ページが表示された直後に呼び出される
        /// </summary>
        void OnOpened();

        /// <summary>
        /// ページが非表示になる直前に呼び出される
        /// </summary>
        /// <param name="ct">キャンセルトークン</param>
        UniTask OnPreCloseAsync(CancellationToken ct);

        /// <summary>
        /// ページが非表示になった直後に呼び出される
        /// </summary>
        void OnClosed();

        /// <summary>
        /// ページが破棄される時に呼び出される
        /// </summary>
        void OnTerminate();

        /// <summary>
        /// 戻るボタンが押された時に呼び出される
        /// </summary>
        /// <returns>戻る処理を行う場合はtrue、デフォルトの戻る処理を行う場合はfalse</returns>
        bool OnBackPressed();
    }
}
