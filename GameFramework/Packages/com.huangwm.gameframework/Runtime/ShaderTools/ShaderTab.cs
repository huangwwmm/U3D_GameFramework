using GF.Common.Data;
using GF.Common.Debug;
using GF.Common.Utility;
using GF.DebugPanel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GF.ShaderTools
{
    [StartupTab("Shader", false)]
    public class ShaderTab : BaseMonoBehaviourTab
    {
        private List<KeywordItem> m_GlobalKeywords;
        /// <summary>
        /// 每帧刷新<see cref="m_GlobalKeywords"/>
        /// 有性能问题，默认不开
        /// </summary>
        private PrefsValue<bool> m_AutoRefreshKeyword;
        /// <summary>
        /// 只显示Enable的Keyword
        /// </summary>
        private PrefsValue<bool> m_OnlyDisplayEnabledKeyword;


        public ShaderTab()
            : base()
        {
            m_GlobalKeywords = new List<KeywordItem>();

            m_AutoRefreshKeyword = new PrefsValue<bool>("ShaderTab m_AutoRefreshKeyword");
            m_OnlyDisplayEnabledKeyword = new PrefsValue<bool>("ShaderTab m_OnlyDisplayEnabledKeyword");
        }

        ~ShaderTab()
        {
            m_GlobalKeywords = null;
        }

        public override void DoGUI(IGUIDrawer drawer)
        {
            drawer.ImportantLabel("Keywords:");
            drawer.BeginToolbarHorizontal();
            m_AutoRefreshKeyword.Set(drawer.ToolbarToggle(m_AutoRefreshKeyword, "自动刷新"));
            if (!m_AutoRefreshKeyword
                && drawer.ToolbarButton(false, "刷新"))
            {
                RefreshShaderKeywords();
            }
            m_OnlyDisplayEnabledKeyword.Set(drawer.ToolbarToggle(m_OnlyDisplayEnabledKeyword, "只显示已启用"));
            if (drawer.ToolbarButton(false, "Log"))
            {
                LogShaderKeywords();
            }
            drawer.EndHorizontal();

            drawer.BeginToolbarHorizontal();
            float rowWidth = 0;
            bool dirty = false;
            for (int iKeyword = 0; iKeyword < m_GlobalKeywords.Count; iKeyword++)
            {
                KeywordItem iterKeyword = m_GlobalKeywords[iKeyword];
                if (m_OnlyDisplayEnabledKeyword
                    && !iterKeyword.Enable)
                {
                    continue;
                }

                if (drawer.ToolbarButton(iterKeyword.Enable, iterKeyword.Content.text))
                {
                    dirty = true;
                    if (iterKeyword.Enable)
                    {
                        Shader.DisableKeyword(iterKeyword.KeywordName);
                    }
                    else
                    {
                        Shader.EnableKeyword(iterKeyword.KeywordName);
                    }
                }

                drawer.CalcMinMaxWidth_Button(iterKeyword.Content, out float minWidth, out float maxWidth);
                rowWidth += maxWidth;
                if (rowWidth >= drawer.GetPanelWidth() * 0.64f)
                {
                    rowWidth = 0;
                    drawer.EndHorizontal();
                    drawer.BeginToolbarHorizontal();
                }
            }
            drawer.EndHorizontal();

            if (dirty
                || m_AutoRefreshKeyword)
            {
                RefreshShaderKeywords();
            }
        }

        public override void OnEditorUpdate(float time, float deltaTime)
        {
        }

        public override void OnFixedUpdate(float time, float deltaTime)
        {
        }

        public override void OnLateUpdate(float time, float deltaTime)
        {
        }

        public override void OnUpdate(float time, float deltaTime)
        {
        }

        private void LogShaderKeywords()
        {
            RefreshShaderKeywords();
            StringBuilder disableKeywords = new StringBuilder();
            StringBuilder enabledKeywords = new StringBuilder();
            for (int iKeyword = 0; iKeyword < m_GlobalKeywords.Count; iKeyword++)
            {
                KeywordItem iterKeyword = m_GlobalKeywords[iKeyword];
                if (Shader.IsKeywordEnabled(iterKeyword.KeywordName))
                {
                    enabledKeywords.Append(iKeyword).Append(",是,")
                        .Append(iterKeyword.KeywordName).Append(',')
                        .Append(iterKeyword.KeywordType).Append('\n');
                }
                else
                {
                    disableKeywords.Append(iKeyword).Append(",否,")
                        .Append(iterKeyword.KeywordName).Append(',')
                        .Append(iterKeyword.KeywordType).Append('\n');
                }
            }

            string shaderKeywords = $"编号,是否启用,名字,类型\n{enabledKeywords.ToString()}{disableKeywords.ToString()}";
            MDebug.Log("ShaderTools", "ShaderKeywords:\n" + shaderKeywords);

#if UNITY_EDITOR
            string reportFileName = $"{Application.dataPath}/../Temp/ShaderKeywords.csv";
            File.WriteAllText(reportFileName, shaderKeywords);
            EditorUtility.OpenWithDefaultApp(reportFileName);
            EditorUtility.RevealInFinder(reportFileName);
#endif
        }

        private void RefreshShaderKeywords()
        {
            m_GlobalKeywords.Clear();
            for (int iKeyword = 0; iKeyword < UnityEngineReflectionUtility.ShaderKeyword.MAX_SHADER_KEYWORDS; iKeyword++)
            {
                ShaderKeyword iterKeyword = UnityEngineReflectionUtility.ShaderKeyword.NewShaderKeyword(iKeyword);
                string keywordName = ShaderKeyword.GetGlobalKeywordName(iterKeyword);
                if (!ShaderUtility.IsValidAndUsed(iterKeyword, keywordName))
                {
                    continue;
                }

                KeywordItem keywordItem = new KeywordItem();
                keywordItem.KeywordName = keywordName;
                keywordItem.KeywordType = ShaderKeyword.GetGlobalKeywordType(iterKeyword);
                keywordItem.Enable = Shader.IsKeywordEnabled(keywordItem.KeywordName);
                keywordItem.Content = new GUIContent(keywordItem.KeywordName);
                m_GlobalKeywords.Add(keywordItem);
            }
        }

        private struct KeywordItem
        {
            public GUIContent Content;
            public bool Enable;
            public string KeywordName;
            public ShaderKeywordType KeywordType;
        }
    }
}