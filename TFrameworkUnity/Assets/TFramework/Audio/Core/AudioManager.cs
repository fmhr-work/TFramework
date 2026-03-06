using System;
using System.Collections.Generic;
using System.Threading;
using TFramework.Core;
using TFramework.Debug;
using TFramework.SaveData;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;

namespace TFramework.Audio
{
    /// <summary>
    /// オーディオ機能の統括管理クラス
    /// </summary>
    public class AudioManager : IAudioService, IInitializable, IDisposable
    {
        private BGMPlayer _bgmPlayer;
        private SEPlayer _sePlayer;
        private AudioMixerController _mixerController;
        private GameObject _rootObject;
        private readonly ISaveDataService _saveDataService;

        // リソースハンドルキャッシュ（リリース用）
        private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> _loadedClips
            = new Dictionary<string, AsyncOperationHandle<AudioClip>>();

        // 音量保存キープレフィックス
        private const string VolumeKeyPrefix = "Audio.Volume.";

        [Inject]
        public AudioManager(ISaveDataService saveDataService = null)
        {
            _saveDataService = saveDataService;
        }

        public UniTask InitializeAsync(CancellationToken token)
        {
            return InitializeAsync(token, null);
        }

        public async UniTask InitializeAsync(CancellationToken token, AudioModuleSettings settings)
        {
            _rootObject = new GameObject("AudioManager");
            GameObject.DontDestroyOnLoad(_rootObject);

            if (settings == null)
            {
                settings = AudioModuleSettings.Instance;
            }

            if (settings == null)
            {
                TLogger.Error("AudioModuleSettings is null!");
                return;
            }

            _mixerController = new AudioMixerController(settings);
            _bgmPlayer = new BGMPlayer(_rootObject, _mixerController);
            _sePlayer = new SEPlayer(_rootObject, _mixerController);

            // SaveDataから音量を復元、なければデフォルト値を適用
            await RestoreOrApplyDefaultVolumesAsync(settings, token);

            TLogger.Info("AudioManager Initialized");
        }

        public async UniTask PlayBGMAsync(string key, float fadeDuration = 1.0f)
        {
            // 既に同じ曲ならロードしない
            if (_bgmPlayer.CurrentBgmKey == key)
            {
                return;
            }

            var clip = await LoadClipAsync(key);
            if (clip != null)
            {
                await _bgmPlayer.PlayAsync(clip, key, fadeDuration);
            }
        }

        public void StopBGM(float fadeDuration = 1.0f)
        {
            _bgmPlayer.StopAsync(fadeDuration).Forget();
        }

        public void PauseBGM()
        {
            _bgmPlayer.Pause();
        }

        public void ResumeBGM()
        {
            _bgmPlayer.Resume();
        }

        public void PlaySE(string key)
        {
            PlaySE3D(key, default);
        }

        public void PlaySE3D(string key, Vector3 position)
        {
            // SEはロード即再生
            LoadAndPlaySE(key, position).Forget();
        }

        private async UniTaskVoid LoadAndPlaySE(string key, Vector3 position)
        {
            var clip = await LoadClipAsync(key);
            if (clip != null)
            {
                _sePlayer.Play(clip, position);
            }
        }

        public void StopSE()
        {
            _sePlayer.StopAll();
        }

        public void SetVolume(AudioLayer layer, float volume)
        {
            // 初期化前はスキップ
            if (_mixerController == null)
            {
                return;
            }

            _mixerController.SetVolume(layer, volume);

            if (layer == AudioLayer.BGM)
            {
                _bgmPlayer?.SetVolume(volume);
            }
            if (layer == AudioLayer.SE)
            {
                _sePlayer?.SetVolume(volume);
            }

            // SaveDataへの音量永続化
            if (_saveDataService != null)
            {
                var key = VolumeKeyPrefix + layer.ToString();
                _saveDataService.SaveAsync(key, volume).Forget();
            }
        }

        public float GetVolume(AudioLayer layer)
        {
            if (_mixerController == null)
            {
                return 0f;
            }
            return _mixerController.GetVolume(layer);
        }

        public void StartBGMDucking(float duckVolume = 0.3f, float transitionDuration = 0.5f)
        {
            if (_mixerController == null)
            {
                return;
            }
            _mixerController.StartDucking(duckVolume, transitionDuration);
        }

        public void StopBGMDucking(float transitionDuration = 0.5f)
        {
            if (_mixerController == null)
            {
                return;
            }
            _mixerController.StopDucking(transitionDuration);
        }

        public void Dispose()
        {
            if (_rootObject != null)
            {
                GameObject.Destroy(_rootObject);
            }

            // ロードしたリソースの解放
            foreach (var handle in _loadedClips.Values)
            {
                Addressables.Release(handle);
            }
            _loadedClips.Clear();
        }

        /// <summary>
        /// SaveDataから音量を復元。保存データが存在しない場合デフォルト値を適用
        /// </summary>
        private async UniTask RestoreOrApplyDefaultVolumesAsync(AudioModuleSettings settings, CancellationToken token)
        {
            var layers = new[]
            {
                (AudioLayer.Master, settings.DefaultMasterVolume),
                (AudioLayer.BGM,    settings.DefaultBgmVolume),
                (AudioLayer.SE,     settings.DefaultSeVolume),
                (AudioLayer.Voice,  settings.DefaultVoiceVolume),
            };

            foreach (var (layer, defaultVolume) in layers)
            {
                float volume = defaultVolume;

                if (_saveDataService != null)
                {
                    var key = VolumeKeyPrefix + layer;
                    volume = await _saveDataService.LoadAsync(key, defaultVolume, token);
                }

                // ミキサーへの音量適用（SetVolumeを経由せず直接 — 保存は行わない）
                _mixerController.SetVolume(layer, volume);

                if (layer == AudioLayer.BGM)
                {
                    _bgmPlayer.SetVolume(volume);
                }
                else if (layer == AudioLayer.SE)
                {
                    _sePlayer.SetVolume(volume);
                }
            }
        }

        /// <summary>
        /// AudioClipをロード（キャッシュ付き）
        /// </summary>
        private async UniTask<AudioClip> LoadClipAsync(string key)
        {
            if (_loadedClips.TryGetValue(key, out var handle))
            {
                if (handle.IsValid())
                {
                    return handle.Result;
                }
            }

            try
            {
                var op = Addressables.LoadAssetAsync<AudioClip>(key);
                var clip = await op.Task.AsUniTask();

                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    _loadedClips[key] = op;
                    return clip;
                }
                TLogger.Error($"AudioClip Load Failed: {key}");
                return null;
            }
            catch (Exception e)
            {
                TLogger.Error($"AudioClip Load Exception: {e.Message}");
                return null;
            }
        }
    }
}
