using System;
using System.Collections.Generic;
using TFramework.Debug;
using UnityEngine;

namespace TFramework.Resource
{
    /// <summary>
    /// 複数のアセットハンドルをグループとして管理
    /// 一括解放が可能
    /// </summary>
    public class AssetHandleGroup : IDisposable
    {
        private readonly List<IDisposable> _handles = new();
        private bool _isDisposed;

        /// <summary>
        /// グループに含まれるハンドルの数
        /// </summary>
        public int Count => _handles.Count;

        /// <summary>
        /// ハンドルをグループに追加
        /// </summary>
        public void Add<T>(IAssetHandle<T> handle) where T : UnityEngine.Object
        {
            if (_isDisposed)
            {
                TLogger.Warning("Attempting to add handle to disposed group.", "Resource");
                return;
            }

            if (handle != null)
            {
                _handles.Add(handle);
            }
        }

        /// <summary>
        /// IDisposableをグループに追加
        /// </summary>
        public void Add(IDisposable disposable)
        {
            if (_isDisposed)
            {
                TLogger.Warning("Attempting to add to disposed group.", "Resource");
                return;
            }

            if (disposable != null)
            {
                _handles.Add(disposable);
            }
        }

        /// <summary>
        /// すべてのハンドルを解放
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var handle in _handles)
            {
                try
                {
                    handle.Dispose();
                }
                catch (Exception ex)
                {
                    TLogger.Error("Error releasing handle in group.", ex, "Resource");
                }
            }

            _handles.Clear();
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            ReleaseAll();
            _isDisposed = true;
        }
    }
}
