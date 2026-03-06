using UnityEngine;

namespace TFramework.Core
{
    /// <summary>
    /// TFrameworkの設定を保持するScriptableObject
    /// Project Settings から設定可能
    /// </summary>
    [CreateAssetMenu(fileName = "TFrameworkSettings", menuName = "TFramework/Settings")]
    public class TFrameworkSettings : ScriptableObject
    {
        private static TFrameworkSettings _instance;

        /// <summary>
        /// シングルトンインスタンス
        /// Resourcesフォルダから自動ロードを試みる
        /// </summary>
        public static TFrameworkSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<TFrameworkSettings>("TFrameworkSettings");
                    if (_instance == null)
                    {
                        _instance = CreateInstance<TFrameworkSettings>();
#if UNITY_EDITOR
                        UnityEngine.Debug.LogWarning(
                            "[TFramework] TFrameworkSettings not found in Resources. " +
                            "Please create one via 'Create > TFramework > Settings' and place it in a Resources folder."
                        );
#endif
                    }
                }
                return _instance;
            }
        }

        [Header("ログ設定")]
        [Tooltip("ログ出力を有効にするかどうか")]
        [SerializeField] private bool _enableLogging = true;

        [Tooltip("最小ログレベル（これ以上のレベルのみ出力）")]
        [SerializeField] private LogLevel _minimumLogLevel = LogLevel.Debug;

        [Tooltip("ログをファイルに出力するかどうか")]
        [SerializeField] private bool _enableFileLogging = false;

        [Header("リソース設定")]
        [Tooltip("アセットキャッシュの最大サイズ（MB）")]
        [SerializeField] private int _assetCacheMaxSizeMB = 256;

        [Tooltip("未使用アセットの自動解放間隔（秒）")]
        [SerializeField] private float _assetUnloadInterval = 60f;

        [Header("プール設定")]
        [Tooltip("デフォルトのプール初期サイズ")]
        [SerializeField] private int _defaultPoolInitialSize = 5;

        [Tooltip("デフォルトのプール最大サイズ")]
        [SerializeField] private int _defaultPoolMaxSize = 100;

        // プロパティ
        public bool EnableLogging => _enableLogging;
        public LogLevel MinimumLogLevel => _minimumLogLevel;
        public bool EnableFileLogging => _enableFileLogging;
        public int AssetCacheMaxSizeMB => _assetCacheMaxSizeMB;
        public float AssetUnloadInterval => _assetUnloadInterval;
        public int DefaultPoolInitialSize => _defaultPoolInitialSize;
        public int DefaultPoolMaxSize => _defaultPoolMaxSize;
    }

    /// <summary>
    /// ログレベル定義
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5,
        Off = 6
    }
}
