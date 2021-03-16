using System;
using System.IO;
using GFEditor.Common.Utility;
using UnityEditor;
using UnityEngine;

namespace GFEditor.ShaderTools.PreprocessShaders
{
    [System.Serializable]
    public class LogPreprocessShadersSetting : BasePreprocessShadersSetting
    {
        public string ReportDirectory;

        private string m_ReportDirectoryFormated;

        public override void OnGUI()
        {
            OnGUI_Base();

            EditorGUILayout.HelpBox("可用的Format标记有:\n"
                    + "{Application.dataPath}\n"
                    + "{DateTime.Now}"
                , MessageType.Info);
            if (EGLUtility.Folder(out ReportDirectory, "报告目录", ReportDirectory))
            {
                m_ReportDirectoryFormated = FormatReportDirectory();
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Format报告目录", m_ReportDirectoryFormated);
            if (GUILayout.Button("打开", GUILayout.Width(32)))
            {
                DirectoryInfo directory = new DirectoryInfo(m_ReportDirectoryFormated);
                while (!Directory.Exists(directory.FullName))
                {
                    if (directory.Parent != null)
                    {
                        directory = directory.Parent;
                        break;
                    }
                }
                if (Directory.Exists(directory.FullName))
                {
                    EditorUtility.RevealInFinder(directory.FullName);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public string FormatReportDirectory()
        {
            return ReportDirectory
                .Replace("{Application.dataPath}", Application.dataPath)
                .Replace("{DateTime.Now}", DateTime.Now.ToString("yyyy-M-dd--HH-mm-ss"));
        }

        public override void OnLoadByGUI()
        {
            m_ReportDirectoryFormated = FormatReportDirectory();
        }

        public override void OnSaveByGUI()
        {
        }
    }
}