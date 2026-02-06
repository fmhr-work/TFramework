using System.Collections.Generic;
using TFramework.Debug;
using UnityEngine;
using UnityEngine.UI;

namespace TFramework.UI
{
    /// <summary>
    /// UIルートCanvas管理
    /// 各UI層のコンテナを管理し、UI要素の親として機能する
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public sealed class UIRoot : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Layer Containers")]
        [SerializeField]
        private RectTransform _backgroundContainer;

        [SerializeField]
        private RectTransform _pageContainer;

        [SerializeField]
        private RectTransform _popupContainer;

        [SerializeField]
        private RectTransform _dialogContainer;

        [SerializeField]
        private RectTransform _toastContainer;

        [SerializeField]
        private RectTransform _loadingContainer;

        [SerializeField]
        private RectTransform _systemContainer;
        #endregion

        #region Private Fields
        private readonly Dictionary<UILayer, RectTransform> _layerContainers = new();
        #endregion

        #region Properties
        public Canvas Canvas { get; private set; }
        public CanvasScaler CanvasScaler { get; private set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// UIRootを初期化する（UIManagerから呼び出される）
        /// </summary>
        public void Initialize(Canvas canvas, CanvasScaler canvasScaler)
        {
            Canvas = canvas;
            CanvasScaler = canvasScaler;

            InitializeLayerContainers();
        }

        /// <summary>
        /// 指定した層のコンテナを取得する
        /// </summary>
        public RectTransform GetLayerContainer(UILayer layer)
        {
            if (_layerContainers.TryGetValue(layer, out var container))
            {
                return container;
            }

            TLogger.Warning($"[UIRoot] Container for layer {layer} not found");
            return _pageContainer;
        }

        /// <summary>
        /// UI要素を指定した層に配置する
        /// </summary>
        public void SetParent(RectTransform element, UILayer layer)
        {
            var container = GetLayerContainer(layer);
            element.SetParent(container, false);
        }
        #endregion

        #region Private Methods
        private void InitializeLayerContainers()
        {
            _layerContainers[UILayer.Background] = _backgroundContainer;
            _layerContainers[UILayer.Page] = _pageContainer;
            _layerContainers[UILayer.Popup] = _popupContainer;
            _layerContainers[UILayer.Dialog] = _dialogContainer;
            _layerContainers[UILayer.Toast] = _toastContainer;
            _layerContainers[UILayer.Loading] = _loadingContainer;
            _layerContainers[UILayer.System] = _systemContainer;
        }
        #endregion
    }
}
