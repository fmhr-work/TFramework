using System;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TFramework.UI
{
    /// <summary>
    /// R3対応UIボタン
    /// クリック、長押し、ポインター状態をObservableで通知する
    /// </summary>
    [AddComponentMenu("TFramework/UI/TF Button")]
    public sealed class UIButton : Button, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Serialized Fields
        [Header("Extended Settings")]
        [SerializeField]
        [Range(0.1f, 3f)]
        private float _longPressDuration = 0.5f;
        #endregion

        #region Private Fields
        private readonly Subject<Unit> _onClickSubject = new();
        private readonly Subject<Unit> _onLongPressSubject = new();
        private readonly Subject<bool> _onPointerDownSubject = new();
        private readonly Subject<bool> _onPointerEnterSubject = new();

        private IDisposable _longPressDisposable;
        private bool _isPointerDown;
        private bool _isInitialized;
        #endregion

        #region Properties
        /// <summary>
        /// Buttonとしてアクセス用（互換性維持）
        /// </summary>
        public Button Button => this;
        
        public new bool interactable
        {
            get => base.interactable;
            set => base.interactable = value;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// ボタンを初期化する（UIManagerやページから呼び出す）
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            // 基底Buttonのクリックイベントを購読
            this.OnClickAsObservable()
                .Subscribe(_ => _onClickSubject.OnNext(Unit.Default))
                .AddTo(this);

            _isInitialized = true;
        }

        /// <summary>
        /// クリックイベントObservable
        /// </summary>
        public Observable<Unit> OnClickAsObservable() => _onClickSubject;

        /// <summary>
        /// 長押しイベントObservable
        /// </summary>
        public Observable<Unit> OnLongPressAsObservable() => _onLongPressSubject;

        /// <summary>
        /// ポインターダウン状態Observable
        /// </summary>
        public Observable<bool> OnPointerDownAsObservable() => _onPointerDownSubject;

        /// <summary>
        /// ポインターEnter状態Observable
        /// </summary>
        public Observable<bool> OnPointerEnterAsObservable() => _onPointerEnterSubject;
        #endregion

        #region IPointerHandlers
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (!interactable)
                return;

            _isPointerDown = true;
            _onPointerDownSubject.OnNext(true);

            // 長押し検出（UpdateではなくR3のTimerで実装）
            _longPressDisposable?.Dispose();
            _longPressDisposable = Observable.Timer(TimeSpan.FromSeconds(_longPressDuration))
                .Subscribe(_ =>
                {
                    if (_isPointerDown)
                    {
                        _onLongPressSubject.OnNext(Unit.Default);
                    }
                });
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            _isPointerDown = false;
            _onPointerDownSubject.OnNext(false);
            _longPressDisposable?.Dispose();
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            _onPointerEnterSubject.OnNext(true);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            _onPointerEnterSubject.OnNext(false);
        }
        #endregion

        #region Cleanup
        protected override void OnDestroy()
        {
            _longPressDisposable?.Dispose();
            _onClickSubject.Dispose();
            _onLongPressSubject.Dispose();
            _onPointerDownSubject.Dispose();
            _onPointerEnterSubject.Dispose();
        }
        #endregion
    }
}
