using System.IO;
using Cysharp.Threading.Tasks;
using TFramework.Debug;

namespace TFramework.SaveData
{
    /// <summary>
    /// ローカルファイルシステムにデータを保存
    /// </summary>
    public class FileStorageProvider : IStorageProvider
    {
        /// <summary>
        /// データを非同期で保存
        /// </summary>
        public async UniTask SaveAsync(string path, byte[] data)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
                await fs.WriteAsync(data, 0, data.Length);
            }
            catch (System.Exception e)
            {
                TLogger.Error($"[TFramework] File Save Error: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// データを非同期で読み込む
        /// </summary>
        public async UniTask<byte[]> LoadAsync(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                byte[] data = new byte[fs.Length];
                await fs.ReadAsync(data, 0, (int)fs.Length);
                return data;
            }
            catch (System.Exception e)
            {
                TLogger.Error($"[TFramework] File Load Error: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// データが存在するか確認する
        /// </summary>
        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        /// <summary>
        /// データを削除する
        /// </summary>
        public void Delete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// 指定ディレクトリ内の全データを削除
        /// </summary>
        public void DeleteAll(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
