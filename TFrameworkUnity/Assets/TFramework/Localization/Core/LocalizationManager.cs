using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TFramework.Core;
using TFramework.Debug;

namespace TFramework.Localization
{
    /// <summary>
    /// ローカライズマネージャー
    /// </summary>
    public sealed class LocalizationManager : ILocalizationService, IInitializable, IDisposable
    {
        #region Private Fields
        private readonly LocalizationSettings _settings;
        private readonly ILocalizationProvider _provider;
        private readonly Subject<LanguageCode> _onLanguageChangedSubject;
        
        private LanguageCode _currentLanguage;
        private LocalizationTable _currentTable;
        private LocalizationTable _fallbackTable;
        #endregion

        #region Properties
        public LanguageCode CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    LoadLanguageAsync(value, CancellationToken.None).Forget();
                }
            }
        }

        public LanguageCode[] SupportedLanguages => _settings.SupportedLanguages;

        public Observable<LanguageCode> OnLanguageChanged => _onLanguageChangedSubject;
        #endregion

        #region Constructor
        public LocalizationManager(LocalizationSettings settings, ILocalizationProvider provider)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _onLanguageChangedSubject = new Subject<LanguageCode>();
            _currentLanguage = _settings.DefaultLanguage;
        }
        #endregion

        #region IInitializable
        public async UniTask InitializeAsync(CancellationToken token)
        {
            TLogger.Info($"[LocalizationManager] Initializing with default language: {_settings.DefaultLanguage}");
            await LoadLanguageAsync(_settings.DefaultLanguage, token);
        }
        #endregion

        #region ILocalizationService
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                TLogger.Warning("[LocalizationManager] Attempted to get text with null or empty key");
                return string.Empty;
            }

            // 現在の言語テーブルから取得
            if (_currentTable != null && _currentTable.ContainsKey(key))
            {
                return _currentTable.Get(key);
            }

            // フォールバックテーブルから取得
            if (_fallbackTable != null && _fallbackTable.ContainsKey(key))
            {
                if (_settings.ShowMissingKeyWarning)
                {
                    TLogger.Warning($"[LocalizationManager] Key '{key}' not found in current language, using fallback");
                }
                return _fallbackTable.Get(key);
            }

            // キーが見つからない
            if (_settings.ShowMissingKeyWarning)
            {
                TLogger.Warning($"[LocalizationManager] Key '{key}' not found in any language table");
            }
            return key;
        }

        public string Get(string key, params object[] args)
        {
            var text = Get(key);
            if (args == null || args.Length == 0)
            {
                return text;
            }

            try
            {
                return string.Format(text, args);
            }
            catch (FormatException ex)
            {
                TLogger.Error($"[LocalizationManager] Format error for key '{key}': {ex.Message}");
                return text;
            }
        }

        public bool HasKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return (_currentTable != null && _currentTable.ContainsKey(key)) ||
                   (_fallbackTable != null && _fallbackTable.ContainsKey(key));
        }

        public async UniTask LoadLanguageAsync(LanguageCode language, CancellationToken ct)
        {
            try
            {
                TLogger.Info($"[LocalizationManager] Loading language: {language}");

                // 現在の言語テーブルをロード
                _currentTable = await _provider.LoadTableAsync(language, ct);
                _currentLanguage = language;

                // フォールバックテーブルをロード（異なる場合のみ）
                if (_settings.FallbackLanguage != language)
                {
                    _fallbackTable = await _provider.LoadTableAsync(_settings.FallbackLanguage, ct);
                }
                else
                {
                    _fallbackTable = null;
                }

                // 言語変更イベント発行
                _onLanguageChangedSubject.OnNext(language);

                TLogger.Info($"[LocalizationManager] Language loaded successfully: {language}");
            }
            catch (Exception ex)
            {
                TLogger.Error($"[LocalizationManager] Failed to load language {language}: {ex.Message}", ex);
                throw;
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            _onLanguageChangedSubject?.Dispose();
            _currentTable?.Clear();
            _fallbackTable?.Clear();
        }
        #endregion
    }
}
