using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TFramework.Scene
{
    /// <summary>
    /// シーンの基底クラス
    /// 各シーンのルートオブジェクト(Entry Point)として機能する
    /// </summary>
    public abstract class SceneControllerBase : MonoBehaviour, ISceneLifecycle, IAsyncStartable
    {
        private bool _isInitialized;
        protected ISceneService SceneService;
        private Core.TFrameworkBootstrap _bootstrap;

        [Inject]
        public void Construct(ISceneService sceneService, Core.TFrameworkBootstrap bootstrap)
        {
            SceneService = sceneService;
            _bootstrap = bootstrap;
        }

        #region VContainer Entry Point
        /// <summary>
        /// VContainerによって初期化時に呼び出される
        /// ここでシーンパラメータを取得し、ライフサイクルを開始する
        /// </summary>
        public async UniTask StartAsync(CancellationToken cancellation)
        {
            // TFrameworkの初期化完了を待機
            if (_bootstrap != null)
            {
                await UniTask.WaitUntil(() => _bootstrap.IsInitialized, cancellationToken: cancellation);
            }

            // 現在のシーン名を取得
            var sceneName = gameObject.scene.name;
            
            // パラメータを取得
            var bridgeData = SceneService.GetSceneBridgeData(sceneName);
            
            // 初期化実行
            await OnInitializeAsync(bridgeData, cancellation);
            
            // アクティブ化
            OnActivate();
        }
        #endregion

        #region ISceneLifecycle
        public async UniTask OnInitializeAsync(ISceneBridgeData bridgeData, CancellationToken ct)
        {
            if (_isInitialized) return;
            await OnInitializeInternalAsync(bridgeData, ct);
            _isInitialized = true;
        }

        public void OnActivate()
        {
            OnActivateInternal();
        }

        public void OnDeactivate()
        {
            OnDeactivateInternal();
        }
        
        public void OnTerminate()
        {
            OnTerminateInternal();
        }
        #endregion

        #region Virtual Methods
        protected virtual UniTask OnInitializeInternalAsync(ISceneBridgeData bridgeData, CancellationToken ct) => UniTask.CompletedTask;
        protected virtual void OnActivateInternal() { }
        protected virtual void OnDeactivateInternal() { }
        protected virtual void OnTerminateInternal() { }
        #endregion
    }
}
