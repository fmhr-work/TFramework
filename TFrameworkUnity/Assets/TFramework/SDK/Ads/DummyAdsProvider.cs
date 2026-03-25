using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using TFramework.Debug;

namespace TFramework.SDK.Ads
{
    /// <summary>
    /// エディタ検証用のダミー広告プロバイダ
    /// </summary>
    public class DummyAdsProvider : IAdsService
    {
        public bool IsInitialized { get; private set; }
        public bool IsRewardedAdReady { get; private set; }
        public bool IsInterstitialReady { get; private set; }

        public Observable<RewardedAdEvent> OnRewardedAdEvent => _onRewardedAdEvent;
        private readonly Subject<RewardedAdEvent> _onRewardedAdEvent = new Subject<RewardedAdEvent>();

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            TLogger.Info("[DummyAds] Initializing Dummy Ads Provider...", "SDK");
            IsInitialized = true;
            return UniTask.CompletedTask;
        }

        public async UniTask<bool> ShowRewardedAdAsync(string placement = "", CancellationToken ct = default)
        {
            if (!IsRewardedAdReady)
            {
                TLogger.Warning("[DummyAds] Ad is not ready.", "SDK");
                _onRewardedAdEvent.OnNext(RewardedAdEvent.FailedToShow);
                return false;
            }

            TLogger.Info($"[DummyAds] Showing Rewarded Ad: {placement}", "SDK");
            _onRewardedAdEvent.OnNext(RewardedAdEvent.Opened);

            // ダミーの視聴待機時間
            await UniTask.Delay(1000, cancellationToken: ct);

            TLogger.Info("[DummyAds] Rewarded Ad finished watching.", "SDK");
            _onRewardedAdEvent.OnNext(RewardedAdEvent.Rewarded);
            _onRewardedAdEvent.OnNext(RewardedAdEvent.Closed);

            IsRewardedAdReady = false; // 表示後消費される
            return true;
        }

        public async UniTask ShowInterstitialAsync(string placement = "", CancellationToken ct = default)
        {
            if (!IsInterstitialReady)
            {
                TLogger.Warning("[DummyAds] Interstitial Ad is not ready.", "SDK");
                return;
            }

            TLogger.Info($"[DummyAds] Showing Interstitial Ad: {placement}", "SDK");
            
            // ダミーの視聴待機時間
            await UniTask.Delay(500, cancellationToken: ct);

            TLogger.Info("[DummyAds] Interstitial Ad closed.", "SDK");
            IsInterstitialReady = false; // 表示後消費される
        }

        public void ShowBanner(BannerPosition position, string placement = "")
        {
            TLogger.Info($"[DummyAds] Showing Banner Ad at {position}", "SDK");
        }

        public void HideBanner()
        {
            TLogger.Info("[DummyAds] Hiding Banner Ad", "SDK");
        }

        public void DestroyBanner()
        {
            TLogger.Info("[DummyAds] Destroying Banner Ad", "SDK");
        }

        public void LoadRewardedAd()
        {
            TLogger.Info("[DummyAds] Loading Rewarded Ad...", "SDK");
            IsRewardedAdReady = true;
            _onRewardedAdEvent.OnNext(RewardedAdEvent.Loaded);
        }

        public void LoadInterstitial()
        {
            TLogger.Info("[DummyAds] Loading Interstitial Ad...", "SDK");
            IsInterstitialReady = true;
        }
    }
}
