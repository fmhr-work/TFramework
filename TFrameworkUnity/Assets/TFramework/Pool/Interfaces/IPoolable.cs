namespace TFramework.Pool
{
    /// <summary>
    /// プール可能オブジェクトのインターフェース
    /// プールからの取得・返却時にコールバックを受け取る
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// プールから取得された時に呼び出される
        /// オブジェクトの初期化処理を行う
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// プールに返却される時に呼び出される
        /// オブジェクトのリセット処理を行う
        /// </summary>
        void OnDespawn();
    }
}
