using GF.Common.Debug;
using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

namespace GFEditor.ShaderTools.PreprocessShaders
{
    public abstract class BasePreprocessShaders<TSetting> : IMPreprocessShaders
        where TSetting : BasePreprocessShadersSetting, new()
    {
        public TSetting Setting = new TSetting();

        public int callbackOrder { get { return m_CallbackOrder; } }

        private int m_CallbackOrder;
        private bool m_Enable;

        public BasePreprocessShaders()
        {
            PreprocessShadersUtility.s_PreprocessShaders.Add(this);
            MDebug.Log("Shader", $"s_PreprocessShaders Add({GetType().FullName})");
        }

        public void OnProcessShader(Shader shader
            , ShaderSnippetData snippet
            , IList<ShaderCompilerData> data)
        {
            if (m_Enable)
            {
                OnProcessShaderInternal(shader, snippet, data);
            }
        }

        public virtual void OnBefore()
        {
            m_Enable = false;
            m_CallbackOrder = 0;

            Type type = GetType();
            string typeFullName = type.FullName;
            List<PreprocessShadersSetting> preprocessShadersSettings = ShaderToolsSetting.GetInstance().PreprocessShadersSettings;
            for (int iPreprocessShaders = 0; iPreprocessShaders < preprocessShadersSettings.Count; iPreprocessShaders++)
            {
                PreprocessShadersSetting iterPreprocessShadersSetting = preprocessShadersSettings[iPreprocessShaders];
                if (iterPreprocessShadersSetting.MonoScriptTypeFullName == typeFullName)
                {
                    Setting = JsonUtility.FromJson(iterPreprocessShadersSetting.SettingJson
                        , typeof(TSetting)) as TSetting;

                    m_Enable = true;
                    m_CallbackOrder = Setting.CallbackOrder;
                    MDebug.Log("Shader"
                        , $"启用Shader处理({typeFullName}), 顺序({m_CallbackOrder}), 设置({iterPreprocessShadersSetting.SettingJson})");

                    break;
                }
            }

            if (m_Enable)
            {
                OnBeforeInternal();
            }
        }

        public virtual void OnAfter()
        {
            if (m_Enable)
            {
                m_Enable = false;
                OnAfterInternal();
            }
        }

        protected abstract void OnBeforeInternal();

        protected abstract void OnAfterInternal();

        protected abstract void OnProcessShaderInternal(Shader shader
            , ShaderSnippetData snippet
            , IList<ShaderCompilerData> data);
    }
}