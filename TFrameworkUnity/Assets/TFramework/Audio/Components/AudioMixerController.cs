using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace TFramework.Audio
{
    /// <summary>
    /// AudioMixerの操作（音量設定・Ducking）を担当
    /// </summary>
    public class AudioMixerController
    {
        private readonly AudioModuleSettings _settings;
        private readonly AudioMixer _mixer;

        // ダッキングキャンセル用トークン
        private CancellationTokenSource _duckingCts;

        // ダッキング前のBGM音量（復元用）
        private float _preDuckBgmVolume = 1.0f;

        // ダッキング中フラグ
        private bool _isDucking;

        public AudioMixerController(AudioModuleSettings settings)
        {
            _settings = settings;
            _mixer = settings.Mixer;
        }

        /// <summary>
        /// ボリュームを設定
        /// </summary>
        /// <param name="layer">オーディオタイプ</param>
        /// <param name="volume">音量 (0.0 - 1.0)</param>
        public void SetVolume(AudioLayer layer, float volume)
        {
            if (_mixer == null)
            {
                return;
            }

            string paramName = GetVolumeParamName(layer);
            if (string.IsNullOrEmpty(paramName))
            {
                return;
            }

            // 0-1の線形値をデシベル(-80~0)に変換
            float db = VolumeToDecibel(volume);
            _mixer.SetFloat(paramName, db);
        }

        /// <summary>
        /// ボリュームを取得
        /// </summary>
        /// <param name="layer">オーディオタイプ</param>
        /// <returns>音量 (0.0 - 1.0)</returns>
        public float GetVolume(AudioLayer layer)
        {
            if (_mixer == null)
            {
                return 0f;
            }

            string paramName = GetVolumeParamName(layer);
            if (string.IsNullOrEmpty(paramName))
            {
                return 0f;
            }

            if (_mixer.GetFloat(paramName, out float db))
            {
                return DecibelToVolume(db);
            }

            return 0f;
        }

        /// <summary>
        /// BGMダッキング開始。BGM音量の一時的低減
        /// </summary>
        /// <param name="duckVolume">ダッキング後の音量 (0.0 - 1.0)</param>
        /// <param name="transitionDuration">遷移時間（秒）</param>
        public void StartDucking(float duckVolume, float transitionDuration)
        {
            // 既存ダッキングトランジションをキャンセル
            _duckingCts?.Cancel();
            _duckingCts?.Dispose();
            _duckingCts = new CancellationTokenSource();

            // ダッキング前の音量を保存（未ダッキング時のみ）
            if (!_isDucking)
            {
                _preDuckBgmVolume = GetVolume(AudioLayer.BGM);
            }

            _isDucking = true;
            TransitionVolumeAsync(AudioLayer.BGM, duckVolume, transitionDuration, _duckingCts.Token).Forget();
        }

        /// <summary>
        /// BGMダッキング解除。元の音量へ復元
        /// </summary>
        /// <param name="transitionDuration">遷移時間（秒）</param>
        public void StopDucking(float transitionDuration)
        {
            // 既存ダッキングトランジションをキャンセル
            _duckingCts?.Cancel();
            _duckingCts?.Dispose();
            _duckingCts = new CancellationTokenSource();

            _isDucking = false;
            TransitionVolumeAsync(AudioLayer.BGM, _preDuckBgmVolume, transitionDuration, _duckingCts.Token).Forget();
        }

        /// <summary>
        /// 音量を滑らかに遷移させる非同期処理
        /// </summary>
        private async UniTaskVoid TransitionVolumeAsync(AudioLayer layer, float targetVolume, float duration, CancellationToken ct)
        {
            if (_mixer == null) return;

            float startVolume = GetVolume(layer);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float current = Mathf.Lerp(startVolume, targetVolume, t);
                SetVolume(layer, current);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            // 最終値の確定適用
            SetVolume(layer, targetVolume);
        }

        /// <summary>
        /// オーディオタイプに対応するMixerパラメータ名を取得
        /// </summary>
        private string GetVolumeParamName(AudioLayer layer)
        {
            return layer switch
            {
                AudioLayer.Master => _settings.MasterVolumeParam,
                AudioLayer.BGM => _settings.BgmVolumeParam,
                AudioLayer.SE => _settings.SeVolumeParam,
                AudioLayer.Voice => _settings.VoiceVolumeParam,
                _ => null
            };
        }

        /// <summary>
        /// 線形音量(0-1)をデシベル(-80~0)に変換
        /// </summary>
        private float VolumeToDecibel(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (volume <= 0.0001f)
            {
                return -80f;
            }
            return Mathf.Log10(volume) * 20f;
        }

        /// <summary>
        /// デシベル(-80~0)を線形音量(0-1)に変換
        /// </summary>
        private float DecibelToVolume(float db)
        {
            if (db <= -80f)
            {
                return 0f;
            }
            return Mathf.Pow(10f, db / 20f);
        }
    }
}
