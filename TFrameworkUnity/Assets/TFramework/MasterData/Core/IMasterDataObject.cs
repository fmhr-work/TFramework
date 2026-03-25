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
    /// メインキーを持つMasterDataのジェネリックインターフェース
    /// </summary>
    /// <typeparam name="TKey">メインキーの型</typeparam>
    public interface IMasterDataObject<out TKey> : IMasterDataObject
    {
        /// <summary>
        /// メインキーを取得する
        /// </summary>
        /// <returns>メインキーの値</returns>
        TKey GetKey();
    }
}
