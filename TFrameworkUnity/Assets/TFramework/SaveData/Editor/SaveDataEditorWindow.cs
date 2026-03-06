using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TFramework.SaveData.Editor
{
    /// <summary>
    /// 保存データの確認・削除を行うEditorウィンドウ
    /// </summary>
    public class SaveDataEditorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string _persistentDataPath;
        private DirectoryInfo[] _slotDirectories;
        
        [MenuItem("TFramework/SaveData/Debug Window")]
        private static void ShowWindow()
        {
            var window = GetWindow<SaveDataEditorWindow>("SaveData Debug");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            _persistentDataPath = Application.persistentDataPath;
            RefreshSlotList();
        }

        private void OnGUI()
        {
            #region Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshSlotList();
            }

            if (GUILayout.Button("Open Folder", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                EditorUtility.RevealInFinder(_persistentDataPath);
            }

            GUILayout.FlexibleSpace();

            bool hasSlots = _slotDirectories is { Length: > 0 };
            GUI.enabled = hasSlots;
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Delete All", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Delete all save data. Are you sure?", "Delete", "Cancel"))
                {
                    DeleteAllSlots();
                }
            }
            GUI.enabled = true;
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
            #endregion

            #region Path Info
            EditorGUILayout.HelpBox($"Save Path: {_persistentDataPath}", MessageType.Info);
            #endregion

            #region Slot List
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_slotDirectories == null || _slotDirectories.Length == 0)
            {
                EditorGUILayout.LabelField("No save data found", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (var slotDir in _slotDirectories)
                {
                    DrawSlotGroup(slotDir);
                }
            }

            EditorGUILayout.EndScrollView();
            #endregion
        }

        /// <summary>
        /// スロットグループを描画
        /// </summary>
        private void DrawSlotGroup(DirectoryInfo slotDir)
        {
            EditorGUILayout.BeginVertical("box");

            // ヘッダー行
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📁 {slotDir.Name}", EditorStyles.boldLabel);
            
            GUI.color = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("Delete Slot", GUILayout.Width(90)))
            {
                if (EditorUtility.DisplayDialog("Confirm", $"Delete {slotDir.Name}. Are you sure?", "Delete", "Cancel"))
                {
                    slotDir.Delete(true);
                    RefreshSlotList();
                    GUIUtility.ExitGUI();
                }
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            // ファイル一覧
            var files = slotDir.GetFiles().Where(f => !f.Name.EndsWith(".meta")).ToArray();
            
            if (files.Length == 0)
            {
                EditorGUILayout.LabelField("  (No files)", EditorStyles.miniLabel);
            }
            else
            {
                // テーブルヘッダー
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("File Name", EditorStyles.miniLabel, GUILayout.MinWidth(150));
                EditorGUILayout.LabelField("Size", EditorStyles.miniLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("Last Modified", EditorStyles.miniLabel, GUILayout.Width(140));
                GUILayout.Space(60); // 削除ボタンのスペース
                EditorGUILayout.EndHorizontal();

                foreach (var file in files)
                {
                    DrawFileRow(file);
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// ファイル行を描画
        /// </summary>
        private void DrawFileRow(FileInfo file)
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField(file.Name, GUILayout.MinWidth(150));
            EditorGUILayout.LabelField(FormatFileSize(file.Length), GUILayout.Width(80));
            EditorGUILayout.LabelField(file.LastWriteTime.ToString("yyyy/MM/dd HH:mm"), GUILayout.Width(140));

            if (GUILayout.Button("Delete", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("Confirm", $"Delete {file.Name}. Are you sure?", "Delete", "Cancel"))
                {
                    file.Delete();
                    RefreshSlotList();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// スロットディレクトリ一覧を更新
        /// </summary>
        private void RefreshSlotList()
        {
            if (!Directory.Exists(_persistentDataPath))
            {
                _slotDirectories = new DirectoryInfo[0];
                return;
            }

            var rootDir = new DirectoryInfo(_persistentDataPath);
            _slotDirectories = rootDir.GetDirectories("Slot_*")
                .OrderBy(d => d.Name)
                .ToArray();
            
            Repaint();
        }

        /// <summary>
        /// 全スロットを削除
        /// </summary>
        private void DeleteAllSlots()
        {
            if (_slotDirectories == null) return;

            foreach (var dir in _slotDirectories)
            {
                if (dir.Exists)
                {
                    dir.Delete(true);
                }
            }

            RefreshSlotList();
        }

        /// <summary>
        /// ファイルサイズを読みやすい形式にフォーマット
        /// </summary>
        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
    }
}
