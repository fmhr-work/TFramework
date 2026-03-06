using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TFramework.Debug;

namespace TFramework.SaveData
{
    /// <summary>
    /// PlayerPrefsを使用してデータを保存するクラス
    /// </summary>
    public class PlayerPrefsStorageProvider : IStorageProvider
    {
        // 管理用キーのプレフィックス
        private const string KeyListPrefix = "_TF_KeyList_";

        [Serializable]
        private class StringArray
        {
            public string[] Items;
        }

        /// <summary>
        /// データを保存
        /// byte[]をBase64に変換してPlayerPrefsに格納
        /// </summary>
        public UniTask SaveAsync(string path, byte[] data)
        {
            try
            {
                string base64 = Convert.ToBase64String(data);
                PlayerPrefs.SetString(path, base64);
                
                // キーをリストに追加
                AddKeyToList(path);
                
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                TLogger.Error($"PlayerPrefs Save Error: {e.Message}");
                throw;
            }

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// データを読み込む
        /// Base64文字列をbyte[]に復元
        /// </summary>
        public UniTask<byte[]> LoadAsync(string path)
        {
            if (!PlayerPrefs.HasKey(path))
            {
                return UniTask.FromResult<byte[]>(null);
            }

            try
            {
                string base64 = PlayerPrefs.GetString(path);
                byte[] data = Convert.FromBase64String(base64);
                return UniTask.FromResult(data);
            }
            catch (Exception e)
            {
                TLogger.Error($"PlayerPrefs Load Error: {e.Message}");
                return UniTask.FromResult<byte[]>(null);
            }
        }

        /// <summary>
        /// データが存在するか確認
        /// </summary>
        public bool Exists(string path)
        {
            return PlayerPrefs.HasKey(path);
        }

        /// <summary>
        /// データを削除
        /// </summary>
        public void Delete(string path)
        {
            if (PlayerPrefs.HasKey(path))
            {
                PlayerPrefs.DeleteKey(path);
                RemoveKeyFromList(path);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// 指定プレフィックスの全データを削除
        /// </summary>
        public void DeleteAll(string prefix)
        {
            // パス区切りの統一
            prefix = prefix?.Replace("\\", "/");

            string keyListKey = KeyListPrefix + prefix;
            if (PlayerPrefs.HasKey(keyListKey))
            {
                string keyListJson = PlayerPrefs.GetString(keyListKey);
                string[] keys = JsonUtility.FromJson<StringArray>(keyListJson)?.Items;
                if (keys != null)
                {
                    foreach (var key in keys)
                    {
                        PlayerPrefs.DeleteKey(key);
                    }
                }
                PlayerPrefs.DeleteKey(keyListKey);
                PlayerPrefs.Save();
            }
        }

        private void AddKeyToList(string key)
        {
            string dir = Path.GetDirectoryName(key);
            // Windowsパス区切りを統一
            dir = dir?.Replace("\\", "/"); 
            string keyListKey = KeyListPrefix + dir;

            List<string> keys = new();
            if (PlayerPrefs.HasKey(keyListKey))
            {
                string json = PlayerPrefs.GetString(keyListKey);
                var wrapper = JsonUtility.FromJson<StringArray>(json);
                if (wrapper?.Items != null)
                {
                    keys.AddRange(wrapper.Items);
                }
            }

            if (!keys.Contains(key))
            {
                keys.Add(key);
                string newJson = JsonUtility.ToJson(new StringArray { Items = keys.ToArray() });
                PlayerPrefs.SetString(keyListKey, newJson);
            }
        }

        private void RemoveKeyFromList(string key)
        {
            string dir = Path.GetDirectoryName(key);
            dir = dir?.Replace("\\", "/");
            string keyListKey = KeyListPrefix + dir;

            if (PlayerPrefs.HasKey(keyListKey))
            {
                string json = PlayerPrefs.GetString(keyListKey);
                var wrapper = JsonUtility.FromJson<StringArray>(json);
                if (wrapper?.Items != null)
                {
                    var list = new List<string>(wrapper.Items);
                    if (list.Remove(key))
                    {
                        string newJson = JsonUtility.ToJson(new StringArray { Items = list.ToArray() });
                        PlayerPrefs.SetString(keyListKey, newJson);
                    }
                }
            }
        }
    }
}
