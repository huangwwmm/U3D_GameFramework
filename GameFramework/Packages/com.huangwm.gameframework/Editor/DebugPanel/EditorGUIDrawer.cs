using GF.Common.Data;
using GF.DebugPanel;
using UnityEditor;
using UnityEngine;

namespace GFEditor.DebugPanel
{
    public class EditorGUIDrawer : IGUIDrawer
    {
        private GUIStyle m_BoxStyle;
        private GUIStyle m_ImportantBoxStyle;
        private GUIStyle m_WarningBoxStyle;
        private GUIStyle m_LabelStyle;
        /// <summary>
        /// 比较重要的文本用这个Style
        /// </summary>
        private GUIStyle m_ImportantLabelStyle;
        /// <summary>
        /// 警告用的
        /// </summary>
        private GUIStyle m_WarningLabelStyle;

        private PrefsValue<Color> m_NormalTextColor;
        private PrefsValue<Color> m_ImportantTextColor;
        private PrefsValue<Color> m_WarningTextColor;

        private float m_PanelWdith;

        public EditorGUIDrawer()
        {
            m_NormalTextColor = new PrefsValue<Color>("EditorGUIDrawer m_NormalTextColor", new Color(172.0f / 255.0f, 172.0f / 255.0f, 172.0f / 255.0f, 255.0f / 255.0f));
            m_ImportantTextColor = new PrefsValue<Color>("EditorGUIDrawer m_ImportantTextColor", Color.green);
            m_WarningTextColor = new PrefsValue<Color>("EditorGUIDrawer m_WarningTextColor", Color.red);

            m_LabelStyle = new GUIStyle(GUI.skin.label);
            m_ImportantLabelStyle = new GUIStyle(GUI.skin.label);
            m_WarningLabelStyle = new GUIStyle(GUI.skin.label);

            m_BoxStyle = new GUIStyle(GUI.skin.box);
            m_BoxStyle.alignment = TextAnchor.MiddleLeft;
            m_ImportantBoxStyle = new GUIStyle(GUI.skin.box);
            m_ImportantBoxStyle.alignment = TextAnchor.MiddleLeft;
            m_WarningBoxStyle = new GUIStyle(GUI.skin.box);
            m_WarningBoxStyle.alignment = TextAnchor.MiddleLeft;
            UpdateGUIStyle();
        }

        public void BeginToolbarHorizontal()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
        }

        public void ImportantLabel(string label)
        {
            EditorGUILayout.LabelField(label, m_ImportantLabelStyle);
        }

        public bool IsEditor()
        {
            return true;
        }

        public bool ToolbarButton(bool value, string label)
        {
            bool newValue = GUILayout.Toggle(value, label, EditorStyles.toolbarButton);
            return newValue != value;
        }

        public bool ToolbarToggle(bool value, string label)
        {
            return GUILayout.Toggle(value, label, EditorStyles.toolbarButton);
        }

        public void DoGUI()
        {
            m_NormalTextColor.Set(EditorGUILayout.ColorField("普通文本", m_NormalTextColor));
            m_ImportantTextColor.Set(EditorGUILayout.ColorField("重要文本", m_ImportantTextColor));
            m_WarningTextColor.Set(EditorGUILayout.ColorField("警告文本", m_WarningTextColor));
            if (GUILayout.Button("应用"))
            {
                UpdateGUIStyle();
            }
        }

        private void UpdateGUIStyle()
        {
            m_LabelStyle.normal.textColor = m_NormalTextColor;
            m_ImportantLabelStyle.normal.textColor = m_ImportantTextColor;
            m_WarningLabelStyle.normal.textColor = m_WarningTextColor;

            m_BoxStyle.normal.textColor = m_NormalTextColor;
            m_ImportantBoxStyle.normal.textColor = m_ImportantTextColor;
            m_WarningBoxStyle.normal.textColor = m_WarningTextColor;
        }

        public void EndHorizontal()
        {
            EditorGUILayout.EndHorizontal();
        }

        public void Space()
        {
            EditorGUILayout.Space();
        }

        public void CalcMinMaxWidth_Button(GUIContent content, out float minWidth, out float maxWidth)
        {
            GUI.skin.button.CalcMinMaxWidth(content, out minWidth, out maxWidth);
        }

        public float GetPanelWidth()
        {
            return m_PanelWdith;
        }

        public void SetPanelWidth(float panelWidth)
        {
            m_PanelWdith = panelWidth;
        }
    }
}