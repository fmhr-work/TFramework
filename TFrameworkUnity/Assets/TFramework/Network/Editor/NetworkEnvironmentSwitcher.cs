using UnityEditor;
using UnityEngine;
using System.Linq;
using TFramework.Debug;

namespace TFramework.Network.Editor
{
    /// <summary>
    /// ネットワーク環境の切り替えと、マクロ定義の更新を行うエディタスクリプト
    /// </summary>
    public static class NetworkEnvironmentSwitcher
    {
        public static void SwitchEnvironment(string newEnvironmentName)
        {
            var settings = NetworkSettings.Instance;
            if (settings == null) return;

            var envConfig = settings.Environments.FirstOrDefault(e => e.Name == newEnvironmentName);
            if (envConfig == null)
            {
                TLogger.Error($"選択された環境 '{newEnvironmentName}' は存在しません。");
                return;
            }

            var oldEnvironmentName = settings.CurrentEnvironment;
            if (oldEnvironmentName == newEnvironmentName)
            {
                // 既に同じ環境であれば何もしない
                return;
            }

            settings.CurrentEnvironment = newEnvironmentName;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            UpdateScriptingDefineSymbols(oldEnvironmentName, newEnvironmentName);

            TLogger.Info($"環境を '{newEnvironmentName}' に変更しました。");
        }

        private static void UpdateScriptingDefineSymbols(string oldEnv, string newEnv)
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            
            // 最新のAPIを使用してシンボルを取得
            #if UNITY_2023_1_OR_NEWER
            var target = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            var definesString = PlayerSettings.GetScriptingDefineSymbols(target);
            #else
            var definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            #endif
            
            var defines = definesString.Split(';').ToList();

            // 古い環境のマクロを削除
            if (!string.IsNullOrEmpty(oldEnv))
            {
                string oldMacro = GetMacroFromEnvironmentName(oldEnv);
                if (defines.Contains(oldMacro))
                {
                    defines.Remove(oldMacro);
                }
            }

            // 新しい環境のマクロを追加
            if (!string.IsNullOrEmpty(newEnv))
            {
                string newMacro = GetMacroFromEnvironmentName(newEnv);
                if (!defines.Contains(newMacro))
                {
                    defines.Add(newMacro);
                }
            }

            #if UNITY_2023_1_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
            #else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines));
            #endif
        }

        private static string GetMacroFromEnvironmentName(string envName)
        {
            if (string.IsNullOrEmpty(envName)) return string.Empty;
            
            // 環境名からプレフィックスを取得し、大文字に変換 (例: development -> DEV)
            string prefix = envName.Length >= 3 ? envName.Substring(0, 3) : envName;
            return prefix.ToUpper();
        }
    }
}
