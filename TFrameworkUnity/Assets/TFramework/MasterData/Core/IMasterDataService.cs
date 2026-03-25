using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TFramework.MasterData
{
    /// <summary>
    /// MasterDataサービスのインターフェース
    /// MasterDataのロード、取得、管理を行う
    /// </summary>
    public partial interface IMasterDataService
    {
        /// <summary>
        /// 初期化処理
        /// 全てのMasterDataアセットをロードする
        /// </summary>
        /// <param name="ct">キャンセレーショントークン</param>
        UniTask InitializeAsync(CancellationToken ct);

        /// <summary>
        /// 指定した型のMasterDataリストを取得する（全件）
        /// </summary>
        /// <typeparam name="T">MasterDataの型</typeparam>
        /// <returns>読み取り専用のデータリスト</returns>
        IReadOnlyList<T> GetAll<T>() where T : class, IMasterDataObject;

        /// <summary>
        /// 指定したメインキーを持つMasterDataを取得する
        /// </summary>
        /// <typeparam name="T">MasterDataの型</typeparam>
        /// <typeparam name="TKey">メインキーの型</typeparam>
        /// <param name="key">メインキー</param>
        /// <returns>見つかったデータ。存在しない場合はnull</returns>
        T Get<T, TKey>(TKey key) where T : class, IMasterDataObject<TKey>;
        
        /// <summary>
        /// 指定した型のコンテナを取得する
        /// 拡張メソッドからの利用を想定している
        /// </summary>
        /// <typeparam name="T">コンテナの型（ScriptableObject）</typeparam>
        /// <returns>コンテナインスタンス。存在しない場合はnull</returns>
        T GetContainer<T>() where T : class;

        /// <summary>
        /// サーバーから最新のMasterDataをダウンロードして更新する（オプション）
        /// </summary>
        /// <param name="ct">キャンセレーショントークン</param>
        UniTask DownloadFromServerAsync(CancellationToken ct);

        /// <summary>
        /// MasterDataを再ロードする
        /// </summary>
        /// <param name="ct">キャンセレーショントークン</param>
        UniTask ReloadAsync(CancellationToken ct);
    }
}
