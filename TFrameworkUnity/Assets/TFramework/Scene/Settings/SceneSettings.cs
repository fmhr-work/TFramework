using UnityEngine;

namespace TFramework.Scene
{
    /// <summary>
    /// シーン管理の設定
    /// </summary>
    [CreateAssetMenu(fileName = "SceneSettings", menuName = "TFramework/Scene/Settings")]
    public class SceneSettings : ScriptableObject
    {
        [Header("Loading")]
        [Tooltip("デフォルトのローディング画面表示遅延（秒）")]
        [SerializeField]
        [Range(0f, 2f)]
        private float _minLoadingDisplayTime = 0.5f;

        [Tooltip("シーンフェードアウト時間（秒）")]
        [SerializeField]
        [Range(0f, 2f)]
        private float _fadeOutDuration = 0.5f;

        [Tooltip("シーンフェードイン時間（秒）")]
        [SerializeField]
        [Range(0f, 2f)]
        private float _fadeInDuration = 0.5f;

        public float MinLoadingDisplayTime => _minLoadingDisplayTime;
        public float FadeOutDuration => _fadeOutDuration;
        public float FadeInDuration => _fadeInDuration;

        #region Singleton Pattern
        private static SceneSettings _instance;
        public static SceneSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<SceneSettings>("SceneSettings");
                    if (_instance == null)
                        _instance = CreateInstance<SceneSettings>();
                }
                return _instance;
            }
        }
        #endregion
    }
}
