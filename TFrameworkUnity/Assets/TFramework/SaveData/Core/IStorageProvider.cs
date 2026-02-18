using Cysharp.Threading.Tasks;

namespace TFramework.SaveData
{
    /// <summary>
    /// Storage Provider Interface
    /// 自体の保存・読み込みを行う（ファイルシステム等）
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// データを保存する
        /// </summary>
        UniTask SaveAsync(string path, byte[] data);

        /// <summary>
        /// データを読み込む
        /// </summary>
        UniTask<byte[]> LoadAsync(string path);

        /// <summary>
        /// データが存在するか確認する
        /// </summary>
        bool Exists(string path);

        /// <summary>
        /// データを削除する
        /// </summary>
        void Delete(string path);
        void DeleteAll(string directoryOrPrefix);
    }
}
