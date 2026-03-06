using System;
using UnityEngine;

namespace TFramework.Resource
{
    /// <summary>
    /// アセットハンドルのインターフェース
    /// 参照カウント付きでリソースの解放を管理
    /// </summary>
    /// <typeparam name="T">アセットの型</typeparam>
    public interface IAssetHandle<out T> : IDisposable where T : UnityEngine.Object
    {
        /// <summary>
        /// ロードされたアセット
        /// </summary>
        T Asset { get; }

        /// <summary>
        /// ハンドルが有効かどうか
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// アセットのアドレス
        /// </summary>
        string Address { get; }

        /// <summary>
        /// 参照を解放する
        /// </summary>
        void Release();
    }
}
