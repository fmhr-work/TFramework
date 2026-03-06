using System.Threading;
using Cysharp.Threading.Tasks;

namespace TFramework.UI
{
    /// <summary>
    /// ダイアログのライフサイクルインターフェース
    /// </summary>
    public interface IUIDialog
    {
        /// <summary>
        /// ダイアログが表示される直前に呼び出される
        /// </summary>
        /// <param name="param">パラメータ</param>
        /// <param name="ct">キャンセルトークン</param>
        UniTask OnPreOpenAsync(object param, CancellationToken ct);

        /// <summary>
        /// ダイアログが表示された直後に呼び出される
        /// </summary>
        void OnOpened();

        /// <summary>
        /// ダイアログが非表示になる直前に呼び出される
        /// </summary>
        /// <param name="ct">キャンセルトークン</param>
        UniTask OnPreCloseAsync(CancellationToken ct);

        /// <summary>
        /// ダイアログが非表示になった直後に呼び出される
        /// </summary>
        void OnClosed();
    }
}
