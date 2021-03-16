using GF.Common.Debug;
using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

namespace GFEditor.ShaderTools.PreprocessShaders
{
    public class AfterPreprocessShaders : IPreprocessShaders
    {
        public static bool s_Handled = false;

        public int callbackOrder { get { return int.MinValue; } }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (s_Handled)
            {
                return;
            }
            s_Handled = true;
        }
    }
}