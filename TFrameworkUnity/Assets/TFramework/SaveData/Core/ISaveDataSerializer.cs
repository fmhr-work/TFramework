namespace TFramework.SaveData
{
    /// <summary>
    /// SaveData Serializer Interface
    /// オブジェクトのシリアライズ・デシリアライズを行う
    /// </summary>
    public interface ISaveDataSerializer
    {
        /// <summary>
        /// オブジェクトをシリアライズする
        /// </summary>
        byte[] Serialize<T>(T data);

        /// <summary>
        /// データをデシリアライズする
        /// </summary>
        T Deserialize<T>(byte[] bytes);
    }
}
