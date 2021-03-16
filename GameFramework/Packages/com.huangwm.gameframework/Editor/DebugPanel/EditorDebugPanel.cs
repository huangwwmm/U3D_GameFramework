using GF.Common;
using GF.Common.Data;
using GF.DebugPanel;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GFEditor.DebugPanel
{
    internal class EditorDebugPanel : EditorWindow, IDebugPanel
    {
        private static bool s_UpdateRepaint = true;

        private List<Tab> m_Tabs;
        private EditorGUIDrawer m_GUIDrawer;
        private Vector3 m_ScrollViewlPosition;

        private PrefsValue<bool> m_EditStyle;

#if UNITY_EDITOR
        [CTMenuItem("调试/Debug Panel")]
        private static void OpenEditorDebugPanel()
        {
            GetWindow<EditorDebugPanel>("Debug Panel", false);
        }
#endif

        public void RegistGUI(string tabName, Action<IGUIDrawer> doGUIAction, bool onlyRuntime)
        {
            if (!TryFindTab(tabName, out Tab tab, out int tabIndex))
            {
                tab = new Tab(tabName, doGUIAction, onlyRuntime);
                m_Tabs.Add(tab);
            }
        }

        public void UnregistGUI(string tabName)
        {
            if (TryFindTab(tabName, out Tab tab, out int tabIndex))
            {
                tab.Release();
                m_Tabs.RemoveAt(tabIndex);
            }
        }

        public void SwitchTab(string tabName)
        {
            if (TryFindTab(tabName, out Tab tab, out int tabIndex))
            {
                tab.Foldout.Set(true);
            }
        }

        protected void OnEnable()
        {
            m_EditStyle = new PrefsValue<bool>("EditorDebugPanel m_EditStyle");

            m_Tabs = new List<Tab>();

            RegistGUI("Debug Panel", DoGUI_DebugPanel, false);

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (DebugPanelInstance._ms_Instance == null
                || !DebugPanelInstance._ms_Instance.Equals(this))
            {
                DebugPanelInstance._ms_Instance = this;
                DebugPanelInstance.RegistStartupTabs();
            }
        }

        protected void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            for (int iTab = 0; iTab < m_Tabs.Count; iTab++)
            {
                m_Tabs[iTab].Release();
            }
            m_Tabs = null;

            // 当Debug窗口被关闭时，就把默认的Debug窗口改成假的Debug窗口
            if (DebugPanelInstance._ms_Instance.Equals(this))
            {
                DebugPanelInstance._SetInstanceToDummy();
            }
        }

        protected void OnGUI()
        {
            if (m_GUIDrawer == null)
            {
                m_GUIDrawer = new EditorGUIDrawer();
            }

            m_GUIDrawer.SetPanelWidth(position.width);

            m_ScrollViewlPosition = EditorGUILayout.BeginScrollView(m_ScrollViewlPosition);
            for (int iTab = 0; iTab < m_Tabs.Count; iTab++)
            {
                Tab iterTab = m_Tabs[iTab];
                OnGUI_Tab(iterTab);
            }
            EditorGUILayout.EndScrollView();
        }

        protected void OnInspectorUpdate()
        {
            if (s_UpdateRepaint)
            {
                Repaint();
            }
        }

        private void OnGUI_Tab(Tab tab)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            tab.Foldout.Set(EditorGUILayout.Foldout(tab.Foldout, tab.TabName, EditorStyles.foldout));
            EditorGUILayout.EndHorizontal();

            if (tab.Foldout
                && tab.DoGUIAction != null)
            {
                tab.DoGUIAction.Invoke(m_GUIDrawer);
            }
        }

        private void DoGUI_DebugPanel(IGUIDrawer drawer)
        {
            drawer.BeginToolbarHorizontal();
            s_UpdateRepaint = drawer.ToolbarToggle(s_UpdateRepaint, "每帧更新");
            m_EditStyle.Set(drawer.ToolbarToggle(m_EditStyle, "编辑主题"));
            drawer.EndHorizontal();

            if (m_EditStyle)
            {
                drawer.ImportantLabel("编辑主题:");
                ((EditorGUIDrawer)drawer).DoGUI();
                drawer.Space();
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange == PlayModeStateChange.ExitingPlayMode)
            {
                for (int iTab = 0; iTab < m_Tabs.Count; iTab++)
                {
                    Tab iterTab = m_Tabs[iTab];
                    if (iterTab.OnlyRuntime)
                    {
                        iterTab.Release();
                        m_Tabs.RemoveAt(iTab);
                        iTab--;
                    }
                }
            }
        }

        private bool TryFindTab(string tabName, out Tab tab, out int tabIndex)
        {
            for (tabIndex = 0; tabIndex < m_Tabs.Count; tabIndex++)
            {
                tab = m_Tabs[tabIndex];
                if (tab.TabName == tabName)
                {
                    return true;
                }
            }

            tab = null;
            return false;
        }

        private class Tab
        {
            public string TabName;
            public Action<IGUIDrawer> DoGUIAction;
            public bool OnlyRuntime;

            public PrefsValue<bool> Foldout;

            public Tab(string tabName, Action<IGUIDrawer> doGUIAction, bool onlyRuntime)
            {
                TabName = tabName;
                DoGUIAction = doGUIAction;
                OnlyRuntime = onlyRuntime;

                Foldout = new PrefsValue<bool>("EditorDebugPanel Tab Foldout " + tabName);
            }

            public void Release()
            {
                DoGUIAction = null;
            }
        }
    }
}