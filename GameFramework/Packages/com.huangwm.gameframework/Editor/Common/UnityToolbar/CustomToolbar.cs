using GF.Common;
using GFEditor.Common.Utility;
using GFEditor.Debug;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static UnityEditor.GenericMenu;

namespace GFEditor.Common.UnityToolbar
{
    [InitializeOnLoad]
    public static class CustomToolbar
    {
        public static Action s_OnLeftButtonsGUI;

        private static List<CTBaseData> ms_CTMenuItemDatas;

        private static bool ms_IsInitialized = false;
        private static GUIContent ms_LogConsoleIcon;

        static CustomToolbar()
        {
            ToolbarExtend.OnLeftToolbarGUI += OnLeftGUI;
            ToolbarExtend.OnRightToolbarGUI += OnRightGUI;
        }

        /// <summary>
        /// 有些GUI的东西只能在OnGUI中初始化，所以在GUI中每帧调用一次这个方法来初始化
        /// </summary>
        private static void InitialieGUI()
        {
            if (ms_IsInitialized)
            {
                return;
            }

            ms_IsInitialized = true;

            ms_LogConsoleIcon = EditorGUIUtility.TrTextContentWithIcon("", "Open log console", "UnityEditor.ConsoleWindow");
        }

        private static void OnLeftGUI()
        {
            InitialieGUI();

            if (EditorGUILayout.DropdownButton(EditorGUIUtility.TrTextContent("GF")
                , FocusType.Keyboard
                , ToolbarExtend.GetCommandDropdownStyle()
                , GUILayout.Width(40)))
            {
                GenericMenu menu = new GenericMenu();
                FillToolbarGenericMenu(menu);
                menu.DropDown(GF.Common.Utility.UnityEngineReflectionUtility.GUILayoutGroup.GetLast(GF.Common.Utility.UnityEngineReflectionUtility.GUILayoutUtility.TopLevel()));
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(ms_LogConsoleIcon, ToolbarExtend.GetCommandStyle()))
            {
#if UNITY_2019_1_OR_NEWER
                EditorWindow.CreateWindow
#else
                EditorWindow.GetWindow
#endif
                        <LogConsoleWindow>(UnityEditorReflectionUtility.GetSceneWindowType()
                    , UnityEditorReflectionUtility.GetGameWindowType());
            }
            s_OnLeftButtonsGUI?.Invoke();
        }

        private static void OnRightGUI()
        {
            InitialieGUI();
        }

#region Custom MenuItem
        private static void FillToolbarGenericMenu(GenericMenu menu)
        {
            if (ms_CTMenuItemDatas == null)
            {
                ms_CTMenuItemDatas = new List<CTBaseData>();

                List<GF.Common.Utility.ReflectionUtility.MethodAndAttributeData> menuItemMethods = new List<GF.Common.Utility.ReflectionUtility.MethodAndAttributeData>();
                List<GF.Common.Utility.ReflectionUtility.MethodAndAttributeData> fillMethods = new List<GF.Common.Utility.ReflectionUtility.MethodAndAttributeData>();
                List<GF.Common.Utility.ReflectionUtility.TypeAndAttributeData> createAssetTypes = new List<GF.Common.Utility.ReflectionUtility.TypeAndAttributeData>();

                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int iAssembly = 0; iAssembly < assemblies.Length; iAssembly++)
                {

                    GF.Common.Utility.ReflectionUtility.CollectionMethodWithAttribute(fillMethods
                       , assemblies[iAssembly]
                       , BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
                       , typeof(CTCustomFillAttribute)
                       , false);

                    GF.Common.Utility.ReflectionUtility.CollectionMethodWithAttribute(menuItemMethods
                        , assemblies[iAssembly]
                        , BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
                        , typeof(CTMenuItemAttribute)
                        , false);

                    GF.Common.Utility.ReflectionUtility.CollectionTypeWithAttribute(createAssetTypes
                        , assemblies[iAssembly]
                        , typeof(CTCreateAssetMenuItemAttribute)
                        , false);
                }

                for (int iMethod = 0; iMethod < fillMethods.Count; iMethod++)
                {
                    ms_CTMenuItemDatas.Add(new CTCustomFillData(fillMethods[iMethod].Method
                        , (CTCustomFillAttribute)fillMethods[iMethod].Attribute));
                }
                for (int iMethod = 0; iMethod < menuItemMethods.Count; iMethod++)
                {
                    ms_CTMenuItemDatas.Add(new CTMenuItemData(menuItemMethods[iMethod].Method
                        , (CTMenuItemAttribute)menuItemMethods[iMethod].Attribute));
                }
                for (int iType = 0; iType < createAssetTypes.Count; iType++)
                {
                    ms_CTMenuItemDatas.Add(new CTCreateAssetData(createAssetTypes[iType].Type
                        , (CTCreateAssetMenuItemAttribute)createAssetTypes[iType].Attribute));
                }
                ms_CTMenuItemDatas.Sort(CTBaseData.ComparerByPriority);
            }

