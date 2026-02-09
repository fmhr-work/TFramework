using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using TFramework.Core;
using TFramework.MasterData;

namespace TFramework.MasterData.Editor
{
    /// <summary>
    /// 生成されたクラスを用いてScriptableObjectを作成し、CSVデータを注入するクラス
    /// コンパイル後の自動実行もサポートする
    /// </summary>
    [InitializeOnLoad]
    public static class MasterDataAssetUpdater
    {
        static MasterDataAssetUpdater()
        {
            // コンパイル完了時に呼ばれるように設計されているが、再ロード時に毎回チェックする
            // フラグが立っていればAsset更新を実行
            if (EditorPrefs.GetBool("MasterData_PendingAssetUpdate", false))
            {
                // 遅延実行（AssetDatabaseの準備を待つため）
                EditorApplication.delayCall += () =>
                {
                    EditorPrefs.SetBool("MasterData_PendingAssetUpdate", false);
                    UpdateAssetsFromPrefs();
                };
            }
        }

        private static void UpdateAssetsFromPrefs()
        {
            // Settingsを探して実行
            var guids = AssetDatabase.FindAssets("t:MasterDataSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var settings = AssetDatabase.LoadAssetAtPath<MasterDataSettings>(path);
                if (settings != null)
                {
                    UpdateAssets(settings);
                }
            }
        }

        /// <summary>
        /// Asset更新処理のメインロジック
        /// </summary>
        public static void UpdateAssets(MasterDataSettings settings)
        {
            if (settings == null) return;
            
            string extPath = EditorPrefs.GetString("TFramework_MasterData_ExtPath", "../MasterData");
            string csvPath = Path.GetFullPath(Path.Combine(Application.dataPath, extPath));
            string assetOutputPath = Path.Combine("Assets", settings.AssetOutputPath);

            if (!Directory.Exists(csvPath)) return;
            if (!Directory.Exists(assetOutputPath))
            {
                Directory.CreateDirectory(assetOutputPath);
            }

            var generatedContainers = new List<ScriptableObject>();
            var files = Directory.GetFiles(csvPath, "*.csv");

            // 生成コードのアセンブリ（Assembly-CSharp）を取得
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

            if (assembly == null)
            {
                Debug.LogError("[MasterData] Assembly-CSharpが見つからないため、型情報を取得できない。");
                return;
            }

            foreach (var file in files)
            {
                try
                {
                    string className = Path.GetFileNameWithoutExtension(file);
                    string fullClassName = $"Game.MasterData.Generated.{className}";
                    string containerClassName = $"{fullClassName}Container";

                    // 型を取得
                    var dataClassType = assembly.GetType(fullClassName);
                    var containerClassType = assembly.GetType(containerClassName);

                    if (dataClassType == null || containerClassType == null)
                    {
                        Debug.LogError($"[MasterData] クラスが見つからない: {className}. コード生成を実行したか？");
                        continue;
                    }

                    // CSV読み込み
                    var csvContent = File.ReadAllText(file, System.Text.Encoding.UTF8);
                    var csvData = CsvParser.Parse(csvContent);
                    if (csvData.Count < 3) continue;

                    var variableNames = csvData[0];
                    var types = csvData[2];

                    // データリストを作成 (List<T>)
                    var listType = typeof(List<>).MakeGenericType(dataClassType);
                    var dataList = Activator.CreateInstance(listType) as IList;

                    // 4行目からデータ読み込み
                    for (int row = 3; row < csvData.Count; row++)
                    {
                        var rowData = csvData[row];
                        var instance = Activator.CreateInstance(dataClassType);

                        for (int col = 0; col < variableNames.Length; col++)
                        {
                            if (col >= rowData.Length) break;
                            if (types[col] == "dummy") continue;

                            var fieldName = variableNames[col];
                            var fieldInfo = dataClassType.GetField(fieldName);
                            if (fieldInfo == null) continue;

                            string valStr = rowData[col];
                            object val = ParseValue(valStr, types[col], fieldInfo.FieldType);
                            fieldInfo.SetValue(instance, val);
                        }
                        dataList.Add(instance);
                    }

                    // コンテナScriptableObjectの作成またはロード
                    string assetPath = $"{assetOutputPath}/{className}Container.asset";
                    var container = AssetDatabase.LoadAssetAtPath(assetPath, containerClassType) as ScriptableObject;
                    if (container == null)
                    {
                        container = ScriptableObject.CreateInstance(containerClassType);
                        AssetDatabase.CreateAsset(container, assetPath);
                    }

                    // SetDataメソッドを呼ぶ
                    // List<T>を受け取る internal void SetData(List<{className}> data)
                    var setDataMethod = containerClassType.GetMethod("SetData", 
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    
                    if (setDataMethod != null)
                    {
                        setDataMethod.Invoke(container, new object[] { dataList });
                        EditorUtility.SetDirty(container);
                        generatedContainers.Add(container);
                        Debug.Log($"[MasterData] Asset Updated: {className}");
                    }
                    else
                    {
                        Debug.LogError($"[MasterData] SetDataメソッドが見つからない: {containerClassName}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MasterData] Asset更新失敗 ({file}): {e}");
                }
            }

            // Settingsのコンテナリストを更新
            settings.SetContainers(generatedContainers);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            
            Debug.Log("[MasterData] 全てのアセット更新が完了した。");
        }

        private static object ParseValue(string str, string csvType, Type targetType)
        {
            if (string.IsNullOrEmpty(str)) return GetDefaultValue(targetType);

            try
            {
                switch (csvType.ToLower())
                {
                    case "int": return int.Parse(str);
                    case "long": return long.Parse(str);
                    case "float": return float.Parse(str, CultureInfo.InvariantCulture);
                    case "bool": 
                        if (bool.TryParse(str, out bool b)) return b;
                        if (str == "1") return true; 
                        return false;
                    case "string": return str;
                    case "datetime": return DateTime.Parse(str, CultureInfo.InvariantCulture);
                    case "enum":
                        // Enumパース
                        return Enum.Parse(targetType, str);
                    default: return str;
                }
            }
            catch
            {
                Debug.LogWarning($"[MasterData] 値のパースに失敗: {str} (Type: {csvType})");
                return GetDefaultValue(targetType);
            }
        }

        private static object GetDefaultValue(Type t)
        {
            if (t.IsValueType) return Activator.CreateInstance(t);
            return null; // stringなど
        }
    }
}
