using System.Text;

namespace TFramework.Pool
{
    /// <summary>
    /// StringBuilderのプール
    /// 文字列連結が必要な場面でGC Allocを削減
    /// </summary>
    public static class StringBuilderPool
    {
        private const int DefaultCapacity = 256;
        private const int MaxCapacity = 4096;

        private static readonly ObjectPool<StringBuilder> _pool = new(
            factory: () => new StringBuilder(DefaultCapacity),
            onGet: null,
            onRelease: sb =>
            {
                // 巨大なStringBuilderはプールに戻さない
                if (sb.Capacity > MaxCapacity)
                {
                    sb.Capacity = DefaultCapacity;
                }
                sb.Clear();
            },
            initialSize: 4,
            maxSize: 16
        );

        /// <summary>
        /// プールからStringBuilderを取得する
        /// </summary>
        public static StringBuilder Get() => _pool.Get();

        /// <summary>
        /// StringBuilderをプールに返却する
        /// </summary>
        public static void Release(StringBuilder sb) => _pool.Release(sb);

        /// <summary>
        /// StringBuilderを取得し、文字列に変換してから返却する
        /// 便利メソッド
        /// </summary>
        public static string ToStringAndRelease(StringBuilder sb)
        {
            var result = sb.ToString();
            Release(sb);
            return result;
        }

        /// <summary>
        /// using構文で使用可能なスコープを取得
        /// </summary>
        public static PooledStringBuilderScope Scope() => new(Get());

        /// <summary>
        /// using構文でStringBuilderを自動返却するスコープ
        /// </summary>
        public readonly struct PooledStringBuilderScope : System.IDisposable
        {
            /// <summary>
            /// プールから取得したStringBuilder
            /// </summary>
            public StringBuilder Builder { get; }

            public PooledStringBuilderScope(StringBuilder sb)
            {
                Builder = sb;
            }

            /// <summary>
            /// 現在の内容を文字列として取得
            /// </summary>
            public override string ToString() => Builder.ToString();

            public void Dispose()
            {
                Release(Builder);
            }
        }
    }
}
