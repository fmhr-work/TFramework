namespace TFramework.MasterData
{
    /// <summary>
    /// 全てのMasterDataの基底インターフェース
    /// 自動生成されるMasterDataクラスが継承する
    /// </summary>
    public interface IMasterDataObject
    {
        // ジェネリック制約のためのマーカーインターフェース
    }

    /// <summary>
    /// 主キーを持つMasterDataのジェネリックインターフェース
    /// </summary>
    /// <typeparam name="TKey">主キーの型</typeparam>
    public interface IMasterDataObject<out TKey> : IMasterDataObject
    {
        /// <summary>
        /// 主キーを取得する
        /// </summary>
        /// <returns>主キーの値</returns>
        TKey GetKey();
    }
}
