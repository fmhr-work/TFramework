using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TFramework.Core.Editor
{
    /// <summary>
    /// Settingsアセット操作ユーティリティ
    /// </summary>
    public static class SettingsAssetUtility
    {
        /// <summary>
        /// Settings探索結果
        /// </summary>
        public sealed class SettingsSearchResult
        {
            public IReadOnlyList<UnityEngine.Object> AllAssets { get; }
            public IReadOnlyList<UnityEngine.Object> AssetsInResources { get; }
            public IReadOnlyList<UnityEngine.Object> AssetsOutsideResources { get; }
            public IReadOnlyList<UnityEngine.Object> AssetsWithResourceName { get; }

            public SettingsSearchResult(
                IReadOnlyList<UnityEngine.Object> allAssets,
                IReadOnlyList<UnityEngine.Object> assetsInResources,
                IReadOnlyList<UnityEngine.Object> assetsOutsideResources,
                IReadOnlyList<UnityEngine.Object> assetsWithResourceName)
            {
                AllAssets = allAssets;
                AssetsInResources = assetsInResources;
                AssetsOutsideResources = assetsOutsideResources;
                AssetsWithResourceName = assetsWithResourceName;
            }
        }

        /// <summary>
        /// 指定型のSettingsをプロジェクトから探索
        /// </summary>
        public static SettingsSearchResult FindAll(Type settingsType, string resourceName)
        {
            if (settingsType == null) throw new ArgumentNullException(nameof(settingsType));

            var all = FindAssetsByType(settingsType);
            var inResources = all.Where(IsInResourcesFolder).ToList();
            var outside = all.Where(a => !IsInResourcesFolder(a)).ToList();
            var withName = all.Where(a => HasFileName(a, $"{resourceName}.asset")).ToList();

            return new SettingsSearchResult(all, inResources, outside, withName);
        }

        /// <summary>
        /// Resources配下に一致するSettingsがあるか探索
        /// </summary>
        public static IReadOnlyList<UnityEngine.Object> FindInResourcesByName(Type settingsType, string resourceName)
        {
            var all = FindAssetsByType(settingsType);
            return all
                .Where(a => IsInResourcesFolder(a) && HasFileName(a, $"{resourceName}.asset"))
                .ToList();
        }

        /// <summary>
        /// ResourcesにSettingsを作成
        /// </summary>
        public static UnityEngine.Object CreateInResources(Type settingsType, string resourceName)
        {
            if (settingsType == null) throw new ArgumentNullException(nameof(settingsType));
            if (string.IsNullOrWhiteSpace(resourceName)) throw new ArgumentException("resourceName is required.", nameof(resourceName));

            EnsureFolder("Assets", "Resources");

            var targetPath = $"Assets/Resources/{resourceName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetPath);
            if (existing != null) return existing;

            var instance = ScriptableObject.CreateInstance(settingsType);
            AssetDatabase.CreateAsset(instance, targetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = instance;
            EditorGUIUtility.PingObject(instance);
            return instance;
        }

        /// <summary>
        /// Resourcesへ移動
        /// </summary>
        public static bool MoveToResources(UnityEngine.Object asset, string resourceName, out string error)
        {
            error = string.Empty;
            if (asset == null)
            {
                error = "asset is null.";
                return false;
            }

            EnsureFolder("Assets", "Resources");

            var srcPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(srcPath))
            {
                error = "asset path not found.";
                return false;
            }

            var dstPath = $"Assets/Resources/{resourceName}.asset";
            if (!string.Equals(srcPath, dstPath, StringComparison.OrdinalIgnoreCase))
            {
                var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dstPath);
                if (existing != null)
                {
                    error = $"Target already exists: {dstPath}";
                    return false;
                }
            }

            var moveError = AssetDatabase.MoveAsset(srcPath, dstPath);
            if (!string.IsNullOrEmpty(moveError))
            {
                error = moveError;
                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        /// <summary>
        /// Resourcesへ複製
        /// </summary>
        public static bool DuplicateToResources(UnityEngine.Object asset, string resourceName, out string error)
        {
            error = string.Empty;
            if (asset == null)
            {
                error = "asset is null.";
                return false;
            }

            EnsureFolder("Assets", "Resources");

            var srcPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(srcPath))
            {
                error = "asset path not found.";
                return false;
            }

            var dstPath = $"Assets/Resources/{resourceName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dstPath);
            if (existing != null)
            {
                error = $"Target already exists: {dstPath}";
                return false;
            }

            if (!AssetDatabase.CopyAsset(srcPath, dstPath))
            {
                error = "CopyAsset failed.";
                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        private static List<UnityEngine.Object> FindAssetsByType(Type settingsType)
        {
            var filter = $"t:{settingsType.Name}";
            var guids = AssetDatabase.FindAssets(filter);
            var list = new List<UnityEngine.Object>(guids.Length);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(path, settingsType);
                if (asset != null) list.Add(asset);
            }
            return list;
        }

        private static bool IsInResourcesFolder(UnityEngine.Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path)) return false;

            path = path.Replace('\\', '/');
            return path.Contains("/Resources/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasFileName(UnityEngine.Object asset, string fileName)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path)) return false;
            return string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureFolder(string parent, string child)
        {
            var combined = $"{parent}/{child}";
            if (AssetDatabase.IsValidFolder(combined)) return;
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}

