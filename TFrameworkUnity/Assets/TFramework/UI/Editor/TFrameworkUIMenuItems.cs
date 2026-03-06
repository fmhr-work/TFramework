using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TFramework.UI.Editor
{
    /// <summary>
    /// TFramework UI用のエディタメニュー項目
    /// Hierarchy右クリックメニューからTFrameworkコンポーネントを作成
    /// </summary>
    public static class TFrameworkUIMenuItems
    {
        #region Text Creation
        /// <summary>
        /// TFTextUGUIを持つGameObjectを作成
        /// </summary>
        [MenuItem("GameObject/TFramework/UI/TF Text - TextMeshPro", false, 10)]
        private static void CreateTFText(MenuCommand menuCommand)
        {
            // GameObjectを作成
            var go = new GameObject("TF Text (TMP)");
            
            // TFTextUGUIコンポーネントを追加
            var textComponent = go.AddComponent<TFTextUGUI>();
            
            // デフォルト設定
            textComponent.text = "New Text";
            textComponent.fontSize = 24;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            // RectTransformの設定
            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50);
            
            // 親の設定とUndo登録
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
        #endregion

        #region Button Creation
        /// <summary>
        /// UIButtonを持つGameObjectを作成
        /// </summary>
        [MenuItem("GameObject/TFramework/UI/TF Button", false, 11)]
        private static void CreateTFButton(MenuCommand menuCommand)
        {
            // ボタン本体を作成
            var buttonGO = new GameObject("TF Button");
            var rectTransform = buttonGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(160, 30);
            
            // UIButtonコンポーネントを追加
            var uiButton = buttonGO.AddComponent<UIButton>();
            
            // Imageコンポーネントを追加（ボタンの背景）
            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 1f);
            
            // 子オブジェクトとしてテキストを作成
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            var textComponent = textGO.AddComponent<TFTextUGUI>();
            textComponent.text = "Button";
            textComponent.fontSize = 18;
            textComponent.color = Color.black;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.verticalAlignment = VerticalAlignmentOptions.Middle;
            
            // 親の設定とUndo登録
            GameObjectUtility.SetParentAndAlign(buttonGO, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(buttonGO, "Create " + buttonGO.name);
            Selection.activeObject = buttonGO;
        }

        /// <summary>
        /// UIButtonを持つGameObject（アイコン付き）を作成
        /// </summary>
        [MenuItem("GameObject/TFramework/UI/TF Button (With Icon)", false, 12)]
        private static void CreateTFButtonWithIcon(MenuCommand menuCommand)
        {
            // ボタン本体を作成
            var buttonGO = new GameObject("TF Button (Icon)");
            var rectTransform = buttonGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(160, 40);
            
            // UIButtonコンポーネントを追加
            var uiButton = buttonGO.AddComponent<UIButton>();
            
            // Imageコンポーネントを追加（ボタンの背景）
            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 1f);
            
            // アイコン用のImageを作成
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(buttonGO.transform, false);
            
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(20, 0);
            iconRect.sizeDelta = new Vector2(24, 24);
            
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.color = Color.white;
            
            // テキストを作成
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.3f, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.sizeDelta = Vector2.zero;
            
            var textComponent = textGO.AddComponent<TFTextUGUI>();
            textComponent.text = "Button";
            textComponent.fontSize = 18;
            textComponent.color = Color.black;
            textComponent.alignment = TextAlignmentOptions.Left;
            textComponent.verticalAlignment = VerticalAlignmentOptions.Middle;
            
            // 親の設定とUndo登録
            GameObjectUtility.SetParentAndAlign(buttonGO, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(buttonGO, "Create " + buttonGO.name);
            Selection.activeObject = buttonGO;
        }
        #endregion

        #region Panel Creation
        /// <summary>
        /// TFramework UI Panel（背景 + タイトル + コンテンツエリア）を作成
        /// </summary>
        [MenuItem("GameObject/TFramework/UI/TF Panel", false, 20)]
        private static void CreateTFPanel(MenuCommand menuCommand)
        {
            // パネル本体
            var panelGO = new GameObject("TF Panel");
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(400, 300);
            
            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            
            // タイトルエリア
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panelGO.transform, false);
            
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(0, 40);
            
            var titleText = titleGO.AddComponent<TFTextUGUI>();
            titleText.text = "Panel Title";
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
            
            // コンテンツエリア
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(panelGO.transform, false);
            
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(10, 10);
            contentRect.offsetMax = new Vector2(-10, -50);
            
            // 親の設定とUndo登録
            GameObjectUtility.SetParentAndAlign(panelGO, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(panelGO, "Create " + panelGO.name);
            Selection.activeObject = panelGO;
        }
        #endregion
    }
}
