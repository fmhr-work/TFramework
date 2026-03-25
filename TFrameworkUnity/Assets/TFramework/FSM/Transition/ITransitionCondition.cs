namespace TFramework.FSM
{
    /// <summary>
    /// ステート遷移の条件を定義するインターフェース
    /// </summary>
    public interface ITransitionCondition
    {
        /// <summary>
        /// 条件が満たされているかどうかを判定
        /// </summary>
        /// <returns>条件が満たされていれば true、そうでなければ false</returns>
        bool Evaluate();
    }
}
