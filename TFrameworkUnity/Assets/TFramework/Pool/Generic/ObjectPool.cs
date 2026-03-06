using System;
using System.Collections.Concurrent;

namespace TFramework.Pool
{
    /// <summary>
    /// 汎用オブジェクトプール
    /// new()制約を持つ任意の型に対して使用可能
    /// </summary>
    /// <typeparam name="T">プールする型</typeparam>
    public class ObjectPool<T> where T : class, new()
    {
        private readonly ConcurrentBag<T> _pool;
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly int _maxSize;

        /// <summary>
        /// プール内のオブジェクト数（概算）
        /// </summary>
        public int Count => _pool.Count;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="factory">オブジェクト生成ファクトリ（省略時はnew T()）</param>
        /// <param name="onGet">取得時のコールバック</param>
        /// <param name="onRelease">返却時のコールバック</param>
        /// <param name="initialSize">初期サイズ</param>
        /// <param name="maxSize">最大サイズ</param>
        public ObjectPool(
            Func<T> factory = null,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            int initialSize = 0,
            int maxSize = 100)
        {
            _pool = new ConcurrentBag<T>();
            _factory = factory ?? (() => new T());
            _onGet = onGet;
            _onRelease = onRelease;
            _maxSize = maxSize;

            // 初期サイズ分のオブジェクトを生成
            for (int i = 0; i < initialSize; i++)
            {
                _pool.Add(_factory());
            }
        }

        /// <summary>
        /// プールからオブジェクトを取得する
        /// </summary>
        public T Get()
        {
            T obj = _pool.TryTake(out var item) ? item : _factory();
            _onGet?.Invoke(obj);
            return obj;
        }

        /// <summary>
        /// オブジェクトをプールに返却する
        /// </summary>
        public void Release(T obj)
        {
            if (obj == null) return;

            _onRelease?.Invoke(obj);

            // 最大サイズを超えている場合は追加しない
            if (_pool.Count < _maxSize)
            {
                _pool.Add(obj);
            }
        }

        /// <summary>
        /// プールをクリアする
        /// </summary>
        public void Clear()
        {
            while (_pool.TryTake(out _)) { }
        }
    }
}
