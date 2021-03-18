using GF.Common.Debug;
using GF.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GF.XLuaFramework
{
    public class LuaScriptLoader : IEnumerator
    {
        private object m_Current;

        private bool m_IsLoaded = false;
        private Dictionary<string, byte[]> m_FileToCodes;
        private LuaScripts m_LuaScripts;

        public object Current { get { return m_Current; } }

        public LuaScriptLoader(KernelInitializeData initializeData, Dictionary<string, byte[]> fileToCodes)
        {
            // UNDONE
            //Core.Kernel.AssetManager.LoadAssetAsync(initializeData.LuaScriptsAssetKey, OnLuaScriptLoaded);
            m_FileToCodes = fileToCodes;
        }

        public bool MoveNext()
        {
            if (m_IsLoaded)
            {
                for (int iScript = 0; iScript < m_LuaScripts.GetCount(); iScript++)
                {
                    m_LuaScripts.GetScriptByIndex(iScript, out string name, out TextAsset script);
                    string scriptText = LuaScriptParser.s_Instance.Parse(script.text);
                    m_FileToCodes.Add(name, System.Text.Encoding.UTF8.GetBytes(scriptText));
                }
                // UNDONE
                //Core.Kernel.AssetManager.ReleaseAsset(m_LuaScripts);
            }
            m_LuaScripts = null;

            return !m_IsLoaded;
        }

        public void Reset()
        {
            m_Current = null;
        }

        private void OnLuaScriptLoaded(string key, object obj)
        {
            m_LuaScripts = obj as LuaScripts;
            m_IsLoaded = true;
        }
    }
}