using System.Threading;
using Cysharp.Threading.Tasks;

namespace TFramework.SaveData
{
    /// <summary>
    /// SaveData Service Interface
    /// データの保存・読み込み・削除を行う
    /// </summary>
    public interface ISaveDataService
    {
        /// <summary>
        /// データを保存する
        /// </summary>
        /// <typeparam name="T">データの型</typeparam>
        /// <param name="key">保存キー</param>
        /// <param name="data">保存するデータ</param>
        /// <param name="token">CancellationToken</param>
        UniTask SaveAsync<T>(string key, T data, CancellationToken token = default);

        /// <summary>
        /// データを読み込む
        /// </summary>
        /// <typeparam name="T">データの型</typeparam>
        /// <param name="key">保存キー</param>
        /// <param name="defaultValue">データが存在しない場合のデフォルト値</param>
        /// <param name="token">CancellationToken</param>
        UniTask<T> LoadAsync<T>(string key, T defaultValue = default, CancellationToken token = default);

        /// <summary>
        /// データが存在するか確認する
        /// </summary>
        /// <param name="key">保存キー</param>
        bool Exists(string key);

        /// <summary>
        /// データを削除する
        /// </summary>
        /// <param name="key">保存キー</param>
        void Delete(string key);

        /// <summary>
        /// 全てのデータを削除する
        /// </summary>
        void DeleteAll();
        
        /// <summary>
        /// 現在のSlot Indexを変更する
        /// </summary>
        void SetSlot(int slotIndex);
        
        /// <summary>
        /// 現在のSlot Indexを取得する
        /// </summary>
        int CurrentSlot { get; }
    }
}
