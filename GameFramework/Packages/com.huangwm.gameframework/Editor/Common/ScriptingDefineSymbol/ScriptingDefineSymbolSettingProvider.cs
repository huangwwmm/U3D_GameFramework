using GF.Common.Data;
using GF.Common.Debug;
using GFEditor.Common.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Common.ScriptingDefineSymbol
{
    public class ScriptingDefineSymbolSettingProvider : SettingsProvider
    {
        private readonly List<DefineGroup> m_DefineGroups;

        private PrefsValue<bool> m_DefineGroupFoldout;
        private PrefsValue<bool> m_OtherFoldout;

        private HashSet<string> m_Defines;
        private string m_OtherDefines = string.Empty;

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new ScriptingDefineSymbolSettingProvider("GF/Scripting Define Symbol "
                , SettingsScope.Project
                , new HashSet<string>(new[] { "GF" }));
        }

        public ScriptingDefineSymbolSettingProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
            m_DefineGroupFoldout = new PrefsValue<bool>("ScriptingDefineSymbolSettingProvider m_DefineGroupFoldout");
            m_OtherFoldout = new PrefsValue<bool>("ScriptingDefineSymbolSettingProvider m_OtherFoldout");

            m_DefineGroups = new List<DefineGroup>();

            DefineGroup gfDebugGroup = new DefineGroup("GF 调试");
            m_DefineGroups.Add(gfDebugGroup);
            gfDebugGroup.Defines.Add(new DefineItem("Debug", "GF_DEBUG"));

            DefineGroup gfEditorLogGroup = new DefineGroup("GF Log(编辑器)");
            m_DefineGroups.Add(gfEditorLogGroup);
            gfEditorLogGroup.Defines.Add(new DefineItem("Verbose", MDebug.LOG_VERBOSE_EDITOR_CONDITIONAL));
            gfEditorLogGroup.Defines.Add(new DefineItem("Log", MDebug.LOG_EDITOR_CONDITIONAL));
            gfEditorLogGroup.Defines.Add(new DefineItem("Warning", MDebug.LOG_WARNING_EDITOR_CONDITIONAL));
            gfEditorLogGroup.Defines.Add(new DefineItem("Error", MDebug.LOG_ERROR_EDITOR_CONDITIONAL));
            gfEditorLogGroup.Defines.Add(new DefineItem("Assert", MDebug.LOG_ASSERT_EDITOR_CONDITIONAL));

            DefineGroup gfBuiltLogGroup = new DefineGroup("GF Log(打包后)");
            m_DefineGroups.Add(gfBuiltLogGroup);
            gfBuiltLogGroup.Defines.Add(new DefineItem("Verbose", MDebug.LOG_VERBOSE_BUILT_CONDITIONAL));
            gfBuiltLogGroup.Defines.Add(new DefineItem("Log", MDebug.LOG_BUILT_CONDITIONAL));
            gfBuiltLogGroup.Defines.Add(new DefineItem("Warning", MDebug.LOG_WARNING_BUILT_CONDITIONAL));
            gfBuiltLogGroup.Defines.Add(new DefineItem("Error", MDebug.LOG_ERROR_BUILT_CONDITIONAL));
            gfBuiltLogGroup.Defines.Add(new DefineItem("Assert", MDebug.LOG_ASSERT_BUILT_CONDITIONAL));

            RefreshDefine();
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("应用"))
            {
                ApplyDefine();
                RefreshDefine();
            }
            if (GUILayout.Button("刷新"))
            {
                RefreshDefine();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            m_DefineGroupFoldout.Set(EditorGUILayout.Foldout(m_DefineGroupFoldout, "组"));
            EditorGUILayout.EndHorizontal();
            if (m_DefineGroupFoldout)
            {
                EditorGUI.indentLevel++;
                for (int iGroup = 0; iGroup < m_DefineGroups.Count; iGroup++)
                {
                    DefineGroup iterGroup = m_DefineGroups[iGroup];
                    EditorGUILayout.BeginHorizontal();
                    iterGroup.Foldout.Set(EditorGUILayout.Foldout(iterGroup.Foldout, iterGroup.Name));
                    if (GUILayout.Button("开启所有", GUILayout.Width(96)))
                    {
                        for (int iDefine = 0; iDefine < iterGroup.Defines.Count; iDefine++)
                        {
                            iterGroup.Defines[iDefine].Enable = true;
                        }
                    }
                    if (GUILayout.Button("关闭所有", GUILayout.Width(96)))
                    {
                        for (int iDefine = 0; iDefine < iterGroup.Defines.Count; iDefine++)
                        {
                            iterGroup.Defines[iDefine].Enable = false;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    if (!iterGroup.Foldout)
                    {
                        continue;
                    }

                    EditorGUI.indentLevel++;
                    for (int iDefine = 0; iDefine < iterGroup.Defines.Count; iDefine++)
                    {
                        DefineItem iterDefine = iterGroup.Defines[iDefine];
                        iterDefine.Enable = EditorGUILayout.ToggleLeft(EditorGUIUtility.TrTextContent(iterDefine.Name
                                , iterDefine.Tooltip)
                            , iterDefine.Enable);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            m_OtherFoldout.Set(EditorGUILayout.Foldout(m_OtherFoldout, "其他"));
            EditorGUILayout.EndHorizontal();
            if (m_OtherFoldout)
            {
                EditorGUI.indentLevel++;
                m_OtherDefines = EditorGUILayout.TextArea(m_OtherDefines);
                EditorGUI.indentLevel--;
            }
        }

        private void ApplyDefine()
        {
            m_Defines.Clear();

            for (int iGroup = 0; iGroup < m_DefineGroups.Count; iGroup++)
            {
                DefineGroup iterGroup = m_DefineGroups[iGroup];
                for (int iDefine = 0; iDefine < iterGroup.Defines.Count; iDefine++)
                {
                    DefineItem iterDefine = iterGroup.Defines[iDefine];
                    if (iterDefine.Enable)
                    {
                        m_Defines.Add(iterDefine.Define);
                    }
                }
            }

            string[] otherDefines = m_OtherDefines.Split(new char[] { '\n' }
                , StringSplitOptions.RemoveEmptyEntries);
            for (int iDefine = 0; iDefine < otherDefines.Length; iDefine++)
            {
                m_Defines.Add(otherDefines[iDefine]);
            }
            ScriptingDefineSymbolsUtility.SetScriptingDefineSymbols(m_Defines.ToList());
        }

        private void RefreshDefine()
        {
            ScriptingDefineSymbolsUtility.s_TemporarySymbols.Clear();
            ScriptingDefineSymbolsUtility.GetScriptingDefineSymbols(ScriptingDefineSymbolsUtility.s_TemporarySymbols);
            m_Defines = new HashSet<string>(ScriptingDefineSymbolsUtility.s_TemporarySymbols);

            HashSet<string> otherDefines = new HashSet<string>(m_Defines);

            for (int iGroup = 0; iGroup < m_DefineGroups.Count; iGroup++)
            {
                DefineGroup iterGroup = m_DefineGroups[iGroup];
                for (int iDefine = 0; iDefine < iterGroup.Defines.Count; iDefine++)
                {
                    DefineItem iterDefine = iterGroup.Defines[iDefine];
                    iterDefine.Enable = m_Defines.Contains(iterDefine.Define);
                    otherDefines.Remove(iterDefine.Define);
                }
            }

            m_OtherDefines = string.Join("\n", otherDefines);
        }

        private class DefineGroup
        {
            public string Name;
            public List<DefineItem> Defines = new List<DefineItem>();
            public PrefsValue<bool> Foldout;

            public DefineGroup(string name)
            {
                Name = name;
                Foldout = new PrefsValue<bool>("ScriptingDefineSymbolSettingProvider DefineGroup " + name);
            }
        }
                 
        private class DefineItem
        {
            public string Name;
            public string Define;
            public string Tooltip;

            public bool Enable;

            public DefineItem(string name, string define)
                : this(name, define, define)
            {
            }

            public DefineItem(string name, string define, string tooltip)
            {
                Name = name;
                Define = define;
                Tooltip = tooltip;
            }
        }
    }
}