using UnityEngine;
using UnityEngine.Audio;

namespace TFramework.Audio
{
    /// <summary>
    /// Audio Module Settings
    /// オーディオ関連設定管理
    /// </summary>
    [CreateAssetMenu(fileName = "AudioModuleSettings", menuName = "TFramework/Settings/Audio Module Settings")]
    public class AudioModuleSettings : ScriptableObject
    {
        [Header("Audio Mixer")]
        [Tooltip("メインAudioMixer")]
        [SerializeField] private AudioMixer _audioMixer;

        [Header("Group Names")]
        [SerializeField] private string _masterGroupName = "Master";
        [SerializeField] private string _bgmGroupName = "BGM";
        [SerializeField] private string _seGroupName = "SE";
        [SerializeField] private string _voiceGroupName = "Voice";

        [Header("Parameter Names")]
        [SerializeField] private string _masterVolumeParam = "MasterVolume";
        [SerializeField] private string _bgmVolumeParam = "BGMVolume";
        [SerializeField] private string _seVolumeParam = "SEVolume";
        [SerializeField] private string _voiceVolumeParam = "VoiceVolume";

        [Header("Default Volumes")]
        [Range(0f, 1f)] [SerializeField] private float _defaultMasterVolume = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float _defaultBgmVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float _defaultSeVolume = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float _defaultVoiceVolume = 1.0f;

        [Header("Pool")]
        [Tooltip("SE用AudioSourceの初期生成数")]
        [SerializeField] private int _initialSeSourceCount = 10;

        public AudioMixer Mixer => _audioMixer;
        public string MasterGroupName => _masterGroupName;
        public string BgmGroupName => _bgmGroupName;
        public string SeGroupName => _seGroupName;
        public string VoiceGroupName => _voiceGroupName;

        public string MasterVolumeParam => _masterVolumeParam;
        public string BgmVolumeParam => _bgmVolumeParam;
        public string SeVolumeParam => _seVolumeParam;
        public string VoiceVolumeParam => _voiceVolumeParam;

        public float DefaultMasterVolume => _defaultMasterVolume;
        public float DefaultBgmVolume => _defaultBgmVolume;
        public float DefaultSeVolume => _defaultSeVolume;
        public float DefaultVoiceVolume => _defaultVoiceVolume;

        public int InitialSeSourceCount => _initialSeSourceCount;

        private static AudioModuleSettings _instance;
        public static AudioModuleSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<AudioModuleSettings>("AudioModuleSettings");
                    if (_instance == null)
                    {
                        _instance = CreateInstance<AudioModuleSettings>();
                    }
                }
                return _instance;
            }
        }
    }
}
