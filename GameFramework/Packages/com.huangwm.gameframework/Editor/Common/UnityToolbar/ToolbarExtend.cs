using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace GFEditor.Common.UnityToolbar
{
    public static class ToolbarExtend
    {
        private static Type TOOLBAR_TYPE = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static Type GUIVIEW_TYPE = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GUIView");
        private static PropertyInfo VISUALTREE_PROPERTYINFO = GUIVIEW_TYPE.GetProperty("visualTree"
            , BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo ONGUI_HANDLER_FIELDINFO = typeof(IMGUIContainer).GetField("m_OnGUIHandler"
            , BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo TOOL_ICONS_FIELDINFO = TOOLBAR_TYPE.GetField("s_ShownToolIcons"
            , BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        public static Action OnLeftToolbarGUI;
        public static Action OnRightToolbarGUI;

        private static ScriptableObject ms_CurrentToolbar;
        private static int ms_ToolIconCount;
        private static bool ms_RegistedGUIHandle = false;

        private static GUIStyle ms_CommandLeftStyle;
        private static GUIStyle ms_CommandMidStyle;
        private static GUIStyle ms_CommandRightStyle;
        private static GUIStyle ms_CommandDropdownStyle;
        private static GUIStyle ms_CommandButtonLeftStyle;
        private static GUIStyle ms_CommandButtonRightStyle;
        private static GUIStyle ms_CommandStyle;

        static ToolbarExtend()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        public static GUIStyle GetCommandLeftStyle()
        {
            return ms_CommandLeftStyle;
        }

        public static GUIStyle GetCommandMidStyle()
        {
            return ms_CommandMidStyle;
        }

        public static GUIStyle GetCommandRightStyle()
        {
            return ms_CommandRightStyle;
        }

        public static GUIStyle GetCommandDropdownStyle()
        {
            return ms_CommandDropdownStyle;
        }

        public static GUIStyle GetCommandButtonLeftStyle()
        {
            return ms_CommandButtonLeftStyle;
        }

        public static GUIStyle GetCommandButtonRightStyle()
        {
            return ms_CommandButtonRightStyle;
        }

        public static GUIStyle GetCommandStyle()
        {
            return ms_CommandStyle;
        }

        private static void OnUpdate()
        {
            if (ms_RegistedGUIHandle)
            {
                return;
            }

            if (ms_CurrentToolbar == null)
            {
                UnityEngine.Object[] toolbars = Resources.FindObjectsOfTypeAll(TOOLBAR_TYPE);
                if (toolbars.Length > 0)
                {
                    ms_CurrentToolbar = (ScriptableObject)toolbars[0];
                }
                else
                {
                    return;
                }
            }

            if (TOOL_ICONS_FIELDINFO.GetValue(null) != null)
            {
                ms_ToolIconCount = TOOL_ICONS_FIELDINFO != null ? ((Array)TOOL_ICONS_FIELDINFO.GetValue(null)).Length : 6;

                VisualElement visualTree = (VisualElement)VISUALTREE_PROPERTYINFO.GetValue(ms_CurrentToolbar, null);
                IMGUIContainer container = (IMGUIContainer)visualTree[0];
                Action onGUIHandler = (Action)ONGUI_HANDLER_FIELDINFO.GetValue(container);
                onGUIHandler -= OnGUI;
                onGUIHandler += OnGUI;
                ONGUI_HANDLER_FIELDINFO.SetValue(container, onGUIHandler);

                ms_RegistedGUIHandle = true;
                ms_CommandLeftStyle = null;
            }
        }

        private static void OnGUI()
        {
            if (ms_CommandLeftStyle == null)
            {
                ms_CommandLeftStyle = new GUIStyle("CommandLeft");
                ms_CommandMidStyle = new GUIStyle("CommandMid");
                ms_CommandRightStyle = new GUIStyle("CommandRight");
                ms_CommandDropdownStyle = new GUIStyle("Dropdown");
                ms_CommandButtonLeftStyle = new GUIStyle("ButtonLeft");
                ms_CommandButtonRightStyle = new GUIStyle("ButtonRight");
                ms_CommandStyle = new GUIStyle("Command")
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    imagePosition = ImagePosition.ImageAbove,
                    fontStyle = FontStyle.Bold,
                };
            }

            float screenWidth = EditorGUIUtility.currentViewWidth;
            // Following calculations match code reflected from Toolbar.OldOnGUI()
            float playButtonsPosition = (screenWidth - 100) / 2;

            Rect leftRect = new Rect(0, 0, screenWidth, Screen.height);
            leftRect.xMin += 10; // Spacing left
            leftRect.xMin += 32 * ms_ToolIconCount; // Tool buttons
            leftRect.xMin += 20; // Spacing between tools and pivot
            leftRect.xMin += 64 * 2; // Pivot buttons
            leftRect.xMin += 20; // Grid button
            leftRect.xMax = playButtonsPosition;

            Rect rightRect = new Rect(0, 0, screenWidth, Screen.height);
            rightRect.xMin = playButtonsPosition;
            rightRect.xMin += ms_CommandMidStyle.fixedWidth * 3; // Play buttons
            rightRect.xMax = screenWidth;
            rightRect.xMax -= 10; // Spacing right
            rightRect.xMax -= 80; // Layout
            rightRect.xMax -= 10; // Spacing between layout and layers
            rightRect.xMax -= 80; // Layers
            rightRect.xMax -= 20; // Spacing between layers and account
            rightRect.xMax -= 80; // Account
            rightRect.xMax -= 10; // Spacing between account and cloud
            rightRect.xMax -= 32; // Cloud
            rightRect.xMax -= 10; // Spacing between cloud and collab
            rightRect.xMax -= 78; // Colab

            // Add spacing around existing controls
            leftRect.xMin += 10;
            leftRect.xMax -=
#if UNITY_2019_1_OR_NEWER
                25
#else
                8
#endif
                ;
            rightRect.xMin -= 15;
            rightRect.xMax -= 10;

            // Add top and bottom margins
            leftRect.y = 5;
            leftRect.height = 24;
            rightRect.y = 5;
            rightRect.height = 24;

            if (leftRect.width > 0)
            {
                GUILayout.BeginArea(leftRect);
                GUILayout.BeginHorizontal();
                OnLeftToolbarGUI?.Invoke();
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            if (rightRect.width > 0)
            {
                GUILayout.BeginArea(rightRect);
                GUILayout.BeginHorizontal();
                OnRightToolbarGUI?.Invoke();
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }
    }
}