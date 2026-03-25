using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TFramework.UI
{
    #region Fade Transition
    /// <summary>
    /// フェード遷移
    /// </summary>
    public sealed class FadeTransition : IUIAnimation
    {
        #region Private Fields
        private readonly float _duration;
        #endregion

        #region Constructor
        public FadeTransition(float duration = 0.3f)
        {
            _duration = duration;
        }
        #endregion

        #region IUIAnimation Implementation
        public async UniTask PlayShowAsync(CanvasGroup target, CancellationToken ct)
        {
            target.alpha = 0f;
            target.interactable = false;
            target.blocksRaycasts = true;

            var elapsed = 0f;
            while (elapsed < _duration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                target.alpha = Mathf.Clamp01(elapsed / _duration);
                await UniTask.Yield(ct);
            }

            target.alpha = 1f;
            target.interactable = true;
        }

        public async UniTask PlayHideAsync(CanvasGroup target, CancellationToken ct)
        {
            target.alpha = 1f;
            target.interactable = false;

            var elapsed = 0f;
            while (elapsed < _duration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                target.alpha = 1f - Mathf.Clamp01(elapsed / _duration);
                await UniTask.Yield(ct);
            }

            target.alpha = 0f;
            target.blocksRaycasts = false;
        }
        #endregion
    }
    #endregion

    #region Slide Transition
    /// <summary>
    /// スライド遷移
    /// </summary>
    public sealed class SlideTransition : IUIAnimation
    {
        #region Enums
        public enum Direction
        {
            Left,
            Right,
            Up,
            Down
        }
        #endregion

        #region Private Fields
        private readonly float _duration;
        private readonly Direction _direction;
        #endregion

        #region Constructor
        public SlideTransition(Direction direction = Direction.Right, float duration = 0.3f)
        {
            _direction = direction;
            _duration = duration;
        }
        #endregion

        #region IUIAnimation Implementation
        public async UniTask PlayShowAsync(CanvasGroup target, CancellationToken ct)
        {
            var rectTransform = target.transform as RectTransform;
            if (rectTransform == null)
            {
                target.alpha = 1f;
                return;
            }

            var startOffset = GetOffset(rectTransform);
            var originalPosition = rectTransform.anchoredPosition;
            rectTransform.anchoredPosition = originalPosition + startOffset;

            target.alpha = 1f;
            target.interactable = false;
            target.blocksRaycasts = true;

            var elapsed = 0f;
            while (elapsed < _duration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                var t = EaseOutCubic(Mathf.Clamp01(elapsed / _duration));
                rectTransform.anchoredPosition = Vector2.Lerp(originalPosition + startOffset, originalPosition, t);
                await UniTask.Yield(ct);
            }

            rectTransform.anchoredPosition = originalPosition;
            target.interactable = true;
        }

        public async UniTask PlayHideAsync(CanvasGroup target, CancellationToken ct)
        {
            var rectTransform = target.transform as RectTransform;
            if (rectTransform == null)
            {
                target.alpha = 0f;
                return;
            }

            var endOffset = GetOffset(rectTransform) * -1;
            var originalPosition = rectTransform.anchoredPosition;

            target.interactable = false;

            var elapsed = 0f;
            while (elapsed < _duration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                var t = EaseInCubic(Mathf.Clamp01(elapsed / _duration));
                rectTransform.anchoredPosition = Vector2.Lerp(originalPosition, originalPosition + endOffset, t);
                await UniTask.Yield(ct);
            }

            rectTransform.anchoredPosition = originalPosition;
            target.alpha = 0f;
            target.blocksRaycasts = false;
        }
        #endregion

        #region Private Methods
        private Vector2 GetOffset(RectTransform rectTransform)
        {
            var size = rectTransform.rect.size;
            return _direction switch
            {
                Direction.Left => new Vector2(-size.x, 0),
                Direction.Right => new Vector2(size.x, 0),
                Direction.Up => new Vector2(0, size.y),
                Direction.Down => new Vector2(0, -size.y),
                _ => Vector2.zero
            };
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        private static float EaseInCubic(float t) => t * t * t;
        #endregion
    }
    #endregion

    #region Scale Transition
    /// <summary>
    /// スケール遷移
    /// </summary>
    public sealed class ScaleTransition : IUIAnimation
    {
        #region Private Fields
        private readonly float _duration;
        private readonly float _startScale;
        #endregion

        #region Constructor
        public ScaleTransition(float startScale = 0.8f, float duration = 0.3f)
        {
            _startScale = startScale;
            _duration = duration;
        }
        #endregion

        #region IUIAnimation Implementation
        public async UniTask PlayShowAsync(CanvasGroup target, CancellationToken ct)
        {
            target.transform.localScale = Vector3.one * _startScale;
            target.alpha = 0f;
            target.interactable = false;
            target.blocksRaycasts = true;

            var elapsed = 0f;
            while (elapsed < _duration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                var t = EaseOutBack(Mathf.Clamp01(elapsed / _duration));
                target.transform.localScale = Vector3.Lerp(Vector3.one * _startScale, Vector3.one, t);
                target.alpha = Mathf.Clamp01(elapsed / _duration);
                await UniTask.Yield(ct);
            }

            target.transform.localScale = Vector3.one;
            target.alpha = 1f;
            target.interactable = true;
        }

        public async UniTask PlayHideAsync(CanvasGroup target, CancellationToken ct)
        {
            target.interactable = false;

            var elapsed = 0f;
            while (elapsed < _duration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / _duration);
                target.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * _startScale, t);
                target.alpha = 1f - t;
                await UniTask.Yield(ct);
            }

            target.transform.localScale = Vector3.one;
            target.alpha = 0f;
            target.blocksRaycasts = false;
        }
        #endregion

        #region Private Methods
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        #endregion
    }
    #endregion
}
