using System.Text;
using UnityEngine;

namespace TFramework.SaveData
{
    /// <summary>
    /// JSON SaveData Serializer
    /// JsonUtilityを使用してシリアライズを行う
    /// </summary>
    public class JsonSaveDataSerializer : ISaveDataSerializer
    {
        /// <summary>
        /// オブジェクトをシリアライズする
        /// </summary>
        public byte[] Serialize<T>(T data)
        {
            string json = JsonUtility.ToJson(data);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// データをデシリアライズする
        /// </summary>
        public T Deserialize<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return default;
            string json = Encoding.UTF8.GetString(bytes);
            return JsonUtility.FromJson<T>(json);
        }
    }
}
