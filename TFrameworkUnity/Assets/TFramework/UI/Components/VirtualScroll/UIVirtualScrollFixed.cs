using System;
using UnityEngine;
using UnityEngine.UI;
using R3;

namespace TFramework.UI
{
    /// <summary>
    /// 固定高さ仮想スクロール
    /// すべてのセルが同じ高さの場合に使用する
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public sealed class UIVirtualScrollFixed : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Scroll Settings")]
        [SerializeField]
        private ScrollRect _scrollRect;

        [SerializeField]
        private RectTransform _content;

        [SerializeField]
        private UIVirtualCell _cellPrefab;

        [SerializeField]
        private float _cellHeight = 100f;

        [SerializeField]
        private float _spacing;

        [SerializeField]
        private int _extraBuffer = 2;
        #endregion

        #region Private Fields
        private IVirtualScrollData _dataProvider;
        private readonly System.Collections.Generic.List<UIVirtualCell> _activeCells = new();
        private readonly System.Collections.Generic.Queue<UIVirtualCell> _cellPool = new();

        private int _firstVisibleIndex;
        private int _lastVisibleIndex;

        private readonly Subject<Vector2> _onScrollSubject = new();
        private CompositeDisposable _disposables;
        #endregion

        #region Properties
        public int ItemCount => _dataProvider?.ItemCount ?? 0;
        #endregion

        #region Public Methods
        /// <summary>
        /// スクロールを初期化する（ページやダイアログから呼び出す）
        /// </summary>
        public void Initialize()
        {
            _disposables = new CompositeDisposable();

            if (_scrollRect != null)
            {
                _scrollRect.onValueChanged.AsObservable()
                    .Subscribe(OnScrollValueChanged)
                    .AddTo(_disposables);
            }
        }

        public void SetData(IVirtualScrollData dataProvider)
        {
            _dataProvider = dataProvider;
            Refresh();
        }

        public void Refresh()
        {
            RecycleAllCells();
            UpdateContentHeight();
            UpdateVisibleCells();
        }

        public Observable<Vector2> OnScrollAsObservable() => _onScrollSubject;
        #endregion

        #region Private Methods
        private void OnScrollValueChanged(Vector2 position)
        {
            UpdateVisibleCells();
            _onScrollSubject.OnNext(position);
        }

        private void UpdateContentHeight()
        {
            if (_dataProvider == null)
            {
                _content.sizeDelta = new Vector2(_content.sizeDelta.x, 0);
                return;
            }

            var totalHeight = _dataProvider.ItemCount * (_cellHeight + _spacing) - _spacing;
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, Mathf.Max(0, totalHeight));
        }

        private void UpdateVisibleCells()
        {
            if (_dataProvider == null || _dataProvider.ItemCount == 0)
                return;

            var viewport = _scrollRect.viewport ?? (_scrollRect.transform as RectTransform);
            var viewportHeight = viewport.rect.height;
            var scrollY = _content.anchoredPosition.y;

            var topY = Math.Max(0, scrollY);
            var bottomY = topY + viewportHeight;

            var newFirstVisible = Mathf.Max(0, Mathf.FloorToInt(topY / (_cellHeight + _spacing)) - _extraBuffer);
            var newLastVisible = Mathf.Min(_dataProvider.ItemCount - 1, Mathf.CeilToInt(bottomY / (_cellHeight + _spacing)) + _extraBuffer);

            // 範囲外のセルをリサイクル
            for (var i = _activeCells.Count - 1; i >= 0; i--)
            {
                var cell = _activeCells[i];
                if (cell.Index < newFirstVisible || cell.Index > newLastVisible)
                {
                    RecycleCell(cell);
                    _activeCells.RemoveAt(i);
                }
            }

            // 新しいセルを作成
            for (var i = newFirstVisible; i <= newLastVisible; i++)
            {
                if (!IsCellActive(i))
                {
                    CreateCell(i);
                }
            }

            _firstVisibleIndex = newFirstVisible;
            _lastVisibleIndex = newLastVisible;
        }

        private bool IsCellActive(int index)
        {
            foreach (var cell in _activeCells)
            {
                if (cell.Index == index)
                    return true;
            }
            return false;
        }

        private void CreateCell(int index)
        {
            var cell = GetCellFromPool();
            var data = GetDataAtIndex(index);
            cell.UpdateCell(index, data);

            var rectTransform = cell.RectTransform;
            rectTransform.SetParent(_content, false);
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.sizeDelta = new Vector2(0, _cellHeight);

            var y = -index * (_cellHeight + _spacing);
            rectTransform.anchoredPosition = new Vector2(0, y);

            cell.gameObject.SetActive(true);
            _activeCells.Add(cell);
        }

        private UIVirtualCell GetCellFromPool()
        {
            if (_cellPool.Count > 0)
                return _cellPool.Dequeue();

            var go = Instantiate(_cellPrefab.gameObject, _content);
            return go.GetComponent<UIVirtualCell>();
        }

        private void RecycleCell(UIVirtualCell cell)
        {
            cell.ResetCell();
            cell.gameObject.SetActive(false);
            _cellPool.Enqueue(cell);
        }

        private void RecycleAllCells()
        {
            foreach (var cell in _activeCells)
            {
                RecycleCell(cell);
            }
            _activeCells.Clear();
        }

        private object GetDataAtIndex(int index)
        {
            // データプロバイダーからデータを取得
            // 実装はデータプロバイダーの型による
            return null;
        }
        #endregion

        #region Cleanup
        private void OnDestroy()
        {
            _disposables?.Dispose();
            _onScrollSubject.Dispose();
        }
        #endregion
    }
}
