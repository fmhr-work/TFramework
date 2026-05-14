namespace TFramework.UI
{
    /// <summary>
    /// ダイアログ表示パラメータクラス
    /// </summary>
    public sealed class UIDialogOpenParam
    {
        public UIDialogOpenParam(object payload = null, bool cacheOnClose = false)
        {
            Payload = payload;
            CacheOnClose = cacheOnClose;
        }

        /// <summary>
        /// ダイアログ本体に渡す表示パラメータ
        /// </summary>
        public object Payload { get; }

        /// <summary>
        /// Close後のインスタンス保持フラグ
        /// </summary>
        public bool CacheOnClose { get; }
    }
}
