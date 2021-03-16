using GF.Common.Debug;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GFEditor.ShaderTools.PreprocessShaders
{
    public static class PreprocessShadersUtility
    {
        internal static List<IMPreprocessShaders> s_PreprocessShaders = new List<IMPreprocessShaders>();

        static PreprocessShadersUtility()
        {
            MDebug.Log("Shader", $"PreprocessShadersUtility Initialize");
            s_PreprocessShaders = new List<IMPreprocessShaders>();
        }

        public static void OnAfter()
        {
            BeforePreprocessShaders.s_Handled = false;

            MDebug.Log("Shader", $"AfterPreprocessShaders");

            for (int iPreprocessShaders = 0; iPreprocessShaders < s_PreprocessShaders.Count; iPreprocessShaders++)
            {
                IMPreprocessShaders iterPreprocessShaders = s_PreprocessShaders[iPreprocessShaders];
                try
                {
                    iterPreprocessShaders.OnAfter();
                }
                catch (Exception e)
                {
                    MDebug.LogError("Shader", $"处理Shader({iterPreprocessShaders.GetType().FullName}).OnAfter失败\n{e.ToString()}");
                }
            }
        }
    }
}