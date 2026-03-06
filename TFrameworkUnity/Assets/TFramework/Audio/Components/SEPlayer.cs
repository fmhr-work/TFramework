
using UnityEngine;
using UnityEngine.Audio;
using Cysharp.Threading.Tasks;

namespace TFramework.Audio
{
    /// <summary>
    /// SEの再生を担当
    /// AudioSourcePoolを使用して再生を行う
    /// </summary>
    public class SEPlayer
    {
        private readonly AudioSourcePool _pool;
        private readonly AudioMixerGroup _seGroup;
        private readonly Transform _root;
        private float _volume = 1.0f;

        public SEPlayer(GameObject root, AudioMixerController mixerController)
        {
            _root = root.transform;

            // Pool用ルートオブジェクト作成
            var poolRoot = new GameObject("SE_Pool_Root");
            poolRoot.transform.SetParent(_root);
            
            // Pool初期化
            _pool = new AudioSourcePool(AudioModuleSettings.Instance.InitialSeSourceCount, poolRoot.transform);
            
            // MixerGroup取得
            if (AudioModuleSettings.Instance.Mixer != null)
            {
                var groups = AudioModuleSettings.Instance.Mixer.FindMatchingGroups("SE");
                if (groups.Length > 0)
                {
                    _seGroup = groups[0];
                }
            }
        }

        public void Play(AudioClip clip, Vector3 position = default)
        {
            if (clip == null)
            {
                return;
            }

            var source = _pool.Get();
            
            // 設定
            // TransformはPoolで管理されているため、必要な場合のみ位置を更新
            if (position != default)
            {
                source.transform.position = position;
                source.spatialBlend = 1f; // 3D
            }
            else
            {
                source.transform.localPosition = Vector3.zero;
                source.spatialBlend = 0f; // 2D
            }

            source.clip = clip;
            source.volume = _volume;
            source.outputAudioMixerGroup = _seGroup;
            
            source.Play();

            // 再生終了後にRelease
            ReleaseAfterPlay(source, clip.length).Forget();
        }

        private async UniTaskVoid ReleaseAfterPlay(AudioSource source, float duration)
        {
            // Time.timeScaleの影響を受けないようにUniTask.Delayを使用
            await UniTask.Delay((int)(duration * 1000), ignoreTimeScale: true);
            
            if (source != null)
            {
                _pool.Release(source);
            }
        }

        public void SetVolume(float volume)
        {
            _volume = volume;
        }

        public void StopAll()
        {
            _pool.StopAllActive();
        }
    }
}
