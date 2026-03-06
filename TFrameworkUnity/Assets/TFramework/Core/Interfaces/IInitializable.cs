using System.Threading;
using Cysharp.Threading.Tasks;

namespace TFramework.Core
{
    /// <summary>
    /// 非同期初期化をサポートするインターフェース
    /// VContainerのIAsyncStartableの代替として使用
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// 非同期初期化処理
        /// </summary>
        UniTask InitializeAsync(CancellationToken token);
    }

    /// <summary>
    /// 初期化の優先順位を指定するためのインターフェース
    /// </summary>
    public interface IInitializablePriority : IInitializable
    {
        /// <summary>
        /// 初期化の優先順位
        /// </summary>
        int Priority { get; }
    }
}
