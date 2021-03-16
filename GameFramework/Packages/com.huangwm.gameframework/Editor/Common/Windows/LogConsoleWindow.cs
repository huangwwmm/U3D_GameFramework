using GF.Common.Collection;
using GF.Common.Data;
using GF.Common.Debug;
using GFEditor.Common.Utility;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Debug
{
    public class LogConsoleWindow : EditorWindow, IHasCustomMenu
    {
        private const int NOT_SET_LOGITEM_ID = -1;
        private const string CUSTROM_FILTER_SCRIPT_FUNCTION_INIT = "Init";
        private const string CUSTROM_FILTER_SCRIPT_FUNCTION_FILTER = "Filter";
        private const string LOG_TAG_EXCULDE_FILTER_STARTWITH = "!";

        private static List<LogConsoleWindow> ms_LogConsoleWindows = new List<LogConsoleWindow>();

        private object m_SplitterState;

        private Vector3 m_TopScrollPosition;
        private Vector3 m_BottomScrollPosition;
        /// <summary>
        /// 显示中的Log的索引对应<see cref="LogItem._Id"/>
        /// </summary>
        private List<int> m_FilteredLogIds;
        /// <summary>
        /// <see cref="m_Collapse"/>
        /// 用于判断Log是否相同
        /// </summary>
        private HashSet<string> m_LogItemHashs;
        /// <summary>
        /// <see cref="m_LogTagFilter"/>
        /// </summary>
        private HashSet<string> m_LogTagFilters;
        /// <summary>
        /// <see cref="m_LogTagFilters"/>
        /// </summary>
        private bool m_IsLogTagExcludeFilter;

        /// <summary>
        /// true: 折叠相同的Log
        /// </summary>
        private PrefsValue<bool> m_Collapse;
        /// <summary>
        /// <see cref="LogType"/>的Filter
        /// </summary>
        private PrefsValue<int> m_LogTypeFlagFilter;
        /// <summary>
        /// <see cref="LogItem.Tag"/>的Filters 以空格分隔
        /// </summary>
        private PrefsValue<string> m_LogTagFilter;
        /// <summary>
        /// true：会追踪显示最新的Log
        /// </summary>
        private PrefsValue<bool> m_FocusLastLog;
        /// <summary>
        /// <see cref="LogItem.Text"/>的Filter
        /// </summary>
        private PrefsValue<string> m_LogTextFilter;

        /// <summary>
        /// 当前选中的Log在显示中的索引
        /// <see cref="m_FilteredLogIds"/>
        /// </summary>
        private int m_SelectedLogId;
        /// <summary>
        /// 上一次选中的Log
        /// </summary>
        private int m_LastSelectedLogId;
        /// <summary>
        /// 每行Log的高度
        /// </summary>
        private float m_LogLineHeight;

        /// <summary>
        /// 修改选项是否改变
        /// </summary>
        private bool m_FilterModified;
        /// <summary>
        /// 最后一次筛选的Log的Id
        /// </summary>
        private int m_LastFilterLogId;
        /// <summary>
        /// 用来计算调用栈中的代码链接
        /// </summary>
        private System.Text.StringBuilder m_TextWithHyperlinks;
        /// <summary>
        /// 窗口下方显示的Log
        /// </summary>
        private string m_BottomLogString;

        private GUIStyle m_TopErrorBoxStyle;
        private GUIStyle m_TopWarningBoxStyle;
        private GUIStyle m_TopLogBoxStyle;
        private GUIStyle m_TopSelectedLogBoxStyle;
        private GUIStyle m_BottomScrollviewStyle;
        private GUIStyle m_BottomMessageStyle;

        private int m_WindowId;

        public static void RepaintAllLogConsoleWindow()
        {
            for (int iConsole = 0; iConsole < ms_LogConsoleWindows.Count; iConsole++)
            {
                ms_LogConsoleWindows[iConsole].Repaint();
            }
        }

        private static void OnHyperLinkClicked(object sender, EventArgs e)
        {
            if (UnityEditorReflectionUtility.EditorGUI.Get_HyperlinkInfos(e).TryGetValue("path", out string path))
            {
                EditorUtility.OpenWithDefaultApp(path);
            }
            else if (UnityEditorReflectionUtility.EditorGUI.Get_HyperlinkInfos(e).TryGetValue("directory", out string directory))
            {
                EditorUtility.RevealInFinder(directory);
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Reinitialize Style"), false, ReinitializeStyle);

            menu.AddItem(EditorGUIUtility.TrTextContent("Format SingleLine/Show Timestamp"), LogItems.GetInstance().ShowTimestamp(), LogItems.GetInstance().SwitchShowTimestamp);
            menu.AddItem(EditorGUIUtility.TrTextContent("Format SingleLine/Show FrameCount"), LogItems.GetInstance().ShowFrameCount(), LogItems.GetInstance().SwitchShowFrameCount);
            menu.AddItem(EditorGUIUtility.TrTextContent("Format SingleLine/Show LogId"), LogItems.GetInstance().ShowLogId(), LogItems.GetInstance().SwitchShowLogId);

            menu.AddItem(EditorGUIUtility.TrTextContent("Load From File"), false, LogItems.GetInstance().LoadFromFile);
        }

        public int GetWindowId()
        {
            return m_WindowId;
        }

        protected void OnInspectorUpdate()
        {
            if (LogItems.GetInstance()._GetLogChanged())
            {
                Repaint();
            }
        }

        protected void OnEnable()
        {
            m_WindowId = 0;
            while (true)
            {
                bool has = false;
                for (int iConsole = 0; iConsole < ms_LogConsoleWindows.Count; iConsole++)
                {
                    if (ms_LogConsoleWindows[iConsole].GetWindowId() == m_WindowId)
                    {
                        has = true;
                        break;
                    }
                }

                if (!has)
                {
                    break;
                }

                m_WindowId++;
            }

            ms_LogConsoleWindows.Add(this);

            LogItems.GetInstance();

            titleContent.text = "Console " + m_WindowId;

            m_SplitterState = UnityEditorReflectionUtility.SplitterGUILayout.CreateSplitterState(new float[] { 70, 30 }, new int[] { 32, 32 }, null);

            m_LogItemHashs = new HashSet<string>();
            m_LogTagFilters = new HashSet<string>();
            m_LogTagFilters = new HashSet<string>();
            m_FilteredLogIds = new List<int>();
            m_SelectedLogId = NOT_SET_LOGITEM_ID;
            m_LastSelectedLogId = NOT_SET_LOGITEM_ID;

            m_Collapse = new PrefsValue<bool>("LogConsoleWindow_m_Collapse" + m_WindowId, false);
            m_LogTypeFlagFilter = new PrefsValue<int>("LogConsoleWindow_m_LogTypeFlagFilter" + m_WindowId, int.MaxValue);
            m_LogTagFilter = new PrefsValue<string>("LogConsoleWindow_m_LogTagFilter" + m_WindowId, string.Empty);
            m_FocusLastLog = new PrefsValue<bool>("LogConsoleWindow_m_FocusLastLog" + m_WindowId, false);
            m_LogTextFilter = new PrefsValue<string>("LogConsoleWindow_m_LogTextFilter" + m_WindowId, string.Empty);
            m_FilterModified = true;
            m_LastFilterLogId = 0;
            CaculateLogTags();

            m_TextWithHyperlinks = new System.Text.StringBuilder();
            UnityEditorReflectionUtility.EditorGUI.Remove_HyperLinkClicked(OnHyperLinkClicked);
            UnityEditorReflectionUtility.EditorGUI.Add_HyperLinkClicked(OnHyperLinkClicked);
        }

        protected void OnDisable()
        {
            ms_LogConsoleWindows.Remove(this);

            m_TextWithHyperlinks = null;

            m_LogItemHashs.Clear();
            m_LogItemHashs = null;

            m_LogTagFilters.Clear();
            m_LogTagFilters = null;

            m_FilteredLogIds.Clear();
            m_FilteredLogIds = null;
        }

        protected void OnGUI()
        {
            Event currentEvent = Event.current;

            TryInitializeStyle();

            LogItems.GetInstance()._GetForGUI(out BetterQueue<LogItem> logItems, out int logCount, out int warningLogCount, out int errorLogCount);
            OnGUI_Toolbar(logCount, warningLogCount, errorLogCount);

            UnityEditorReflectionUtility.SplitterGUILayout.BeginVerticalSplit(m_SplitterState);

            FilterLog(logItems);

            #region Top
            int filterLogCount = m_FilteredLogIds.Count;
            int startIndex = 0, endIndex = filterLogCount - 1;
            int maxDisplayLine = Mathf.CeilToInt(position.height / m_LogLineHeight);
            if (maxDisplayLine <= filterLogCount)
            {
                if (m_FocusLastLog)
                {
                    startIndex = Mathf.Clamp(filterLogCount - maxDisplayLine, 0, endIndex);
                }
                else
                {
                    startIndex = Mathf.Clamp(Mathf.FloorToInt(m_TopScrollPosition.y / m_LogLineHeight), 0, endIndex);
                    endIndex = Mathf.Clamp(startIndex + maxDisplayLine, startIndex, endIndex);
                }
            }

            if (startIndex > 0)
            {
                GUILayout.Label("", GUILayout.Height(startIndex * m_LogLineHeight));
            }
            for (int iSelectedLog = startIndex; iSelectedLog <= endIndex; iSelectedLog++)
            {
                int iterLogIndex = LogItems.GetInstance().LogIdToIndex(m_FilteredLogIds[iSelectedLog]);
                if (iterLogIndex < 0 || iterLogIndex >= logItems.Count)
                {
                    continue;
                }
                LogItem iterLog = logItems[iterLogIndex];

                GUIStyle iterStyle;
                if (iterLog._Id == m_SelectedLogId)
                {
                    iterStyle = m_TopSelectedLogBoxStyle;
                }
                else
                {
                    switch (iterLog._LogType)
                    {
                        case LogType.Assert:
                        case LogType.Error:
                        case LogType.Exception:
                            iterStyle = m_TopErrorBoxStyle;
                            break;
                        case LogType.Warning:
                            iterStyle = m_TopWarningBoxStyle;
                            break;
                        case LogType.Log:
                            iterStyle = m_TopLogBoxStyle;
                            break;
                        default:
                            iterStyle = m_TopLogBoxStyle;
                            break;
                    }
                }
                if (GUILayout.Button(iterLog._SingleLineText, iterStyle))
                {
                    m_SelectedLogId = m_SelectedLogId == iterLog._Id
                        ? NOT_SET_LOGITEM_ID
                        : iterLog._Id;
                }
            }
            if (endIndex < filterLogCount - 1)
            {
                GUILayout.Label("", GUILayout.Height((filterLogCount - endIndex - 1) * m_LogLineHeight));
            }
            EditorGUILayout.EndScrollView();
            if (m_FocusLastLog)
            {
                m_TopScrollPosition.y = m_LogLineHeight * filterLogCount;
            }
            #endregion

            #region Top Event Handle
            if (currentEvent.type == EventType.KeyDown
                && m_SelectedLogId != NOT_SET_LOGITEM_ID)
            {
                switch (currentEvent.keyCode)
                {
                    case KeyCode.UpArrow:
                        for (int iLog = m_FilteredLogIds.Count - 1; iLog >= 0; iLog--)
                        {
                            if (m_FilteredLogIds[iLog] < m_SelectedLogId)
                            {
                                m_SelectedLogId = m_FilteredLogIds[iLog];
                                break;
                            }
                        }
                        break;
                    case KeyCode.DownArrow:
                        for (int iLog = 0; iLog < m_FilteredLogIds.Count; iLog++)
                        {
                            if (m_FilteredLogIds[iLog] > m_SelectedLogId)
                            {
                                m_SelectedLogId = m_FilteredLogIds[iLog];
                                break;
                            }
                        }
                        break;
                }
                currentEvent.Use();
            }
            #endregion

            #region Bottom
            m_BottomScrollPosition = EditorGUILayout.BeginScrollView(m_BottomScrollPosition, m_BottomScrollviewStyle);
            int selectedLogIndex = LogItems.GetInstance().LogIdToIndex(m_SelectedLogId);
            if (selectedLogIndex >= 0
                && selectedLogIndex < logItems.Count)
            {
                LogItem selectedLog = logItems[selectedLogIndex];
                if (m_SelectedLogId != m_LastSelectedLogId)
                {
                    m_BottomLogString = string.Format("{0}\n\nStackTrace\n{1}", selectedLog.Text, StacktraceWithHyperlinks(selectedLog.ST));
                }
                EditorGUILayout.SelectableLabel(m_BottomLogString
                    , m_BottomMessageStyle
                    , GUILayout.ExpandWidth(true)
                    , GUILayout.ExpandHeight(true)
                    , GUILayout.MinHeight(m_BottomMessageStyle.CalcHeight(new GUIContent(m_BottomLogString), position.width)));

                if ((currentEvent.type == EventType.ValidateCommand
                        || currentEvent.type == EventType.ExecuteCommand)
                    && currentEvent.commandName == "Copy")
                {
                    if (currentEvent.type == EventType.ExecuteCommand)
                    {
                        // 这里拷贝的时候不需要Hyperlinks格式
                        EditorGUIUtility.systemCopyBuffer = string.Format("{0}\n\nStackTrace\n{1}", selectedLog.Text, selectedLog.ST);
                    }
                    currentEvent.Use();
                }
            }
            m_LastSelectedLogId = m_SelectedLogId;
            EditorGUILayout.EndScrollView();
            #endregion

            UnityEditorReflectionUtility.SplitterGUILayout.EndVerticalSplit();
            LogItems.GetInstance()._ResetLogChanged();
        }

        /// <summary>
        /// Copy from <see cref="UnityEditor.ConsoleWindow.StacktraceWithHyperlinks"/>
        /// </summary>
        /// <param name="stacktraceText"></param>
        /// <returns></returns>
        private string StacktraceWithHyperlinks(string stacktraceText)
        {
            m_TextWithHyperlinks.Clear();
            var lines = stacktraceText.Split(new string[] { "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; ++i)
            {
                string textBeforeFilePath = ") (at ";
                int filePathIndex = lines[i].IndexOf(textBeforeFilePath, StringComparison.Ordinal);
                if (filePathIndex > 0)
                {
                    filePathIndex += textBeforeFilePath.Length;
                    if (lines[i][filePathIndex] != '<') // sometimes no url is given, just an id between <>, we can't do an hyperlink
                    {
                        string filePathPart = lines[i].Substring(filePathIndex);
                        int lineIndex = filePathPart.LastIndexOf(":", StringComparison.Ordinal); // LastIndex because the url can contain ':' ex:"C:"
                        if (lineIndex > 0)
                        {
                            int endLineIndex = filePathPart.LastIndexOf(")", StringComparison.Ordinal); // LastIndex because files or folder in the url can contain ')'
                            if (endLineIndex > 0)
                            {
                                string lineString =
                                    filePathPart.Substring(lineIndex + 1, (endLineIndex) - (lineIndex + 1));
                                string filePath = filePathPart.Substring(0, lineIndex);

                                m_TextWithHyperlinks.Append(lines[i].Substring(0, filePathIndex));
                                m_TextWithHyperlinks.Append("<a href=\"" + filePath + "\"" + " line=\"" + lineString + "\">");
                                m_TextWithHyperlinks.Append(filePath + ":" + lineString);
                                m_TextWithHyperlinks.Append("</a>)\n");

                                continue; // continue to evade the default case
                            }
                        }
                    }
                }
                // default case if no hyperlink : we just write the line
                m_TextWithHyperlinks.Append(lines[i] + "\n");
            }
            // Remove the last \n
            if (m_TextWithHyperlinks.Length > 0) // textWithHyperlinks always ends with \n if it is not empty
                m_TextWithHyperlinks.Remove(m_TextWithHyperlinks.Length - 1, 1);

            return m_TextWithHyperlinks.ToString();
        }

        private void OnGUI_Toolbar(int logCount, int warningLogCount, int errorLogCount)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            // Left
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
            {
                LogItems.GetInstance()._Clear();
                m_FilterModified = true;
            }

            m_FilterModified |= m_FocusLastLog.Set(GUILayout.Toggle(m_FocusLastLog, "Focus", EditorStyles.toolbarButton));

            m_FilterModified |= m_Collapse.Set(GUILayout.Toggle(m_Collapse, "Collapse", EditorStyles.toolbarButton));
            m_FilterModified |= SetFlag(LogType.Log, GUILayout.Toggle(HasFlag(LogType.Log), "L " + logCount, EditorStyles.toolbarButton));
            m_FilterModified |= SetFlag(LogType.Warning, GUILayout.Toggle(HasFlag(LogType.Warning), "W " + warningLogCount, EditorStyles.toolbarButton));
            m_FilterModified |= SetFlag(LogType.Error, GUILayout.Toggle(HasFlag(LogType.Error), "E " + errorLogCount, EditorStyles.toolbarButton));
            GUILayout.FlexibleSpace();

            // Right
            m_FilterModified |= m_LogTagFilter.Set(EditorGUILayout.DelayedTextField(m_LogTagFilter, EditorStyles.toolbarTextField));
            m_FilterModified |= m_LogTextFilter.Set(EditorGUILayout.DelayedTextField(m_LogTextFilter, EditorStyles.toolbarTextField));

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                m_FilterModified = true;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void FilterLog(BetterQueue<LogItem> logItems)
        {
            if (m_FilterModified)
            {
                CaculateLogTags();

                if (m_Collapse)
                {
                    m_LogItemHashs.Clear();
                }

                m_FilteredLogIds.Clear();
            }

            int startLogIndex = m_FilterModified
                ? 0
                : Mathf.Max(0, LogItems.GetInstance().LogIdToIndex(m_LastFilterLogId + 1));
            m_TopScrollPosition = EditorGUILayout.BeginScrollView(m_TopScrollPosition);
            for (int iLog = startLogIndex; iLog < logItems.Count; iLog++)
            {
                LogItem iterLog = logItems[iLog];

                if (!HasFlag(iterLog._LogType))
                {
                    continue;
                }

                if (m_Collapse
                    && !m_LogItemHashs.Add(iterLog._Hash))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(m_LogTagFilter)
                    && (m_IsLogTagExcludeFilter
                        ? m_LogTagFilters.Contains(iterLog.Tag)
                        : !m_LogTagFilters.Contains(iterLog.Tag)))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(m_LogTextFilter)
                    && !iterLog.Text.Contains(m_LogTextFilter)
                    && !iterLog.ST.Contains(m_LogTextFilter))
                {
                    continue;
                }

                m_FilteredLogIds.Add(iterLog._Id);
            }

            m_LastFilterLogId = logItems.Count > 0
                ? logItems[logItems.Count - 1]._Id
                : -1;
            m_FilterModified = false;
        }

        private void ReinitializeStyle()
        {
            m_TopErrorBoxStyle = null;
        }

        private void TryInitializeStyle()
        {
            if (m_TopErrorBoxStyle == null)
            {
                m_TopErrorBoxStyle = new GUIStyle(GUI.skin.label);
                m_TopErrorBoxStyle.richText = true;
                m_TopWarningBoxStyle = new GUIStyle(GUI.skin.label);
                m_TopWarningBoxStyle.richText = true;
                m_TopLogBoxStyle = new GUIStyle(GUI.skin.label);
                m_TopLogBoxStyle.richText = true;
                m_TopSelectedLogBoxStyle = new GUIStyle(GUI.skin.label);
                m_TopSelectedLogBoxStyle.richText = true;
                m_BottomScrollviewStyle = new GUIStyle("CN Box");
                m_BottomMessageStyle = new GUIStyle("CN Message");
                m_BottomMessageStyle.richText = true;

                if (UnityEditorReflectionUtility.InternalEditorUtility.HasPro())
                {
                    m_TopErrorBoxStyle.normal.textColor = Color.red;
                    m_TopWarningBoxStyle.normal.textColor = Color.yellow;
                    m_TopLogBoxStyle.normal.textColor = Color.green;
                    m_TopSelectedLogBoxStyle.normal.textColor = Color.white;
                }
                else
                {
                    m_TopErrorBoxStyle.normal.textColor = Color.red;
                    m_TopWarningBoxStyle.normal.textColor = new Color(130.0f / 255.0f, 75.0f / 255.0f, 0.0f / 255.0f, 255.0f / 255.0f);
                    m_TopLogBoxStyle.normal.textColor = Color.black;
                    m_TopSelectedLogBoxStyle.normal.textColor = Color.blue;
                }
            }

            m_LogLineHeight = m_LogLineHeight > 0
                ? m_LogLineHeight
                : m_TopSelectedLogBoxStyle.lineHeight + m_TopSelectedLogBoxStyle.margin.top + m_TopSelectedLogBoxStyle.margin.bottom;
        }

        private void CaculateLogTags()
        {
            m_LogTagFilters.Clear();
            m_IsLogTagExcludeFilter = m_LogTagFilter.Get().StartsWith(LOG_TAG_EXCULDE_FILTER_STARTWITH);
            string[] tags = m_IsLogTagExcludeFilter
                ? m_LogTagFilter.Get().Substring(LOG_TAG_EXCULDE_FILTER_STARTWITH.Length).Split(' ')
                : m_LogTagFilter.Get().Split(' ');
            for (int iTag = 0; iTag < tags.Length; iTag++)
            {
                string iterTag = tags[iTag];

                m_LogTagFilters.Add(iterTag);
            }
        }

        private bool HasFlag(LogType logType)
        {
            if (logType == LogType.Assert
                || logType == LogType.Exception)
            {
                logType = LogType.Error;
            }

            return (m_LogTypeFlagFilter & (1 << ((int)logType))) > 0;
        }

        /// <returns>IsChanged</returns>
        private bool SetFlag(LogType logType, bool enable)
        {
            if (logType == LogType.Assert
                || logType == LogType.Exception)
            {
                logType = LogType.Error;
            }

            if (enable)
            {
                return m_LogTypeFlagFilter.Set(m_LogTypeFlagFilter | (1 << (int)logType));
            }
            else
            {
                return m_LogTypeFlagFilter.Set(m_LogTypeFlagFilter & ~(1 << (int)logType));
            }
        }
    }
}