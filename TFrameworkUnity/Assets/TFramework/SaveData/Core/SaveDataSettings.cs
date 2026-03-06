using UnityEngine;

namespace TFramework.SaveData
{
    /// <summary>
    /// SaveData Settings
    /// </summary>
    [CreateAssetMenu(fileName = "SaveDataSettings", menuName = "TFramework/Settings/SaveData Settings")]
    public class SaveDataSettings : ScriptableObject
    {
        public enum StorageType
        {
            File,
            PlayerPrefs // WebGL Support
        }

        public enum EncryptionType
        {
            None,
            AES
        }

        [Header("Storage")]
        [Tooltip("データの保存方法")]
        [SerializeField] private StorageType _storageType = StorageType.File;
        
        [Tooltip("保存ファイル名（拡張子含む）")]
        [SerializeField] private string _fileName = "savedata.dat";

        [Header("Encryption")]
        [Tooltip("暗号化方式")]
        [SerializeField] private EncryptionType _encryptionType = EncryptionType.AES;
        
        [Tooltip("暗号化キー（16文字 or 32文字）")]
        [SerializeField] private string _encryptionKey = "1234567890123456"; // Default key (should be changed)

        [Tooltip("初期化ベクトル（16文字）")]
        [SerializeField] private string _initializationVector = "1234567890123456"; // Default IV

        [Header("Debug")]
        [Tooltip("Editor上で保存データを可読なJSONとして保存するか（暗号化無視）")]
        [SerializeField] private bool _useJsonInEditor = true;

        /// <summary>
        /// 保存方法を取得する
        /// </summary>
        public StorageType Type => _storageType;

        /// <summary>
        /// ファイル名を取得する
        /// </summary>
        public string FileName => _fileName;

        /// <summary>
        /// 暗号化方式を取得する
        /// </summary>
        public EncryptionType Encryption => _encryptionType;

        /// <summary>
        /// 暗号化キーを取得する
        /// </summary>
        public string EncryptionKey => _encryptionKey;

        /// <summary>
        /// 初期化ベクトルを取得する
        /// </summary>
        public string InitializationVector => _initializationVector;

        /// <summary>
        /// Editor上でJSONを使用するか
        /// </summary>
        public bool UseJsonInEditor => _useJsonInEditor;

        private static SaveDataSettings _instance;

        public static SaveDataSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<SaveDataSettings>("SaveDataSettings");
                    if (_instance == null)
                    {
#if UNITY_EDITOR
                        // Debug.LogWarning("[TFramework] SaveDataSettings not found. Using default.");
#endif
                        _instance = CreateInstance<SaveDataSettings>();
                    }
                }
                return _instance;
            }
        }
    }
}
