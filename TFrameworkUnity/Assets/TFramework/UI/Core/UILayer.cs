namespace TFramework.UI
{
    /// <summary>
    /// UI層の定義
    /// 各層は特定のソート順範囲を持ち、上位層が常に前面に表示される
    /// </summary>
    public enum UILayer
    {
        /// <summary>
        /// 背景層 (Sort Order: 0-99)
        /// </summary>
        Background = 0,

        /// <summary>
        /// ページ層 (Sort Order: 100-199)
        /// </summary>
        Page = 100,

        /// <summary>
        /// ポップアップ層 (Sort Order: 200-299)
        /// </summary>
        Popup = 200,

        /// <summary>
        /// ダイアログ層 (Sort Order: 300-399)
        /// </summary>
        Dialog = 300,

        /// <summary>
        /// トースト層 (Sort Order: 400-499)
        /// </summary>
        Toast = 400,

        /// <summary>
        /// ローディング層 (Sort Order: 500-599)
        /// </summary>
        Loading = 500,

        /// <summary>
        /// システム層 (Sort Order: 600-699)
        /// </summary>
        System = 600
    }
}
