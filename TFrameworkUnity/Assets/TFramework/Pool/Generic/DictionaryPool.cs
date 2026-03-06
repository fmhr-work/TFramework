using System.Collections.Generic;

namespace TFramework.Pool
{
    /// <summary>
    /// Dictionary＜TKey, TValue＞のプール
    /// 一時的な辞書が必要な場面でGC Allocを削減
    /// </summary>
    /// <typeparam name="TKey">キーの型</typeparam>
    /// <typeparam name="TValue">値の型</typeparam>
    public static class DictionaryPool<TKey, TValue>
    {
        private static readonly ObjectPool<Dictionary<TKey, TValue>> _pool = new(
            factory: () => new Dictionary<TKey, TValue>(),
            onGet: null,
            onRelease: dict => dict.Clear(),
            initialSize: 4,
            maxSize: 16
        );

        /// <summary>
        /// プールから辞書を取得する
        /// </summary>
        public static Dictionary<TKey, TValue> Get() => _pool.Get();

        /// <summary>
        /// 辞書をプールに返却する
        /// </summary>
        public static void Release(Dictionary<TKey, TValue> dict) => _pool.Release(dict);

        /// <summary>
        /// using構文で使用可能なスコープを取得
        /// </summary>
        public static PooledDictionaryScope Scope() => new(Get());

        /// <summary>
        /// using構文でDictionary&lt;TKey, TValue&gt;を自動返却するスコープ
        /// </summary>
        public readonly struct PooledDictionaryScope : System.IDisposable
        {
            /// <summary>
            /// プールから取得した辞書
            /// </summary>
            public Dictionary<TKey, TValue> Dictionary { get; }

            public PooledDictionaryScope(Dictionary<TKey, TValue> dict)
            {
                Dictionary = dict;
            }

            public void Dispose()
            {
                Release(Dictionary);
            }
        }
    }
}
