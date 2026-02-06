using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;

namespace TFramework.UI
{
    /// <summary>
    /// TFramework用TextMeshProUGUIラッパー
    /// ローカライズ、アニメーション、リアクティブ機能を提供
    /// </summary>
    [AddComponentMenu("TFramework/UI/TF Text - TextMeshPro")]
    public sealed class TFTextUGUI : TextMeshProUGUI
    {
        #region Serialized Fields
        [Header("Extended Settings")]
        [SerializeField]
        [Tooltip("ローカライズキーを使用する場合に設定")]
        private string _localizationKey;

        [SerializeField]
        [Tooltip("タイプライター効果の速度（文字/秒）")]
        private float _typewriterSpeed = 30f;
        #endregion

        #region Private Fields
        private readonly Subject<string> _onTextChangedSubject = new();
        private CancellationTokenSource _animationCts;
        #endregion

        #region Properties
        /// <summary>
        /// TextMeshProUGUIとしてアクセス用（互換性維持）
        /// </summary>
        public TextMeshProUGUI TextComponent => this;

        /// <summary>
        /// ローカライズキー
        /// </summary>
        public string LocalizationKey
        {
            get => _localizationKey;
            set
            {
                _localizationKey = value;
                // TODO: ローカライズサービスと連携してテキストを設定
            }
        }

        /// <summary>
        /// テキスト変更Observable
        /// </summary>
        public Observable<string> OnTextChanged => _onTextChangedSubject;
        #endregion

        #region Public Methods
        /// <summary>
        /// テキストを設定（Observableで通知）
        /// </summary>
        public void SetTextContent(string newText)
        {
            if (text == newText) return;
            text = newText;
            _onTextChangedSubject.OnNext(newText);
        }

        /// <summary>
        /// フォーマット付きテキスト設定
        /// </summary>
        public void SetFormattedText(string format, params object[] args)
        {
            SetTextContent(string.Format(format, args));
        }

        /// <summary>
        /// 数値をフォーマット付きで設定
        /// </summary>
        public void SetNumber(int number, string format = "N0")
        {
            SetTextContent(number.ToString(format));
        }

        /// <summary>
        /// 数値をフォーマット付きで設定
        /// </summary>
        public void SetNumber(float number, string format = "F2")
        {
            SetTextContent(number.ToString(format));
        }

        /// <summary>
        /// 色付きテキストを設定
        /// </summary>
        public void SetColoredText(string content, Color textColor)
        {
            var colorHex = ColorUtility.ToHtmlStringRGB(textColor);
            SetTextContent($"<color=#{colorHex}>{content}</color>");
        }

        /// <summary>
        /// タイプライター効果でテキストを表示
        /// </summary>
        public async UniTask ShowTextWithTypewriterAsync(string newText, CancellationToken ct = default)
        {
            // 既存のアニメーションをキャンセル
            CancelAnimation();
            _animationCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            text = string.Empty;
            maxVisibleCharacters = 0;

            var fullText = newText;
            text = fullText;

            try
            {
                var totalChars = fullText.Length;
                var duration = totalChars / _typewriterSpeed;
                var elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    var progress = Mathf.Clamp01(elapsed / duration);
                    maxVisibleCharacters = Mathf.FloorToInt(progress * totalChars);

                    await UniTask.Yield(PlayerLoopTiming.Update, _animationCts.Token);
                }

                maxVisibleCharacters = totalChars;
                _onTextChangedSubject.OnNext(fullText);
            }
            catch (OperationCanceledException)
            {
                // アニメーションがキャンセルされた
                maxVisibleCharacters = int.MaxValue;
            }
            finally
            {
                _animationCts?.Dispose();
                _animationCts = null;
            }
        }

        /// <summary>
        /// フェードインアニメーション
        /// </summary>
        public async UniTask FadeInAsync(float duration = 0.3f, CancellationToken ct = default)
        {
            CancelAnimation();
            _animationCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            try
            {
                var startAlpha = 0f;
                var endAlpha = 1f;
                var elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    var alphaValue = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                    color = new Color(color.r, color.g, color.b, alphaValue);

                    await UniTask.Yield(PlayerLoopTiming.Update, _animationCts.Token);
                }

                color = new Color(color.r, color.g, color.b, endAlpha);
            }
            catch (OperationCanceledException)
            {
                // アニメーションがキャンセルされた
            }
            finally
            {
                _animationCts?.Dispose();
                _animationCts = null;
            }
        }

        /// <summary>
        /// フェードアウトアニメーション
        /// </summary>
        public async UniTask FadeOutAsync(float duration = 0.3f, CancellationToken ct = default)
        {
            CancelAnimation();
            _animationCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            try
            {
                var startAlpha = color.a;
                var endAlpha = 0f;
                var elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    var alphaValue = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                    color = new Color(color.r, color.g, color.b, alphaValue);

                    await UniTask.Yield(PlayerLoopTiming.Update, _animationCts.Token);
                }

                color = new Color(color.r, color.g, color.b, endAlpha);
            }
            catch (OperationCanceledException)
            {
                // アニメーションがキャンセルされた
            }
            finally
            {
                _animationCts?.Dispose();
                _animationCts = null;
            }
        }

        /// <summary>
        /// アニメーションをキャンセル
        /// </summary>
        public void CancelAnimation()
        {
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = null;
        }

        /// <summary>
        /// テキストをクリア
        /// </summary>
        public void Clear()
        {
            SetTextContent(string.Empty);
        }
        #endregion

        #region Cleanup
        protected override void OnDestroy()
        {
            CancelAnimation();
            _onTextChangedSubject.Dispose();
            base.OnDestroy();
        }
        #endregion
    }
}