            for (int iMenuItem = 0; iMenuItem < ms_CTMenuItemDatas.Count; iMenuItem++)
            {
                ms_CTMenuItemDatas[iMenuItem].FillGenericMenu(menu);
            }
        }

        public abstract class CTBaseData
        {
            protected int m_Priority;

            public static int ComparerByPriority(CTBaseData x, CTBaseData y)
            {
                return x.m_Priority - y.m_Priority;
            }

            public abstract void FillGenericMenu(GenericMenu menu);
        }

        public class CTMenuItemData : CTBaseData
        {
            private GUIContent m_Content;
            private MenuFunction m_Function;

            public CTMenuItemData(MethodInfo method, CTMenuItemAttribute attribute)
            {
                m_Content = new GUIContent(attribute.MenuName);
                m_Priority = attribute.Priority;
                m_Function = (MenuFunction)Delegate.CreateDelegate(typeof(MenuFunction), method);
            }

            public override void FillGenericMenu(GenericMenu menu)
            {
                menu.AddItem(m_Content
                    , false
                    , m_Function);
            }
        }

        public class CTCreateAssetData : CTBaseData
        {
            private GUIContent m_Content;
            private CreateAssetData m_CreateAssetData;


            private static void CreateAsset(object userData)
            {
                CreateAssetData createAssetData = (CreateAssetData)userData;

                string selectionPath = string.Empty;
                string[] selectionAssetGUIDs = Selection.assetGUIDs;
                if (selectionAssetGUIDs.Length > 0)
                {
                    selectionPath = AssetDatabase.GUIDToAssetPath(selectionAssetGUIDs[0]);
                    if (!string.IsNullOrEmpty(selectionPath))
                    {
                        if (System.IO.File.Exists(selectionPath))
                        {
                            selectionPath = selectionPath.Substring(0, selectionPath.LastIndexOf('/'));
                        }
                    }
                }

                if (string.IsNullOrEmpty(selectionPath))
                {
                    selectionPath = "Assets";
                }

                string targetPath = $"{selectionPath}/{createAssetData.AssetName}.asset";
                int fileIndex = -1;
                while(System.IO.File.Exists(targetPath))
                {
                    fileIndex++;
                    targetPath = $"{selectionPath}/{createAssetData.AssetName} {fileIndex}.asset";
                }

                ScriptableObject asset = ScriptableObject.CreateInstance(createAssetData.Type);
                AssetDatabase.CreateAsset(asset, targetPath);
                Selection.activeObject = asset;
            }

            public CTCreateAssetData(Type type, CTCreateAssetMenuItemAttribute attribute)
            {
                m_Content = new GUIContent(attribute.MenuName);
                m_Priority = attribute.Priority;

                m_CreateAssetData.Type = type;
                m_CreateAssetData.AssetName = attribute.DefaultAssetName ?? type.Name;
            }

            public override void FillGenericMenu(GenericMenu menu)
            {
                menu.AddItem(m_Content
                    , false
                    , CreateAsset
                    , m_CreateAssetData);
            }

            public struct CreateAssetData
            {
                public Type Type;
                public string AssetName;
            }
        }

        public class CTCustomFillData : CTBaseData
        {
            private MethodInfo m_Method;

            public CTCustomFillData(MethodInfo method, CTCustomFillAttribute attribute)
            {
                m_Priority = attribute.Priority;
                m_Method = method;
            }

            public override void FillGenericMenu(GenericMenu menu)
            {
                m_Method.Invoke(null, new object[] { menu });
            }
        }
#endregion
    }
}