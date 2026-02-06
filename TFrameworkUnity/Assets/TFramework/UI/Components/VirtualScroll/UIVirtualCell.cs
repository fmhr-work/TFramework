using UnityEngine;

namespace TFramework.UI
{
    /// <summary>
    /// 仮想スクロールセル基底クラス
    /// </summary>
    public abstract class UIVirtualCell : MonoBehaviour
    {
        #region Private Fields
        private int _index = -1;
        private RectTransform _rectTransform;
        #endregion

        #region Properties
        public int Index => _index;

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = transform as RectTransform;
                return _rectTransform;
            }
        }
        #endregion

        #region Public Methods
        public void SetIndex(int index)
        {
            _index = index;
        }

        public abstract void UpdateCell(int index, object data);

        public virtual void ResetCell()
        {
            _index = -1;
        }
        #endregion
    }

    /// <summary>
    /// 型付き仮想スクロールセル
    /// </summary>
    /// <typeparam name="T">データの型</typeparam>
    public abstract class UIVirtualCell<T> : UIVirtualCell
    {
        #region Public Methods
        public override void UpdateCell(int index, object data)
        {
            SetIndex(index);
            if (data is T typedData)
            {
                OnUpdateCell(index, typedData);
            }
        }
        #endregion

        #region Protected Methods
        protected abstract void OnUpdateCell(int index, T data);
        #endregion
    }
}
