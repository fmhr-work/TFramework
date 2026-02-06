using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TFramework.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TFramework.Resource
{
    /// <summary>
    /// リソースサービスのインターフェース
    /// Addressablesを抽象化し、リソースの読み込み・解放を管理
    /// </summary>
    public interface IResourceService : IService
    {
        /// <summary>
        /// アセットがロード済みかどうか
        /// </summary>
        bool IsLoaded(string address);

        /// <summary>
        /// アセットが存在するかどうか（軽量チェック）
        /// </summary>
        UniTask<bool> ExistsAsync(string address, CancellationToken ct);

        /// <summary>
        /// アセットを非同期でロードする
        /// 注意: この方法ではリソースの解放が自動で行われないため、
        /// LoadWithHandleAsync を推奨
        /// </summary>
        UniTask<T> LoadAsync<T>(string address, CancellationToken ct) where T : UnityEngine.Object;

        /// <summary>
        /// アセットをハンドル付きで非同期ロードする（推奨）
        /// ハンドルを使用してリソースの解放を管理
        /// </summary>
        UniTask<IAssetHandle<T>> LoadWithHandleAsync<T>(string address, CancellationToken ct) where T : UnityEngine.Object;

        /// <summary>
        /// プレハブをインスタンス化する
        /// </summary>
        UniTask<GameObject> InstantiateAsync(string address, Transform parent, CancellationToken ct);

        /// <summary>
        /// プレハブをインスタンス化する（位置・回転指定）
        /// </summary>
        UniTask<GameObject> InstantiateAsync(string address, Vector3 position, Quaternion rotation, Transform parent, CancellationToken ct);

        /// <summary>
        /// プレハブをインスタンス化して指定コンポーネントを取得する
        /// </summary>
        /// <typeparam name="T">取得するコンポーネントの型</typeparam>
        UniTask<T> InstantiateAsync<T>(string address, Transform parent, CancellationToken ct) where T : Component;

        /// <summary>
        /// プレハブをインスタンス化して指定コンポーネントを取得する（位置・回転指定）
        /// </summary>
        /// <typeparam name="T">取得するコンポーネントの型</typeparam>
        UniTask<T> InstantiateAsync<T>(string address, Vector3 position, Quaternion rotation, Transform parent, CancellationToken ct) where T : Component;

        /// <summary>
        /// シーンを非同期でロードする
        /// </summary>
        UniTask LoadSceneAsync(string address, LoadSceneMode mode, CancellationToken ct);

        /// <summary>
        /// ハンドルを解放する
        /// </summary>
        void Release(object handle);

        /// <summary>
        /// アドレスで指定されたアセットを解放する
        /// </summary>
        void ReleaseByAddress(string address);

        /// <summary>
        /// InstantiateAsyncで生成されたGameObjectを解放する
        /// </summary>
        void ReleaseInstance(GameObject instance);

        /// <summary>
        /// ダウンロードサイズを取得する
        /// </summary>
        UniTask<long> GetDownloadSizeAsync(string address, CancellationToken ct);

        /// <summary>
        /// アセットをダウンロードする
        /// </summary>
        UniTask DownloadAsync(string address, IProgress<float> progress, CancellationToken ct);

        /// <summary>
        /// ラベルで指定されたアセットをダウンロードする
        /// </summary>
        UniTask DownloadByLabelAsync(string label, IProgress<float> progress, CancellationToken ct);

        /// <summary>
        /// 未使用のアセットを解放する
        /// </summary>
        void UnloadUnusedAssets();
    }
}
