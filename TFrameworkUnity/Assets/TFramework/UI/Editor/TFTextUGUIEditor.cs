#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TFramework.UI.Editor
{
    /// <summary>
    /// TFTextUGUI用のカスタムエディター
    /// </summary>
    [CustomEditor(typeof(TFTextUGUI))]
    [CanEditMultipleObjects]
    public class TFTextUGUIEditor : TMPro.EditorUtilities.TMP_EditorPanelUI
    {
        private SerializedProperty _useLocalizationProp;
        private SerializedProperty _localizationKeyProp;
        private SerializedProperty _localizationParametersProp;
        private SerializedProperty _typewriterSpeedProp;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            _useLocalizationProp = serializedObject.FindProperty("_useLocalization");
            _localizationKeyProp = serializedObject.FindProperty("_localizationKey");
            _localizationParametersProp = serializedObject.FindProperty("_localizationParameters");
            _typewriterSpeedProp = serializedObject.FindProperty("_typewriterSpeed");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // TFramework設定セクション
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("TFramework Settings", EditorStyles.boldLabel);
            
            DrawTFrameworkSettings();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("TextMeshPro Settings", EditorStyles.boldLabel);
            
            serializedObject.ApplyModifiedProperties();

            // 基底クラス（TextMeshPro）のインスペクター描画
            base.OnInspectorGUI();
        }

        private void DrawTFrameworkSettings()
        {
            // Localization Section
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(_useLocalizationProp, new GUIContent("Use Localization", "ローカライズを使用"));
            
            if (_useLocalizationProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_localizationKeyProp, new GUIContent("Localization Key", "ローカライズキー"));
                EditorGUILayout.PropertyField(_localizationParametersProp, new GUIContent("Parameters", "ローカライズパラメーター"), true);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(5);
            
            // Animation Section
            EditorGUILayout.PropertyField(_typewriterSpeedProp, new GUIContent("Typewriter Speed", "タイプライター効果の速度（文字/秒）"));
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif
