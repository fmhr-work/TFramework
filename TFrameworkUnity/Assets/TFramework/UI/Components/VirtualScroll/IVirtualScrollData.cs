namespace TFramework.UI
{
    /// <summary>
    /// 仮想スクロールデータインターフェース
    /// </summary>
    public interface IVirtualScrollData
    {
        /// <summary>
        /// データの総数
        /// </summary>
        int ItemCount { get; }
    }

    /// <summary>
    /// 仮想スクロールデータアイテムインターフェース
    /// </summary>
    public interface IVirtualScrollDataItem
    {
        /// <summary>
        /// アイテムの高さを取得する
        /// </summary>
        float GetItemHeight();
    }
}
