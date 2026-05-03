using TFramework.Debug;
using UnityEngine;

namespace TFramework.Input
{
    /// <summary>
    /// 入力モジュールの設定
    /// </summary>
    [CreateAssetMenu(fileName = "InputModuleSettings", menuName = "TFramework/Input/Settings")]
    public class InputModuleSettings : ScriptableObject
    {
        [Header("Behavior")]
        [SerializeField]
        [Tooltip("デフォルトで入力を有効化")]
        private bool _enableOnStart = true;

        [SerializeField]
        [Tooltip("プレイヤーが操作不能時に入力をロック")]
        private bool _lockOnPlayerIncapacitated = true;

        public bool EnableOnStart => _enableOnStart;
        public bool LockOnPlayerIncapacitated => _lockOnPlayerIncapacitated;

        #region Singleton
        private static InputModuleSettings _instance;

        public static InputModuleSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<InputModuleSettings>("InputModuleSettings");
                    if (_instance == null)
                    {
                        TLogger.Warning("[InputModuleSettings] Settings asset not found in Resources folder, using default.");
                        _instance = CreateInstance<InputModuleSettings>();
                    }
                }
                return _instance;
            }
        }
        #endregion
    }
}