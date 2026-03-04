using UnityEditor;
using UnityEngine;

namespace TFramework.Network.Editor
{
    /// <summary>
    /// NetworkSettingsのカスタムインスペクタ
    /// </summary>
    [CustomEditor(typeof(NetworkSettings))]
    public class NetworkSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var envProp = serializedObject.FindProperty("_currentEnvironment");
            
            EditorGUILayout.HelpBox($"現在の環境: {envProp.stringValue}\n環境の切り替えは上部メニュー [TFramework] -> [Network] -> [Environments] から行ってください。", MessageType.Info);
            
            // _currentEnvironment 自体はインスペクタでは編集不可にする（メニューから変更させるため）
            GUI.enabled = false;
            EditorGUILayout.PropertyField(envProp);
            GUI.enabled = true;

            EditorGUILayout.Space();

            // その他のプロパティを描画
            DrawPropertiesExcluding(serializedObject, "m_Script", "_currentEnvironment");

            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(20);
            
            EditorGUILayout.HelpBox("環境を追加または削除した場合は、以下のボタンを押して上部メニューを更新してください。", MessageType.Warning);

            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button("Apply & Refresh Environment Menu", GUILayout.Height(35)))
            {
                NetworkMenuGenerator.Generate();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
