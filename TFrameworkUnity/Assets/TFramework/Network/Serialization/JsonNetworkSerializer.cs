using UnityEngine;
#if NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif

namespace TFramework.Network
{
    /// <summary>
    /// JSON形式のシリアライザー実装クラス
    /// </summary>
    public class JsonNetworkSerializer : INetworkSerializer
    {
        /// <summary>
        /// オブジェクトのJSON文字列への変換
        /// </summary>
        public string Serialize<T>(T data)
        {
#if NEWTONSOFT_JSON
            return JsonConvert.SerializeObject(data);
#else
            return JsonUtility.ToJson(data);
#endif
        }

        /// <summary>
        /// JSON文字列のオブジェクトへの変換
        /// </summary>
        public T Deserialize<T>(string data)
        {
#if NEWTONSOFT_JSON
            return JsonConvert.DeserializeObject<T>(data);
#else
            return JsonUtility.FromJson<T>(data);
#endif
        }
    }
}
