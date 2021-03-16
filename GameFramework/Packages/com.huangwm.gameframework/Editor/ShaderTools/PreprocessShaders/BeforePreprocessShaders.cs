using GF.Common.Debug;
using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

namespace GFEditor.ShaderTools.PreprocessShaders
{
    public class BeforePreprocessShaders : IPreprocessShaders
    {
        public static bool s_Handled = false;

        public int callbackOrder { get { return int.MaxValue; } }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (s_Handled)
            {
                return;
            }
            s_Handled = true;

            MDebug.Log("Shader", $"BeforePreprocessShaders");

            PreprocessShadersUtility.s_PreprocessShaders.Sort(IMPreprocessShadersComparison);
            for (int iPreprocessShaders = 0; iPreprocessShaders < PreprocessShadersUtility.s_PreprocessShaders.Count; iPreprocessShaders++)
            {
                IMPreprocessShaders iterPreprocessShaders = PreprocessShadersUtility.s_PreprocessShaders[iPreprocessShaders];
                try
                {
                    iterPreprocessShaders.OnBefore();
                }
                catch(Exception e)
                {
                    MDebug.LogError("Shader", $"处理Shader({iterPreprocessShaders.GetType().FullName}).OnBefore失败\n{e.ToString()}");
                }
            }
        }

        private int IMPreprocessShadersComparison(IMPreprocessShaders x, IMPreprocessShaders y)
        {
            return x.callbackOrder - y.callbackOrder;
        }
    }
}