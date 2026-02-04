using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Core;
using UnityEngine;

namespace TFramework.Pool
{
    /// <summary>
    /// プール統計情報
    /// </summary>
    public struct PoolStats
    {
        /// <summary>プール内の利用可能なオブジェクト数</summary>
        public int AvailableCount;

        /// <summary>現在使用中のオブジェクト数</summary>
        public int ActiveCount;

        /// <summary>プールの最大サイズ</summary>
        public int MaxSize;

        /// <summary>合計生成数</summary>
        public int TotalCreated;

        /// <summary>合計スポーン回数</summary>
        public int TotalSpawned;

        /// <summary>合計デスポーン回数</summary>
        public int TotalDespawned;
    }

    /// <summary>
    /// GameObjectプール管理のインターフェース
    /// </summary>
    public interface IPoolManager : IService
    {
        /// <summary>
        /// プールからGameObjectを取得する
        /// </summary>
        /// <param name="key">プールのキー（プレハブ名など）</param>
        /// <param name="position">配置位置</param>
        /// <param name="rotation">回転</param>
        /// <param name="parent">親Transform</param>
        /// <returns>取得したGameObject</returns>
        GameObject Spawn(string key, Vector3 position = default, Quaternion rotation = default, Transform parent = null);

        /// <summary>
        /// プールからコンポーネントを取得する
        /// </summary>
        T Spawn<T>(string key, Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : Component;

        /// <summary>
        /// GameObjectをプールに返却する
        /// </summary>
        /// <param name="obj">返却するGameObject</param>
        /// <param name="delay">遅延時間（秒）</param>
        void Despawn(GameObject obj, float delay = 0f);

        /// <summary>
        /// 指定したオブジェクトを事前に生成する
        /// </summary>
        /// <param name="key">プールのキー</param>
        /// <param name="count">生成数</param>
        /// <param name="ct">キャンセルトークン</param>
        UniTask PrewarmAsync(string key, int count, CancellationToken ct);

        /// <summary>
        /// 新しいプールを作成する
        /// </summary>
        /// <param name="key">プールのキー</param>
        /// <param name="prefab">プレハブ</param>
        /// <param name="initialSize">初期サイズ</param>
        /// <param name="maxSize">最大サイズ</param>
        void CreatePool(string key, GameObject prefab, int initialSize = 0, int maxSize = 100);

        /// <summary>
        /// 指定したプールをクリアする
        /// </summary>
        void ClearPool(string key);

        /// <summary>
        /// すべてのプールをクリアする
        /// </summary>
        void ClearAll();

        /// <summary>
        /// プールの統計情報を取得する
        /// </summary>
        PoolStats GetStats(string key);

        /// <summary>
        /// 指定したキーのプールが存在するかどうか
        /// </summary>
        bool HasPool(string key);
    }
}
