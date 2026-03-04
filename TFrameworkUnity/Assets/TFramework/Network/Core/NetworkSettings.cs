using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFramework.Network
{
    /// <summary>
    /// ネットワーク設定を管理するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkSettings", menuName = "TFramework/Network/NetworkSettings")]
    public class NetworkSettings : ScriptableObject
    {
        private static NetworkSettings _instance;

        /// <summary>
        /// シングルトンインスタンスの取得
        /// </summary>
        public static NetworkSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<NetworkSettings>("NetworkSettings");
#if UNITY_EDITOR
                    if (_instance == null)
                    {
                        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:NetworkSettings");
                        if (guids.Length > 0)
                        {
                            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                            _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<NetworkSettings>(path);
                        }
                    }
#endif
                }
                return _instance;
            }
        }

        [Header("Environment")]
        [SerializeField] private string _currentEnvironment = "development";
        
        [Serializable]
        public class EnvironmentConfig
        {
            public string Name;
            public string BaseUrl;
            public string SchemaUrl;
        }

        [SerializeField] private List<EnvironmentConfig> _environments = new List<EnvironmentConfig>()
        {
            new EnvironmentConfig { Name = "development", BaseUrl = "https://dev-api.example.com", SchemaUrl = "https://dev-api.example.com/docs/api_schema.json" }
        };

        [Header("Code Generator")]
        [Tooltip("自動コード生成の出力先フォルダ（Assetsからの相対パス）")]
        [SerializeField] private string _apiOutputPath = "Scripts/Generated/Network/API";

        [Header("Timeouts")]
        [SerializeField] private int _connectTimeout = 10;
        [SerializeField] private int _requestTimeout = 30;

        /// <summary>
        /// 現在のネットワーク環境の名前の取得・設定
        /// </summary>
        public string CurrentEnvironment
        {
            get => _currentEnvironment;
            set => _currentEnvironment = value;
        }

        /// <summary>
        /// APIコード出力パスを取得する
        /// </summary>
        public string ApiOutputPath => _apiOutputPath;

        /// <summary>
        /// 設定されている環境の一覧を取得する
        /// </summary>
        public IReadOnlyList<EnvironmentConfig> Environments => _environments;

        /// <summary>
        /// 現在の環境に基づいたBaseURLの取得
        /// </summary>
        public string BaseUrl
        {
            get
            {
                var config = _environments.FirstOrDefault(e => e.Name == _currentEnvironment);
                return config != null ? config.BaseUrl : string.Empty;
            }
        }

        /// <summary>
        /// 現在の環境に基づいたSchemaURLの取得
        /// </summary>
        public string SchemaUrl
        {
            get
            {
                var config = _environments.FirstOrDefault(e => e.Name == _currentEnvironment);
                return config != null ? config.SchemaUrl : string.Empty;
            }
        }

        /// <summary>
        /// 接続タイムアウト（秒）の取得
        /// </summary>
        public int ConnectTimeout => _connectTimeout;

        /// <summary>
        /// リクエストタイムアウト（秒）の取得
        /// </summary>
        public int RequestTimeout => _requestTimeout;
    }
}
