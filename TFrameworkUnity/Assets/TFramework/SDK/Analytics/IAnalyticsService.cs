using System.Collections.Generic;

namespace TFramework.SDK
{
    /// <summary>
    /// 分析サービス（アナリティクス）のインターフェース
    /// Firebase, AppsFlyer等のイベントトラッキングシステムのラッパーとして機能する
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// サービスの初期化が完了しているか
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// カスタムイベントを送信
        /// </summary>
        /// <param name="eventName">イベント名</param>
        /// <param name="parameters">付随するパラメータのディクショナリ（オプション）</param>
        void TrackEvent(string eventName, Dictionary<string, object> parameters = null);

        /// <summary>
        /// ユーザープロパティ（属性）を設定
        /// </summary>
        /// <param name="name">プロパティ名</param>
        /// <param name="value">値</param>
        void SetUserProperty(string name, string value);

        /// <summary>
        /// ユーザーIDを設定
        /// </summary>
        /// <param name="userId">一意のユーザーID</param>
        void SetUserId(string userId);

        /// <summary>
        /// レベル（ステージ）開始イベントを送信
        /// </summary>
        /// <param name="level">レベル番号まはたID</param>
        void TrackLevelStart(string level);

        /// <summary>
        /// レベル（ステージ）クリアイベントを送信
        /// </summary>
        /// <param name="level">レベル番号まはたID</param>
        /// <param name="score">スコア（オプション）</param>
        void TrackLevelComplete(string level, int score = 0);

        /// <summary>
        /// レベル（ステージ）失敗イベントを送信
        /// </summary>
        /// <param name="level">レベル番号まはたID</param>
        void TrackLevelFail(string level);

        /// <summary>
        /// 購入イベントを送信
        /// </summary>
        /// <param name="productId">商品ID</param>
        /// <param name="price">価格</param>
        /// <param name="currency">通貨コード（例：USD, JPY）</param>
        void TrackPurchase(string productId, decimal price, string currency);
    }
}
