using System;
using System.Collections.Generic;
using TFramework.Audio;
using TFramework.Localization;
using TFramework.MasterData;
using TFramework.Network;
using TFramework.SaveData;
using TFramework.Scene;
using TFramework.Time;
using TFramework.UI;

namespace TFramework.Core.Editor
{
    /// <summary>
    /// モジュール設定カタログ
    /// </summary>
    public static class ModuleSettingsCatalog
    {
        /// <summary>
        /// モジュール設定定義
        /// </summary>
        public readonly struct ModuleSettingsDefinition
        {
            public string DisplayName { get; }
            public Type SettingsType { get; }
            public string ResourceName { get; }
            public string DefaultAssetPath { get; }

            public ModuleSettingsDefinition(string displayName, Type settingsType, string resourceName, string defaultAssetPath)
            {
                DisplayName = displayName;
                SettingsType = settingsType;
                ResourceName = resourceName;
                DefaultAssetPath = defaultAssetPath;
            }
        }

        /// <summary>
        /// モジュール定義一覧
        /// </summary>
        public static IReadOnlyList<ModuleSettingsDefinition> Definitions { get; } = new List<ModuleSettingsDefinition>
        {
            new("Core / TFramework", typeof(TFrameworkSettings), "TFrameworkSettings", "Assets/Resources/TFrameworkSettings.asset"),
            new("UI", typeof(UISettings), "UISettings", "Assets/Resources/UISettings.asset"),
            new("Audio", typeof(AudioModuleSettings), "AudioModuleSettings", "Assets/Resources/AudioModuleSettings.asset"),
            new("Network", typeof(NetworkSettings), "NetworkSettings", "Assets/Resources/NetworkSettings.asset"),
            new("SaveData", typeof(SaveDataSettings), "SaveDataSettings", "Assets/Resources/SaveDataSettings.asset"),
            new("Localization", typeof(LocalizationSettings), "LocalizationSettings", "Assets/Resources/LocalizationSettings.asset"),
            new("Time", typeof(TimeSettings), "TimeSettings", "Assets/Resources/TimeSettings.asset"),
            new("Scene", typeof(SceneSettings), "SceneSettings", "Assets/Resources/SceneSettings.asset"),
            new("MasterData", typeof(MasterDataSettings), "MasterDataSettings", "Assets/Resources/MasterDataSettings.asset")
        };
    }
}

