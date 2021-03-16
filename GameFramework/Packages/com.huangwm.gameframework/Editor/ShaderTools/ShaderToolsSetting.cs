using GFEditor.Common.Data;
using GFEditor.ShaderTools.PreprocessShaders;
using System;
using System.Collections.Generic;

namespace GFEditor.ShaderTools
{
    [Serializable]
    public class ShaderToolsSetting : BaseProjectSetting
    {
        public const string SETTING_NAME = "ShaderToolsSetting";

        private static ShaderToolsSetting ms_Instance;

        public List<PreprocessShadersSetting> PreprocessShadersSettings = new List<PreprocessShadersSetting>();

        public static ShaderToolsSetting GetInstance()
        {
            if (ms_Instance == null)
            {
                ms_Instance = Load<ShaderToolsSetting>(SETTING_NAME);
            }
            return ms_Instance;
        }

        protected override string GetSettingName()
        {
            return SETTING_NAME;
        }
    }
}