using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GF.Core.Lua
{
    /// <summary>
    /// 用于收集项目中的Lua脚本
    /// 没用interface是因为interface不能继承ScriptableObject
    /// </summary>
    public abstract class ILuaScripts : ScriptableObject
    {
#if UNITY_EDITOR
        /// <returns>Name</returns>
        public abstract string AddScript(string path, TextAsset script);
        public abstract void Clear();
#endif
    }
}