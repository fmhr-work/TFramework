using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Core;

namespace TFramework.UI
{
    /// <summary>
    /// UIサービスインターフェース
    /// ページナビゲーション、ダイアログ、トースト、ローディング表示を管理する
    /// </summary>
    public interface IUIService : IService, IDisposableService
    {
        #region Properties
        /// <summary>
        /// 現在表示中のページ
        /// </summary>
        UIPageBase CurrentPage { get; }

        /// <summary>
        /// ページスタックの数
        /// </summary>
        int PageStackCount { get; }

        /// <summary>
        /// ローディング表示中かどうか
        /// </summary>
        bool IsLoading { get; }
        #endregion

        #region Page Navigation
        /// <summary>
        /// ページを表示する
        /// </summary>
        /// <typeparam name="TPage">ページの型</typeparam>
        /// <param name="param">パラメータ</param>
        /// <param name="ct">キャンセルトークン</param>
        UniTask ShowPageAsync<TPage>(object param = null, CancellationToken ct = default)
            where TPage : UIPageBase;

        /// <summary>
        /// Addressableキーを指定してページを表示する
        /// </summary>
        /// <param name="address">Addressableキー</param>
        /// <param name="param">パラメータ</param>
        /// <param name="ct">キャンセルトークン</param>
        UniTask ShowPageAsync(string address, object param = null, CancellationToken ct = default);

        /// <summary>
        /// 前のページに戻る
        /// </summary>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>戻れた場合true</returns>
        UniTask<bool> GoBackAsync(CancellationToken ct = default);

        /// <summary>
        /// ページスタックをクリアする
        /// </summary>
        /// <param name="ct">キャンセルトークン</param>
        UniTask ClearStackAsync(CancellationToken ct = default);
        #endregion

        #region Dialog
        /// <summary>
        /// ダイアログを表示する（結果なし）
        /// </summary>
        /// <typeparam name="TDialog">ダイアログの型</typeparam>
        /// <param name="param">パラメータ</param>
        /// <param name="ct">キャンセルトークン</param>
        UniTask ShowDialogAsync<TDialog>(object param = null, CancellationToken ct = default)
            where TDialog : UIDialogBase;

        /// <summary>
        /// ダイアログを表示する（結果あり）
        /// </summary>
        /// <typeparam name="TDialog">ダイアログの型</typeparam>
        /// <typeparam name="TResult">結果の型</typeparam>
        /// <param name="param">パラメータ</param>
        /// <param name="ct">キャンセルトークン</param>
        UniTask<TResult> ShowDialogAsync<TDialog, TResult>(object param = null, CancellationToken ct = default)
            where TDialog : UIDialogBase<TResult>;

        /// <summary>
        /// Addressableキーを指定してダイアログを表示する
        /// </summary>
        /// <param name="address">Addressableキー</param>
        /// <param name="param">パラメータ</param>
        /// <param name="ct">キャンセルトークン</param>
        UniTask ShowDialogAsync(string address, object param = null, CancellationToken ct = default);
        #endregion

        #region Toast
        /// <summary>
        /// トーストを表示する
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <param name="duration">表示時間（秒、0以下でデフォルト）</param>
        void ShowToast(string message, float duration = 0);
        #endregion

        #region Loading
        /// <summary>
        /// ローディングを表示する
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <returns>Disposeでローディングを非表示</returns>
        IDisposable ShowLoading(string message = null);

        /// <summary>
        /// ローディングを非表示にする
        /// </summary>
        void HideLoading();
        #endregion

        #region Address Registration
        /// <summary>
        /// ページタイプとAddressableキーを登録する
        /// </summary>
        void RegisterPageAddress<TPage>(string address) where TPage : UIPageBase;

        /// <summary>
        /// ダイアログタイプとAddressableキーを登録する
        /// </summary>
        void RegisterDialogAddress<TDialog>(string address) where TDialog : UIDialogBase;
        #endregion
    }
}
