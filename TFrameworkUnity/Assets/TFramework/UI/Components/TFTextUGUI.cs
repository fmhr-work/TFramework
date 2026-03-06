using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TFramework.Localization;
using TMPro;
using UnityEngine;
using VContainer;

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
        [Header("Localization")]
        [SerializeField]
        [Tooltip("ローカライズを使用")]
        private bool _useLocalization;

        [SerializeField]
        [Tooltip("ローカライズキー")]
        private string _localizationKey;

        [SerializeField]
        [Tooltip("ローカライズパラメーター")]
        private string[] _localizationParameters;

        [Header("Animation")]
        [SerializeField]
        [Tooltip("タイプライター効果の速度（文字/秒）")]
        private float _typewriterSpeed = 30f;
        #endregion

        #region Private Fields
        private readonly Subject<string> _onTextChangedSubject = new();
        private CancellationTokenSource _animationCts;
        private ILocalizationService _localization;
        private IDisposable _languageChangedSubscription;
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
                UpdateLocalizedText();
            }
        }

        /// <summary>
        /// テキスト変更Observable
        /// </summary>
        public Observable<string> OnTextChanged => _onTextChangedSubject;
        #endregion

        #region Localization
        /// <summary>
        /// VContainerでLocalizationServiceを注入
        /// </summary>
        [Inject]
        public void Construct(ILocalizationService localization)
        {
            _localization = localization;

            if (_useLocalization && !string.IsNullOrEmpty(_localizationKey))
            {
                UpdateLocalizedText();

                // 言語変更時に自動更新
                _languageChangedSubscription = _localization.OnLanguageChanged
                    .Subscribe(_ => UpdateLocalizedText());
            }
        }

        /// <summary>
        /// ローカライズテキストを更新
        /// </summary>
        public void UpdateLocalizedText()
        {
            if (!_useLocalization || _localization == null || string.IsNullOrEmpty(_localizationKey))
            {
                return;
            }

            if (_localizationParameters != null && _localizationParameters.Length > 0)
            {
                SetTextContent(_localization.Get(_localizationKey, _localizationParameters));
            }
            else
            {
                SetTextContent(_localization.Get(_localizationKey));
            }
        }

        /// <summary>
        /// ローカライズキーとパラメーターを設定
        /// </summary>
        public void SetLocalizationKey(string key, params string[] parameters)
        {
            _useLocalization = true;
            _localizationKey = key;
            _localizationParameters = parameters;
            
            // サブスクリプションがなければ登録
            if (_localization != null && _languageChangedSubscription == null)
            {
                _languageChangedSubscription = _localization.OnLanguageChanged
                    .Subscribe(_ => UpdateLocalizedText());
            }

            UpdateLocalizedText();
        }
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
            _languageChangedSubscription?.Dispose();
            _onTextChangedSubject.Dispose();
            base.OnDestroy();
        }
        #endregion
    }
}
