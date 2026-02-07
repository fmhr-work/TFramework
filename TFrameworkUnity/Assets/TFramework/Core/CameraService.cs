using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TFramework.Core
{
    /// <summary>
    /// カメラ管理サービス
    /// </summary>
    public class CameraService : IInitializable
    {
        private Camera _mainCamera;
        
        public Camera MainCamera => _mainCamera;

        public async UniTask InitializeAsync(CancellationToken ct)
        {
            // シーン内のMain Cameraを検索
            _mainCamera = Camera.main;
            
            if (_mainCamera == null)
            {
                CreateDefaultCamera();
            }
            else
            {
                // Main CameraをDontDestroyOnLoadに設定
                Object.DontDestroyOnLoad(_mainCamera.gameObject);
            }
            
            await UniTask.CompletedTask;
        }

        private void CreateDefaultCamera()
        {
            var cameraObj = new GameObject("Main Camera");
            _mainCamera = cameraObj.AddComponent<Camera>();
            _mainCamera.tag = "MainCamera";
            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            _mainCamera.backgroundColor = Color.black;
            _mainCamera.orthographic = true;
            _mainCamera.orthographicSize = 5;
            _mainCamera.depth = 0;
            
            Object.DontDestroyOnLoad(cameraObj);
        }
    }
}
