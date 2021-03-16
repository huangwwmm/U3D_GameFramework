using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;

namespace GFEditor.ShaderTools.PreprocessShaders
{
    public interface IMPreprocessShaders : IPreprocessShaders
    {
        void OnBefore();
        void OnAfter();
    }
}