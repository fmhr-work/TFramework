using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TFramework.Core
{
    /// <summary>
    /// シーン側で所有するMainCameraを参照するためのサービス
    /// </summary>
    public sealed class CameraService : IInitializable, System.IDisposable
    {
        private Camera _mainCamera;
        private bool _subscribed;

        /// <summary>
        /// 現在のアクティブシーンで使うMainCamera
        /// </summary>
        public Camera MainCamera => _mainCamera;

        /// <summary>
        /// 初期化時にシーンのMainCameraを取得し、以後はシーン切り替えイベントで再バインドする
        /// </summary>
        public async UniTask InitializeAsync(CancellationToken token)
        {
            if (!_subscribed)
            {
                // シーン切り替え時に毎回MainCameraを取り直す
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.activeSceneChanged += OnActiveSceneChanged;
                _subscribed = true;
            }

            // 起動直後の現在シーンに対しても一度バインドしておく
            BindCamera(SceneManager.GetActiveScene());
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 購読したシーンイベントを解除する。
        /// </summary>
        public void Dispose()
        {
            if (!_subscribed)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            _subscribed = false;
        }

        /// <summary>
        /// シーンロード直後の再バインド
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BindCamera(scene);
        }

        /// <summary>
        /// ActiveSceneが切り替わった時の再バインド
        /// </summary>
        private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
        {
            BindCamera(newScene);
        }

        /// <summary>
        /// 指定シーンからMainCameraを選択し、見つからなければFallbackCameraを作成
        /// その後、AudioListenerを含めて重複カメラを整理
        /// </summary>
        private void BindCamera(Scene scene)
        {
            var preferredCamera = FindPreferredCamera(scene);
            if (preferredCamera == null)
            {
                // シーン内にMainCameraが無い場合のみ、最小構成のFallbackCameraを作る
                preferredCamera = CreateFallbackCamera();
            }

            // MainCameraタグを統一しておく
            preferredCamera.tag = "MainCamera";

            if (preferredCamera.GetComponent<AudioListener>() == null)
            {
                preferredCamera.gameObject.AddComponent<AudioListener>();
            }

            _mainCamera = preferredCamera;
            CleanupDuplicateCameras(_mainCamera);
        }

        /// <summary>
        /// 対象シーン内から使用すべきCameraを探す
        /// 優先順位は"activeかつ有効なMainCameraタグ" → "activeなCamera" → "シーン内の任意のCamera"。
        /// </summary>
        private static Camera FindPreferredCamera(Scene scene)
        {
            var cameras = Object.FindObjectsOfType<Camera>(true);
            Camera activeSceneCamera = null;
            Camera anySceneCamera = null;

            for (int i = 0; i < cameras.Length; i++)
            {
                var camera = cameras[i];
                if (camera == null || camera.gameObject.scene != scene)
                {
                    continue;
                }

                if (anySceneCamera == null)
                {
                    anySceneCamera = camera;
                }

                if (camera.gameObject.activeInHierarchy && camera.enabled)
                {
                    if (camera.CompareTag("MainCamera"))
                    {
                        return camera;
                    }

                    if (activeSceneCamera == null)
                    {
                        activeSceneCamera = camera;
                    }
                }
            }

            return activeSceneCamera ?? anySceneCamera;
        }

        /// <summary>
        /// MainCameraが無い時の保険として、シーン内にだけ存在するCameraを生成する
        /// </summary>
        private static Camera CreateFallbackCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.depth = 0f;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        /// <summary>
        /// 現在のpreferredCamera以外のCamera/AudioListenerを無効化する。
        /// </summary>
        private static void CleanupDuplicateCameras(Camera preferredCamera)
        {
            var cameras = Object.FindObjectsOfType<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                var camera = cameras[i];
                if (camera == null || camera == preferredCamera)
                {
                    continue;
                }

                if (camera.enabled)
                {
                    camera.enabled = false;
                }

                var listener = camera.GetComponent<AudioListener>();
                if (listener != null && listener.enabled)
                {
                    listener.enabled = false;
                }
            }
        }
    }
}
