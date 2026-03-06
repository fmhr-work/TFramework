using System.Collections.Generic;

namespace TFramework.Pool
{
    /// <summary>
    /// List&lt;T&gt;のプール
    /// 一時的なリストが必要な場面でGC Allocを削減
    /// </summary>
    /// <typeparam name="T">リストの要素型</typeparam>
    public static class ListPool<T>
    {
        private static readonly ObjectPool<List<T>> _pool = new(
            factory: () => new List<T>(),
            onGet: null,
            onRelease: list => list.Clear(),
            initialSize: 4,
            maxSize: 32
        );

        /// <summary>
        /// プールからリストを取得する
        /// </summary>
        public static List<T> Get() => _pool.Get();

        /// <summary>
        /// リストをプールに返却する
        /// </summary>
        public static void Release(List<T> list) => _pool.Release(list);

        /// <summary>
        /// using構文で使用可能なスコープを取得
        /// </summary>
        public static PooledListScope Scope() => new(Get());

        /// <summary>
        /// using構文でList&lt;T&gt;を自動返却するスコープ
        /// </summary>
        public readonly struct PooledListScope : System.IDisposable
        {
            /// <summary>
            /// プールから取得したリスト
            /// </summary>
            public List<T> List { get; }

            public PooledListScope(List<T> list)
            {
                List = list;
            }

            public void Dispose()
            {
                Release(List);
            }
        }
    }
}
