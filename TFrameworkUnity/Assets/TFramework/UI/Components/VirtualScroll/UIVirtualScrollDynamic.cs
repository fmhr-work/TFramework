using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace TFramework.UI
{
    /// <summary>
    /// 可変高さ仮想スクロール
    /// セルごとに高さが異なる場合に使用する
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public sealed class UIVirtualScrollDynamic : MonoBehaviour
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
        private float _spacing;

        [SerializeField]
        private int _extraBuffer = 2;
        #endregion

        #region Private Fields
        private IVirtualScrollData _dataProvider;
        private readonly List<UIVirtualCell> _activeCells = new();
        private readonly Queue<UIVirtualCell> _cellPool = new();
        private readonly List<float> _cellHeights = new();
        private readonly List<float> _cellPositions = new();

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
        /// スクロールを初期化する
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

        public void SetData(IVirtualScrollData dataProvider, List<float> cellHeights)
        {
            _dataProvider = dataProvider;
            _cellHeights.Clear();
            _cellHeights.AddRange(cellHeights);
            CalculateCellPositions();
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

        private void CalculateCellPositions()
        {
            _cellPositions.Clear();
            var currentY = 0f;

            for (var i = 0; i < _cellHeights.Count; i++)
            {
                _cellPositions.Add(currentY);
                currentY += _cellHeights[i] + _spacing;
            }
        }

        private void UpdateContentHeight()
        {
            if (_cellPositions.Count == 0)
            {
                _content.sizeDelta = new Vector2(_content.sizeDelta.x, 0);
                return;
            }

            var lastIndex = _cellPositions.Count - 1;
            var totalHeight = _cellPositions[lastIndex] + _cellHeights[lastIndex];
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, totalHeight);
        }

        private void UpdateVisibleCells()
        {
            if (_dataProvider == null || _dataProvider.ItemCount == 0)
                return;

            var viewport = _scrollRect.viewport ?? (_scrollRect.transform as RectTransform);
            var viewportHeight = viewport.rect.height;
            var scrollY = _content.anchoredPosition.y;

            var topY = Mathf.Max(0, scrollY);
            var bottomY = topY + viewportHeight;

            var newFirstVisible = FindIndexAtPosition(topY);
            var newLastVisible = FindIndexAtPosition(bottomY);

            newFirstVisible = Mathf.Max(0, newFirstVisible - _extraBuffer);
            newLastVisible = Mathf.Min(_dataProvider.ItemCount - 1, newLastVisible + _extraBuffer);

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

        private int FindIndexAtPosition(float y)
        {
            for (var i = 0; i < _cellPositions.Count; i++)
            {
                if (_cellPositions[i] + _cellHeights[i] >= y)
                    return i;
            }
            return _cellPositions.Count - 1;
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
            if (index < 0 || index >= _cellHeights.Count)
                return;

            var cell = GetCellFromPool();
            var data = GetDataAtIndex(index);
            cell.UpdateCell(index, data);

            var rectTransform = cell.RectTransform;
            rectTransform.SetParent(_content, false);
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.sizeDelta = new Vector2(0, _cellHeights[index]);
            rectTransform.anchoredPosition = new Vector2(0, -_cellPositions[index]);

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
