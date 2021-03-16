using GF.Common;
using GF.Common.Debug;
using GF.Core.Lua;
using System.Collections.Generic;
using UnityEngine;

namespace GF.XLuaFramework
{
    [CTCreateAssetMenuItem("XLua/创建LuaScripts", "LuaScripts")]
    public class LuaScripts : ILuaScripts
    {
        [SerializeField]
        private List<string> m_Names = new List<string>();
        [SerializeField]
        private List<TextAsset> m_Scripts = new List<TextAsset>();

        public int GetCount()
        {
            return m_Names.Count;
        }

        public void GetScriptByIndex(int index, out string name, out TextAsset textAsset)
        {
            name = m_Names[index];
            textAsset = m_Scripts[index];
        }

#if UNITY_EDITOR
        public override string AddScript(string name, TextAsset script)
        {
            int startIndex = XLuaSetting.GetInstance().ExportedLuaRoot.Length + 1;
            int length = name.IndexOf('.') - startIndex;
            name = name.Substring(startIndex, length).Replace(@"/",".");
            m_Names.Add(name);
            m_Scripts.Add(script);
            return name;
        }

        public override void Clear()
        {
            m_Names.Clear();
            m_Scripts.Clear();
        }
#endif
    }
}