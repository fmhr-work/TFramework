using System.IO;
using UnityEditor;
using UnityEngine;

namespace TFramework.MasterData.Editor
{
    /// <summary>
    /// MasterDataを取り込むエディタウィンドウ
    /// </summary>
    public class MasterDataImporterWindow : EditorWindow
    {
        private MasterDataSettings _settings;
        private const string WindowTitle = "MasterData Importer";
        private string _externalDataPath = "../MasterData";
        private const string ExtPathPrefKey = "TFramework_MasterData_ExtPath";
        
        [MenuItem("TFramework/MasterData/Importer")]
        public static void ShowWindow()
        {
            GetWindow<MasterDataImporterWindow>(WindowTitle);
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            _externalDataPath = EditorPrefs.GetString(ExtPathPrefKey, "../MasterData");

            _settings = Resources.Load<MasterDataSettings>("MasterDataSettings");
            if (_settings == null)
            {
                var guids = AssetDatabase.FindAssets("t:MasterDataSettings");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _settings = AssetDatabase.LoadAssetAtPath<MasterDataSettings>(path);
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("MasterData Importer", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _settings = (MasterDataSettings)EditorGUILayout.ObjectField("Settings", _settings, typeof(MasterDataSettings), false);
            if (EditorGUI.EndChangeCheck())
            {
                // Settingsが変更されたら保存等は特に不要だが、参照が変わるだけ
            }

            if (_settings == null)
            {
                EditorGUILayout.HelpBox("MasterDataSettingsが見つからない。作成するか選択すること。", MessageType.Warning);
                if (GUILayout.Button("Create Settings"))
                {
                    CreateSettings();
                }
                return;
            }

            GUILayout.Space(10);
            GUILayout.Label("Path Settings", EditorStyles.boldLabel);

            // CSV Path with Browse Button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("CSV Path");
            var newPath = EditorGUILayout.TextField(_externalDataPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string openPath = string.IsNullOrEmpty(newPath) ? Application.dataPath : Path.Combine(Application.dataPath, newPath);
                string selectedPath = EditorUtility.OpenFolderPanel("Select CSV Folder", openPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    newPath = selectedPath.StartsWith(Application.dataPath) 
                        ? FileUtil.GetProjectRelativePath(selectedPath) 
                        : selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (newPath != _externalDataPath)
            {
                _externalDataPath = newPath;
                EditorPrefs.SetString(ExtPathPrefKey, _externalDataPath);
            }

            // Output Paths (Read-only for now, or editable if needed)
            EditorGUILayout.LabelField("Code Output:", _settings.CodeOutputPath);
            EditorGUILayout.LabelField("Asset Output:", _settings.AssetOutputPath);

            GUILayout.Space(20);

            // One-click Import
            if (GUILayout.Button("Import All (Generate & Update)", GUILayout.Height(40)))
            {
                ImportAll();
            }
            
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("Import Allを実行すると、コード生成とAsset更新が一括で行われる。", MessageType.Info);
        }

        private void CreateSettings()
        {
            var asset = CreateInstance<MasterDataSettings>();
            if (!Directory.Exists("Assets/TFramework/Resources"))
            {
                Directory.CreateDirectory("Assets/TFramework/Resources");
            }
            AssetDatabase.CreateAsset(asset, "Assets/TFramework/Resources/MasterDataSettings.asset");
            AssetDatabase.SaveAssets();
            _settings = asset;
        }

        private void ImportAll()
        {
            if (_settings == null) return;
            
            // Generate Code
            string csvValuesPath = _externalDataPath;
            // 相対パスの場合、ProjectRoot基準で絶対パス化 ("Assets/../Path"などを想定)
            if (!Path.IsPathRooted(csvValuesPath))
            {
                 csvValuesPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", csvValuesPath));
            }

            // "Assets/..." で始まる場合、そのまま絶対パスに変換
            if(csvValuesPath.StartsWith("Assets"))
            {
                csvValuesPath = Path.GetFullPath(csvValuesPath); 
            }
            
            // パスが存在しない場合、Assetsフォルダ基準で再試行。それでもダメならエラー
            if (!Directory.Exists(csvValuesPath)) 
            {
                 csvValuesPath = Path.GetFullPath(Path.Combine(Application.dataPath, _externalDataPath));
                 if (!Directory.Exists(csvValuesPath))
                 {
                    EditorUtility.DisplayDialog("Error", $"CSVディレクトリが見つからない: {_externalDataPath}", "OK");
                    return;
                 }
            }

            var files = Directory.GetFiles(csvValuesPath, "*.csv");
            if (files.Length == 0)
            {
                Debug.LogWarning("[MasterData] .csvファイルが見つからない");
                return;
            }

            string codeOutputPath = Path.Combine(Application.dataPath, _settings.CodeOutputPath);
            if (!Directory.Exists(codeOutputPath)) Directory.CreateDirectory(codeOutputPath);

            var classNames = new System.Collections.Generic.List<string>();
            bool errorOccurred = false;

            foreach (var file in files)
            {
                try
                {
                    var content = File.ReadAllText(file, System.Text.Encoding.UTF8);
                    var csvData = CsvParser.Parse(content);
                    var className = Path.GetFileNameWithoutExtension(file);
                    
                    CodeGenerator.GenerateClass(className, csvData, codeOutputPath);
                    classNames.Add(className);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[MasterData] コード生成失敗 ({file}): {e.Message}");
                    errorOccurred = true;
                }
            }

            if (classNames.Count > 0)
            {
                CodeGenerator.GenerateServiceExtensions(classNames, codeOutputPath);
            }

            if (errorOccurred)
            {
                EditorUtility.DisplayDialog("Error", "コード生成中にエラーが発生した。コンソールを確認すること。", "OK");
                return;
            }

            // Set flag for Asset Update
            EditorPrefs.SetBool("MasterData_PendingAssetUpdate", true);

            // Refresh to trigger compilation
            AssetDatabase.Refresh();
            
            Debug.Log("[MasterData] コード生成完了。コンパイル後にAsset更新が実行される。");
        }
    }
}
