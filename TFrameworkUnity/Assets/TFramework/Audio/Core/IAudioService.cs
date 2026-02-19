using Cysharp.Threading.Tasks;

namespace TFramework.Audio
{
    /// <summary>
    /// オーディオ再生・停止・音量制御機能を提供
    /// </summary>
    public interface IAudioService
    {
        /// <summary>
        /// BGM再生
        /// </summary>
        /// <param name="key">BGMキー(Addressables Key等)</param>
        /// <param name="fadeDuration">フェード時間(秒)</param>
        UniTask PlayBGMAsync(string key, float fadeDuration = 1.0f);

        /// <summary>
        /// BGM停止
        /// </summary>
        /// <param name="fadeDuration">フェード時間(秒)</param>
        void StopBGM(float fadeDuration = 1.0f);

        /// <summary>
        /// BGM一時停止
        /// </summary>
        void PauseBGM();

        /// <summary>
        /// BGM再開
        /// </summary>
        void ResumeBGM();

        /// <summary>
        /// SE再生
        /// </summary>
        /// <param name="key">SEキー(Addressables Key等)</param>
        void PlaySE(string key);

        /// <summary>
        /// 3D SE再生
        /// </summary>
        /// <param name="key">SEキー</param>
        /// <param name="position">再生位置</param>
        void PlaySE3D(string key, UnityEngine.Vector3 position);

        /// <summary>
        /// SE停止(全停止)
        /// </summary>
        void StopSE();

        /// <summary>
        /// 音量設定
        /// </summary>
        /// <param name="layer">レイヤー(Master, BGM, SE)</param>
        /// <param name="volume">音量(0.0 - 1.0)</param>
        void SetVolume(AudioLayer layer, float volume);

        /// <summary>
        /// 音量取得
        /// </summary>
        /// <param name="layer">レイヤー</param>
        float GetVolume(AudioLayer layer);

        /// <summary>
        /// BGMダッキング開始。BGM音量の一時的低減
        /// </summary>
        /// <param name="duckVolume">ダッキング適用後の音量（0.0 - 1.0）</param>
        /// <param name="transitionDuration">遷移時間（秒）</param>
        void StartBGMDucking(float duckVolume = 0.3f, float transitionDuration = 0.5f);

        /// <summary>
        /// BGMダッキング解除。元の音量へ復元
        /// </summary>
        /// <param name="transitionDuration">遷移時間（秒）</param>
        void StopBGMDucking(float transitionDuration = 0.5f);
    }

    /// <summary>
    /// オーディオレイヤー
    /// </summary>
    public enum AudioLayer
    {
        Master,
        BGM,
        SE,
        Voice
    }
}
