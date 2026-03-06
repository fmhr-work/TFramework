using System;
using System.Threading;
using TFramework.Debug;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TFramework.Resource
{
    /// <summary>
    /// Addressablesのアセットハンドル実装
    /// 参照カウントを管理し、適切なタイミングでリソースを解放
    /// </summary>
    public class AssetHandle<T> : IAssetHandle<T> where T : UnityEngine.Object
    {
        private readonly AsyncOperationHandle<T> _handle;
        private readonly Action<AssetHandle<T>> _onRelease;
        private bool _isReleased;

        /// <inheritdoc/>
        public T Asset => _isReleased ? null : _handle.Result;

        /// <inheritdoc/>
        public bool IsValid => !_isReleased && _handle.IsValid() && _handle.Status == AsyncOperationStatus.Succeeded;

        /// <inheritdoc/>
        public string Address { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="handle">Addressablesのハンドル</param>
        /// <param name="address">アセットのアドレス</param>
        /// <param name="onRelease">解放時のコールバック</param>
        internal AssetHandle(AsyncOperationHandle<T> handle, string address, Action<AssetHandle<T>> onRelease = null)
        {
            _handle = handle;
            Address = address;
            _onRelease = onRelease;
        }

        /// <inheritdoc/>
        public void Release()
        {
            if (_isReleased) return;

            _isReleased = true;

            if (_handle.IsValid())
            {
                Addressables.Release(_handle);
            }

            _onRelease?.Invoke(this);

            TLogger.Trace($"Released asset handle: {Address}", "Resource");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Release();
        }
    }
}
