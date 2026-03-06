using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using TFramework.Core;
using TFramework.Debug;

namespace TFramework.SaveData
{
    /// <summary>
    /// データの保存・読み込みを管理するクラス
    /// </summary>
    public class SaveDataManager : ISaveDataService, IInitializable, IDisposable
    {
        private readonly SaveDataSettings _settings;
        private IStorageProvider _storage;
        private IEncryptionProvider _encryption;
        private ISaveDataSerializer _serializer;
        
        private int _currentSlot = 0;
        private Dictionary<string, object> _cache = new Dictionary<string, object>();

        public int CurrentSlot => _currentSlot;

        public SaveDataManager(SaveDataSettings settings)
        {
            _settings = settings;
        }

        public UniTask InitializeAsync(CancellationToken token = default)
        {
            // Initialize Providers
            switch (_settings.Type)
            {
                case SaveDataSettings.StorageType.File:
                    _storage = new FileStorageProvider();
                    break;
                case SaveDataSettings.StorageType.PlayerPrefs:
                    _storage = new PlayerPrefsStorageProvider();
                    break;
            }

            switch (_settings.Encryption)
            {
                case SaveDataSettings.EncryptionType.AES:
                    _encryption = new AESEncryptionProvider(_settings.EncryptionKey, _settings.InitializationVector);
                    break;
                case SaveDataSettings.EncryptionType.None:
                    _encryption = null;
                    break;
            }

            _serializer = new JsonSaveDataSerializer();
            
            TLogger.Info("SaveDataManager Initialized");
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 現在のスロットインデックスを変更
        /// </summary>
        public void SetSlot(int slotIndex)
        {
            _currentSlot = slotIndex;
            _cache.Clear(); // Slot変更時にキャッシュをクリア
        }

        /// <summary>
        /// データを保存
        /// </summary>
        public async UniTask SaveAsync<T>(string key, T data, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            string path = GetSaveFilePath(key);
            
            // Serialize
            byte[] bytes = _serializer.Serialize(data);

            // Encrypt
            if (_encryption != null)
            {
                #if UNITY_EDITOR
                if (!_settings.UseJsonInEditor)
                #endif
                {
                    bytes = _encryption.Encrypt(bytes);
                }
            }

            // Save
            await _storage.SaveAsync(path, bytes);
            
            // Update Cache
            _cache[key] = data;
            
            TLogger.Info($"Data Saved: {key} (Slot {_currentSlot})");
        }

        /// <summary>
        /// データを読み込む
        /// </summary>
        public async UniTask<T> LoadAsync<T>(string key, T defaultValue = default, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            // Check Cache
            if (_cache.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            string path = GetSaveFilePath(key);
            
            if (!_storage.Exists(path))
            {
                return defaultValue;
            }

            // Load
            byte[] bytes = await _storage.LoadAsync(path);
            if (bytes == null) return defaultValue;

            // Decrypt
            if (_encryption != null)
            {
                #if UNITY_EDITOR
                // Editorでの暗号化スキップ判定ロジックが必要だが、
                // 保存時に暗号化していない場合は復号化も失敗する可能性があるため
                // ヘッダーチェックなどが理想的。
                // ここでは簡易的に、設定に従う。
                if (!_settings.UseJsonInEditor)
                #endif
                {
                    bytes = _encryption.Decrypt(bytes);
                }
            }
            
            if (bytes == null) return defaultValue; // Decryption failed

            // Deserialize
            T data = _serializer.Deserialize<T>(bytes);
            
            // Cache
            _cache[key] = data;

            return data;
        }

        /// <summary>
        /// データが存在するか確認
        /// </summary>
        public bool Exists(string key)
        {
            if (_cache.ContainsKey(key))
            {
                return true;
            }
            string path = GetSaveFilePath(key);
            return _storage.Exists(path);
        }

        /// <summary>
        /// データを削除
        /// </summary>
        public void Delete(string key)
        {
            string path = GetSaveFilePath(key);
            _storage.Delete(path);
            
            if (_cache.ContainsKey(key))
            {
                _cache.Remove(key);
            }
        }

        /// <summary>
        /// 全てのデータを削除（現在のスロット）
        /// </summary>
        public void DeleteAll()
        {
            try
            {
                string dir = GetSaveDirectory();
                _storage.DeleteAll(dir);
            }
            catch (Exception e)
            {
                TLogger.Error($"DeleteAll Error: {e.Message}");
            }
            _cache.Clear();
        }

        public void Dispose()
        {
            _cache.Clear();
        }

        private string GetSaveFilePath(string key)
        {
            string fileName = $"{key}_{_settings.FileName}";
            return Path.Combine(GetSaveDirectory(), fileName);
        }

        private string GetSaveDirectory()
        {
            return Path.Combine(Application.persistentDataPath, $"Slot_{_currentSlot}");
        }
    }
}
