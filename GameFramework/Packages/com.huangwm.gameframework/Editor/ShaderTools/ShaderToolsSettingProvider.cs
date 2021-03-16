using GF.Common.Data;
using GF.Common.Debug;
using GF.Common.Utility;
using GFEditor.Common.Utility;
using GFEditor.ShaderTools.PreprocessShaders;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GFEditor.ShaderTools
{
    public class ShaderToolsSettingProvider : SettingsProvider
    {
        private PrefsValue<bool> m_PreprocessShadersFoldout;
        private List<PreprocessShadersForGUI> m_PreprocessShadersForGUIs;

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new ShaderToolsSettingProvider("GF/Shader Tools"
                , SettingsScope.Project
                , new HashSet<string>(new[] { "GF", "Shader" }));
        }

        public ShaderToolsSettingProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
            m_PreprocessShadersFoldout = new PrefsValue<bool>("FolderLinkSettingProvider m_PreprocessShadersFoldout");
            m_PreprocessShadersForGUIs = new List<PreprocessShadersForGUI>();
            LoadPreprocessShaders();
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            if (GUILayout.Button("保存"))
            {
                SavePreprocessShaders();
                ShaderToolsSetting.GetInstance().Save();
                LoadPreprocessShaders();
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            m_PreprocessShadersFoldout.Set(EditorGUILayout.Foldout(m_PreprocessShadersFoldout, "打包时Shader处理"));
            EditorGUILayout.EndHorizontal();
            if (m_PreprocessShadersFoldout)
            {
                EditorGUI.indentLevel++;
                OnGUI_PreprocessShaders();
                EditorGUI.indentLevel--;
            }
        }

        private void LoadPreprocessShaders()
        {
            m_PreprocessShadersForGUIs.Clear();
            List<PreprocessShadersSetting> preprocessShadersSettings = ShaderToolsSetting.GetInstance().PreprocessShadersSettings;
            for (int iPreprocessShaders = 0; iPreprocessShaders < preprocessShadersSettings.Count; iPreprocessShaders++)
            {
                PreprocessShadersSetting iterPreprocessShadersSetting = preprocessShadersSettings[iPreprocessShaders];
                try
                {
                    PreprocessShadersForGUI preprocessShadersForGUI = new PreprocessShadersForGUI();
                    preprocessShadersForGUI.Script = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(iterPreprocessShadersSetting.MonoScriptGUID)) as MonoScript;
                    if (preprocessShadersForGUI.Script == null)
                    {
                        continue;
                    }

                    Type baseType = preprocessShadersForGUI.Script.GetClass();
                    while (true)
                    {
                        baseType = baseType.BaseType;
                        if (baseType.IsGenericType)
                        {
                            break;
                        }
                    }
                    Type settingType = baseType.GenericTypeArguments[0];
                    preprocessShadersForGUI.Setting = JsonUtility.FromJson(iterPreprocessShadersSetting.SettingJson
                        , settingType) as BasePreprocessShadersSetting;
                    if (preprocessShadersForGUI.Setting == null)
                    {
                        continue;
                    }

                    preprocessShadersForGUI.Setting.OnLoadByGUI();

                    m_PreprocessShadersForGUIs.Add(preprocessShadersForGUI);
                }
                catch (Exception e)
                {
                    MDebug.LogWarning("Shader", $"加载Shader处理失败\n{e.ToString()}");
                }
            }
        }

        private void SavePreprocessShaders()
        {
            ShaderToolsSetting.GetInstance().PreprocessShadersSettings.Clear();
            for (int iPreprocessShaders = 0; iPreprocessShaders < m_PreprocessShadersForGUIs.Count; iPreprocessShaders++)
            {
                PreprocessShadersForGUI iterPreprocessShadersForGUI = m_PreprocessShadersForGUIs[iPreprocessShaders];
                if (iterPreprocessShadersForGUI.Script == null
                    || iterPreprocessShadersForGUI.Setting == null)
                {
                    continue;
                }

                try
                {
                    iterPreprocessShadersForGUI.Setting.OnSaveByGUI();

                    PreprocessShadersSetting preprocessShadersSetting = new PreprocessShadersSetting();
                    preprocessShadersSetting.MonoScriptGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(iterPreprocessShadersForGUI.Script));
                    preprocessShadersSetting.MonoScriptTypeFullName = iterPreprocessShadersForGUI.Script.GetClass().FullName;
                    preprocessShadersSetting.SettingJson = JsonUtility.ToJson(iterPreprocessShadersForGUI.Setting);
                    ShaderToolsSetting.GetInstance().PreprocessShadersSettings.Add(preprocessShadersSetting);
                }
                catch (Exception e)
                {
                    MDebug.LogWarning("Shader", $"保存Shader处理失败\n{e.ToString()}");
                }
            }
        }

        private void OnGUI_PreprocessShaders()
        {
            ShaderToolsSetting setting = ShaderToolsSetting.GetInstance();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加Shader处理"))
            {
                m_PreprocessShadersForGUIs.Add(new PreprocessShadersForGUI());
            }
            if (GUILayout.Button("打包后"))
            {
                PreprocessShadersUtility.OnAfter();
            }
            EditorGUILayout.EndHorizontal();

            for (int iPreprocessShaders = 0; iPreprocessShaders < m_PreprocessShadersForGUIs.Count; iPreprocessShaders++)
            {
                PreprocessShadersForGUI iterPreprocessShadersForGUI = m_PreprocessShadersForGUIs[iPreprocessShaders];
                EditorGUILayout.BeginHorizontal();
                if (EGLUtility.ObjectField(out iterPreprocessShadersForGUI.Script, "处理脚本", iterPreprocessShadersForGUI.Script, true))
                {
                    if (iterPreprocessShadersForGUI.Script != null)
                    {
                        TryGenerateSetting(iPreprocessShaders, iterPreprocessShadersForGUI);
                    }
                }
                if (GUILayout.Button("X", GUILayout.Width(32)))
                {
                    m_PreprocessShadersForGUIs.RemoveAt(iPreprocessShaders);
                    iPreprocessShaders--;
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                if (iterPreprocessShadersForGUI.Setting != null)
                {
                    iterPreprocessShadersForGUI.Setting.OnGUI();
                }
                EditorGUILayout.Space();
            }
        }

        private void TryGenerateSetting(int preprocessShadersIndex
          , PreprocessShadersForGUI preprocessShadersForGUI)
        {
            for (int iPreprocessShaders = 0; iPreprocessShaders < m_PreprocessShadersForGUIs.Count; iPreprocessShaders++)
            {
                if (iPreprocessShaders == preprocessShadersIndex)
                {
                    continue;
                }
                PreprocessShadersForGUI iterPreprocessShadersForGUI = m_PreprocessShadersForGUIs[iPreprocessShaders];
                if (iterPreprocessShadersForGUI.Script == preprocessShadersForGUI.Script)
                {
                    MDebug.LogWarning("Shader", "不能选择两个相同的脚本");
                    preprocessShadersForGUI.Script = null;
                    return;
                }
            }

            Type type = preprocessShadersForGUI.Script.GetClass();
            if (type.IsAbstract)
            {
                MDebug.LogWarning("Shader", "不能是Abstract类");
                preprocessShadersForGUI.Script = null;
                return;
            }
            try
            {
                Type baseType = type;
                while (true)
                {
                    baseType = baseType.BaseType;
                    if (baseType.IsGenericType)
                    {
                        break;
                    }
                }
                Type settingType = baseType.GenericTypeArguments[0];
                object preprocessShadersSetting = settingType.Assembly.CreateInstance(settingType.FullName);
                preprocessShadersForGUI.Setting = preprocessShadersSetting as BasePreprocessShadersSetting;
            }
            catch (Exception e)
            {
                MDebug.LogError("Shader", "生成Setting失败\n" + e.ToString());
                preprocessShadersForGUI.Script = null;
            }
        }


        private class PreprocessShadersForGUI
        {
            public MonoScript Script;
            public BasePreprocessShadersSetting Setting;
        }
    }
}