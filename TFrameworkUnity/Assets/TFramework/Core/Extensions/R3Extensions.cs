using System;
using System.Collections.Generic;
using R3;

namespace TFramework.Core
{
    /// <summary>
    /// R3関連の拡張メソッド
    /// </summary>
    public static class R3Extensions
    {
        /// <summary>
        /// Observable を ReadOnlyReactiveProperty に変換
        /// 初期値なしの場合はデフォルト値を使用
        /// </summary>
        public static ReadOnlyReactiveProperty<T> ToReadOnlyReactiveProperty<T>(
            this Observable<T> source,
            T initialValue = default)
        {
            var prop = new ReactiveProperty<T>(initialValue);
            source.Subscribe(x => prop.Value = x);
            return prop;
        }

        /// <summary>
        /// 前回の値と現在の値をペアで受け取るObservable
        /// </summary>
        public static Observable<(T Previous, T Current)> Pairwise<T>(this Observable<T> source)
        {
            return source.Scan(
                (Previous: default(T), Current: default(T)),
                (acc, current) => (acc.Current, current)
            ).Skip(1); // 最初のデフォルト値ペアをスキップ
        }

        /// <summary>
        /// 条件が真の間だけ値を通すObservable
        /// </summary>
        public static Observable<T> WhereTrue<T>(this Observable<T> source, Func<T, bool> predicate)
        {
            return source.Where(predicate);
        }

        /// <summary>
        /// nullでない値のみを通すObservable
        /// </summary>
        public static Observable<T> WhereNotNull<T>(this Observable<T> source) where T : class
        {
            return source.Where(x => x != null);
        }

        /// <summary>
        /// 指定した型にキャストできる値のみを通すObservable
        /// </summary>
        public static Observable<TResult> OfType<TSource, TResult>(this Observable<TSource> source)
            where TResult : TSource
        {
            return source.Where(x => x is TResult).Select(x => (TResult)(object)x);
        }

        /// <summary>
        /// 値が変更されたときのみ通知するObservable（カスタム比較）
        /// </summary>
        public static Observable<T> DistinctUntilChangedBy<T, TKey>(
            this Observable<T> source,
            IEqualityComparer<T> keySelector)
        {
            return source.DistinctUntilChanged(keySelector);
        }

        /// <summary>
        /// エラーをキャッチして代替値を返すObservable
        /// </summary>
        public static Observable<T> CatchAndReturn<T>(this Observable<T> source, T fallbackValue)
        {
            return source.Catch<T, Exception>(_ => Observable.Return(fallbackValue));
        }

        /// <summary>
        /// エラーをキャッチしてログ出力し、空のObservableを返す
        /// </summary>
        public static Observable<T> CatchAndLogError<T>(this Observable<T> source, string context = null)
        {
            return source.Catch<T, Exception>(ex =>
            {
                var message = string.IsNullOrEmpty(context)
                    ? $"[TFramework] Observable error: {ex}"
                    : $"[TFramework] Observable error in {context}: {ex}";
                UnityEngine.Debug.LogError(message);
                return Observable.Empty<T>();
            });
        }

        /// <summary>
        /// 購読時に初期値を即座に発行するObservable
        /// </summary>
        public static Observable<T> StartWithValue<T>(this Observable<T> source, T initialValue)
        {
            return Observable.Return(initialValue).Concat(source);
        }
    }
}
