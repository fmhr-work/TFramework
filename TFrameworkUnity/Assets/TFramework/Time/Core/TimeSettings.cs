using TFramework.Debug;
using UnityEngine;

namespace TFramework.Time
{
    /// <summary>
    /// 時間管理の設定
    /// </summary>
    [CreateAssetMenu(fileName = "TimeSettings", menuName = "TFramework/Time/Settings")]
    public class TimeSettings : ScriptableObject
    {
        [Header("Defaults")]
        [SerializeField]
        [Tooltip("デフォルトのタイムスケール")]
        private float _defaultTimeScale = 1.0f;
        
        [SerializeField]
        [Tooltip("最大タイムスケール制限")]
        private float _maxTimeScale = 100.0f;
        
        [SerializeField]
        [Tooltip("最小タイムスケール制限")]
        private float _minTimeScale = 0.0f;

        public float DefaultTimeScale => _defaultTimeScale;
        public float MaxTimeScale => _maxTimeScale;
        public float MinTimeScale => _minTimeScale;

        #region Singleton
        private static TimeSettings _instance;

        public static TimeSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<TimeSettings>("TimeSettings");
                    if (_instance == null)
                    {
                        TLogger.Warning("[TimeSettings] Settings asset not found in Resources folder"); 
                    }
                }
                return _instance;
            }
        }
        #endregion
    }
}
