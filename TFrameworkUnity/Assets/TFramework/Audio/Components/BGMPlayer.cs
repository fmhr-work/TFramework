using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TFramework.Audio
{
    /// <summary>
    /// BGMの再生、フェード、停止を担当
    /// CrossFadeに対応するため2つのAudioSourceを使用
    /// </summary>
    public class BGMPlayer
    {
        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private bool _isUsingSourceA = true;
        private string _currentBgmKey;
        private float _volume = 1.0f;
        
        private AudioMixerController _mixerController;

        public string CurrentBgmKey => _currentBgmKey;

        public BGMPlayer(GameObject root, AudioMixerController mixerController)
        {
            _mixerController = mixerController;

            // AudioSource生成
            _sourceA = CreateAudioSource(root, "BGM_Source_A");
            _sourceB = CreateAudioSource(root, "BGM_Source_B");
        }

        private AudioSource CreateAudioSource(GameObject root, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;

            if (AudioModuleSettings.Instance.Mixer != null)
            {
                var groups = AudioModuleSettings.Instance.Mixer.FindMatchingGroups("BGM");
                if (groups != null && groups.Length > 0)
                {
                    source.outputAudioMixerGroup = groups[0];
                }
            }
            return source;
        }

        /// <summary>
        /// BGMを再生
        /// </summary>
        public async UniTask PlayAsync(AudioClip clip, string key, float fadeDuration)
        {
            if (_currentBgmKey == key)
            {
                return; // 同じ曲なら何もしない
            }

            _currentBgmKey = key;
            
            // 現在のソースと次のソースを決定
            var currentSource = _isUsingSourceA ? _sourceA : _sourceB;
            var nextSource = _isUsingSourceA ? _sourceB : _sourceA;

            // 次のソース準備
            nextSource.clip = clip;
            nextSource.volume = 0f;
            nextSource.Play();

            // CrossFade開始
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float t = timer / fadeDuration;

                if (currentSource.isPlaying)
                {
                    currentSource.volume = (1f - t) * _volume;
                }
                nextSource.volume = t * _volume;

                await UniTask.Yield();
            }

            // 完了処理
            currentSource.Stop();
            currentSource.volume = 0f;
            nextSource.volume = _volume;

            // フラグ反転
            _isUsingSourceA = !_isUsingSourceA;
        }

        /// <summary>
        /// BGMを停止
        /// </summary>
        public async UniTask StopAsync(float fadeDuration)
        {
            _currentBgmKey = null;
            var currentSource = _isUsingSourceA ? _sourceA : _sourceB;

            if (!currentSource.isPlaying)
            {
                return;
            }

            float startVolume = currentSource.volume;
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float t = timer / fadeDuration;
                currentSource.volume = (1f - t) * startVolume;
                await UniTask.Yield();
            }

            currentSource.Stop();
            currentSource.volume = 0f;
        }

        public void Pause()
        {
            if (_sourceA.isPlaying)
            {
                _sourceA.Pause();
            }
            if (_sourceB.isPlaying)
            {
                _sourceB.Pause();
            }
        }

        public void Resume()
        {
            _sourceA.UnPause();
            _sourceB.UnPause();
        }

        public void SetVolume(float volume)
        {
            _volume = volume;
            // 現在再生中のソースに反映
            var currentSource = _isUsingSourceA ? _sourceA : _sourceB;
            if (currentSource.isPlaying)
            {
                currentSource.volume = _volume;
            }
        }
    }
}
