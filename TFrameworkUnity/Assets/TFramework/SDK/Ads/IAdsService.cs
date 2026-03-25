using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace TFramework.SDK
{
    /// <summary>
    /// リワード広告のイベント種別
    /// </summary>
    public enum RewardedAdEvent
    {
        Loaded,
        FailedToLoad,
        Opened,
        Closed,
        Rewarded,
        FailedToShow
    }

    /// <summary>
    /// バナー広告の表示位置
    /// </summary>
    public enum BannerPosition
    {
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center
    }

    /// <summary>
    /// 広告サービスのインターフェース
    /// 各種広告SDK（AdMob, UnityAds等）のラッパーとして機能する
    /// </summary>
    public interface IAdsService
    {
        /// <summary>
        /// サービスの初期化が完了しているかどうか
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// リワード広告（動画等）の準備ができているか
        /// </summary>
        bool IsRewardedAdReady { get; }

        /// <summary>
        /// リワード広告のイベント通知（R3 Observable）
        /// </summary>
        Observable<RewardedAdEvent> OnRewardedAdEvent { get; }

        /// <summary>
        /// インタースティシャル広告の準備ができているか
        /// </summary>
        bool IsInterstitialReady { get; }

        /// <summary>
        /// 広告サービスの初期化
        /// </summary>
        /// <param name="ct">キャンセレーショントークン</param>
        UniTask InitializeAsync(CancellationToken ct = default);

        /// <summary>
        /// リワード広告を再生
        /// </summary>
        /// <param name="placement">広告のプレースメントID（オプション）</param>
        /// <param name="ct">キャンセレーショントークン</param>
        /// <returns>報酬付与された場合はtrue、キャンセル等の場合はfalse</returns>
        UniTask<bool> ShowRewardedAdAsync(string placement = "", CancellationToken ct = default);

        /// <summary>
        /// インタースティシャル広告を再生
        /// </summary>
        /// <param name="placement">広告のプレースメントID（オプション）</param>
        /// <param name="ct">キャンセレーショントークン</param>
        UniTask ShowInterstitialAsync(string placement = "", CancellationToken ct = default);

        /// <summary>
        /// バナー広告を表示
        /// </summary>
        /// <param name="position">表示位置</param>
        /// <param name="placement">広告のプレースメントID（オプション）</param>
        void ShowBanner(BannerPosition position, string placement = "");

        /// <summary>
        /// バナー広告を非表示に
        /// </summary>
        void HideBanner();

        /// <summary>
        /// バナー広告を破棄
        /// </summary>
        void DestroyBanner();

        /// <summary>
        /// リワード広告を事前読み込み
        /// </summary>
        void LoadRewardedAd();

        /// <summary>
        /// インタースティシャル広告を事前読み込み
        /// </summary>
        void LoadInterstitial();
    }
}
