namespace TFramework.Network
{
    /// <summary>
    /// ネットワークデータのシリアライズ処理を定義するインターフェース
    /// </summary>
    public interface INetworkSerializer
    {
        /// <summary>
        /// オブジェクトのシリアライズ
        /// </summary>
        string Serialize<T>(T data);

        /// <summary>
        /// 文字列のデシリアライズ
        /// </summary>
        T Deserialize<T>(string data);
    }
}
