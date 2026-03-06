using System.Collections.Generic;
using UnityEngine;

namespace TFramework.MasterData
{
    /// <summary>
    /// MasterDataモジュールの設定
    /// </summary>
    [CreateAssetMenu(fileName = "MasterDataSettings", menuName = "TFramework/Settings/MasterData Settings")]
    public class MasterDataSettings : ScriptableObject
    {
        [Header("Import Settings")]
        
        [Tooltip("自動コード生成の出力先フォルダ（Assetsからの相対パス）")]
        [SerializeField] private string _codeOutputPath = "Scripts/Generated/MasterData";
        
        [Tooltip("Asset生成の出力先フォルダ（Assetsからの相対パス）")]
        [SerializeField] private string _assetOutputPath = "Data/MasterData";

        [Header("Runtime Settings")]
        [Tooltip("アプリケーション起動時に自動的にすべてのMasterDataをロードするか")]
        [SerializeField] private bool _autoLoadOnStartup = true;
        
        [Tooltip("生成されたMasterDataコンテナのリスト（Importerによって自動更新される）")]
        [SerializeField] private List<ScriptableObject> _containers = new List<ScriptableObject>();

        [Header("Server Settings (Option)")]
        [Tooltip("サーバー同期を有効にするか")]
        [SerializeField] private bool _enableServerSync;
        
        [Tooltip("MasterData配信サーバーのURL")]
        [SerializeField] private string _serverUrl = "https://example.com/api/masterdata";

        /// <summary>
        /// コード出力パスを取得する
        /// </summary>
        public string CodeOutputPath => _codeOutputPath;

        /// <summary>
        /// アセット出力パスを取得する
        /// </summary>
        public string AssetOutputPath => _assetOutputPath;

        /// <summary>
        /// クライアント起動時に自動ロードするかを取得する
        /// </summary>
        public bool AutoLoadOnStartup => _autoLoadOnStartup;
        
        /// <summary>
        /// 登録されているコンテナリストを取得する
        /// </summary>
        public IReadOnlyList<ScriptableObject> Containers => _containers;
        
        /// <summary>
        /// サーバー同期が有効かを取得する
        /// </summary>
        public bool EnableServerSync => _enableServerSync;
        
        /// <summary>
        /// サーバーURLを取得する
        /// </summary>
        public string ServerUrl => _serverUrl;
        
        /// <summary>
        /// コンテナリストを設定する（Editor用）
        /// </summary>
        public void SetContainers(List<ScriptableObject> containers)
        {
            _containers = containers;
        }

        private static MasterDataSettings _instance;

        public static MasterDataSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<MasterDataSettings>("MasterDataSettings");
                    if (_instance == null)
                    {
#if UNITY_EDITOR
                        UnityEngine.Debug.LogWarning("[TFramework] MasterDataSettings not found in Resources. Using default values.");
#endif
                        _instance = CreateInstance<MasterDataSettings>();
                    }
                }
                return _instance;
            }
        }
    }
}
