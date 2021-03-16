using System;
using UnityEditor;

namespace GFEditor.ShaderTools.PreprocessShaders
{
    [System.Serializable]
    public abstract class BasePreprocessShadersSetting
    {
        public int CallbackOrder;

        public void OnGUI_Base()
        {
            CallbackOrder = EditorGUILayout.IntField("执行顺序", CallbackOrder);
        }

        public abstract void OnGUI();

        public abstract void OnLoadByGUI();
        public abstract void OnSaveByGUI();

    }
}