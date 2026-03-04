namespace TFramework.Network
{
    /// <summary>
    /// APIリクエストの基底クラス
    /// </summary>
    public abstract class RequestBase
    {
        /// <summary>
        /// APIのエンドポイント名（URLパス）の取得
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// HTTPメソッドの種類の取得
        /// </summary>
        public abstract ApiType Type { get; }
        
        /// <summary>
        /// リクエストパラメータの妥当性の検証
        /// </summary>
        /// <returns>妥当な場合はtrue</returns>
        public virtual bool Check() => true;
    }
}
