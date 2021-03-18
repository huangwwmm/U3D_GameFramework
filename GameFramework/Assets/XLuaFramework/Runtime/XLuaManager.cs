using GF.Common.Debug;
using GF.Core.Behaviour;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;
using GF.Core.Lua;
using GF.XLuaFramework.Lib;

namespace GF.XLuaFramework
{
    public class XLuaManager : BaseBehaviour, ILuaManager
    {
        private LuaEnv m_LuaEnv;
        private Dictionary<string, byte[]> m_FileToCodes;

        [Core.InitializePackage("XLuaManager", (int)Core.PackageProiority.XLuaManager)]
        internal static IEnumerator InitializePackage(Core.KernelInitializeData initializeData)
        {
            XLuaManager xluaManager = new XLuaManager();
            Core.Kernel.LuaManager = xluaManager;
            return xluaManager.InitializeAsync(initializeData);
        }

        private XLuaManager()
            : base("XLuaManager", (int)BehaviourPriority.LuaManager, BehaviourGroup.Default.ToString())
        {
            SetEnable(false);
        }

        private IEnumerator InitializeAsync(Core.KernelInitializeData initializeData)
        {
            new LuaScriptParser(initializeData.LuaScriptingDefine);
            initializeData.LuaScriptingDefine = null;

#if UNITY_EDITOR
            if (!initializeData.LoadLuaByAssetDatabaseWhenEditor)
#endif
            {
                m_FileToCodes = new Dictionary<string, byte[]>();
                LuaScriptLoader luaScriptLoader = new LuaScriptLoader(initializeData, m_FileToCodes);
                yield return luaScriptLoader;

                LuaScriptParser.s_Instance.Release();
            }

            m_LuaEnv = new LuaEnv();
            m_LuaEnv.AddLoader(CustomLoader);

            #region add libs
            m_LuaEnv.AddBuildin("cjson", OpenLuaLibrary.LoadCJson);

            if (initializeData.LuaLibs != null)
            {
                for (int iLib = 0; iLib < initializeData.LuaLibs.Length; iLib++)
                {
                    LuaLibItem iterLib = initializeData.LuaLibs[iLib];
                    if (iterLib.Initer is XLua.LuaDLL.lua_CSFunction initer)
                    {
                        m_LuaEnv.AddBuildin(iterLib.Name, initer);
                    }
                    else
                    {
                        MDebug.LogError("XLua", $"Lub lib({iterLib.Name}).Initer not is a lua_CSFunction");
                    }
                }
            }
            #endregion

            #region load log
            if (initializeData.LuaEnableHighPerformanceLog)
            {
                m_LuaEnv.AddBuildin("gfloginternal", OpenLuaLibrary.LoadGFLogInternal);
                Execuret_RequireToGlobalVariable("gfloginternal", "gfloginternal");
            }
            else
            {
                GFLogInternalLib.OpenLib(m_LuaEnv.L, "gfloginternal");
            }
            Execuret_RequireToGlobalVariable("gflog", "Common.GFLog");
            #endregion

            GFTimeLib.OpenLib(m_LuaEnv.L, "gftime");

            GFEventCenterLib.OpenLib(m_LuaEnv.L, "gfeventcenter");
            Execuret_Require("Common.GFEventCenter");

            #region start lua
            ExecuteString(string.Format("require('{0}')", initializeData.LuaEnterFile));
            ExecuteString(initializeData.LuaEnterFunction);
            #endregion

            SetEnable(true);
        }

        public LuaEnv GetLuaEnv()
        {
            return m_LuaEnv;
        }

        public byte[] CustomLoader(ref string filepath)
        {
#if UNITY_EDITOR
            if (m_FileToCodes == null)
            {
                string luaRoot = XLuaSetting.GetInstance().ExportedLuaRoot;
                string luaPath = $"{luaRoot}/{filepath.Replace('.', '/')}.lua.txt";
                TextAsset luaTextAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(luaPath) as TextAsset;
                if (luaTextAsset == null)
                {
                    MDebug.LogError("XLua", $"Not found lua script: " + filepath);
                    return null;
                }
                return System.Text.Encoding.UTF8.GetBytes(LuaScriptParser.s_Instance.Parse(luaTextAsset.text));
            }
#endif

            if (m_FileToCodes.TryGetValue(filepath, out byte[] code))
            {
                return code;
            }
            else
            {
                MDebug.LogError("XLua", $"Not found lua script: " + filepath);
                return null;
            }
        }

        public void ExecuteString(string luaScript)
        {
            if (m_LuaEnv != null)
            {
                try
                {
                    MDebug.Log("XLua", $"Begin execute lua string:\n{luaScript}");
                    m_LuaEnv.DoString(luaScript);
                    MDebug.Log("XLua", $"End execute lua string:\n{luaScript}");
                }
                catch (Exception ex)
                {
                    MDebug.LogError("XLua", $"Execute lua string Exception:\n{ex}\n\n{luaScript}");
                }
            }
        }

        public int GetUsingMemory()
        {
            return m_LuaEnv.Memroy;
        }

        public void Execuret_Require(string requireName)
        {
            ExecuteString($"require('{requireName}')");
        }

        public void Execuret_RequireToGlobalVariable(string globalVariableName, string requireName)
        {
            ExecuteString($"{globalVariableName} = require('{requireName}')");
        }

        public void GC(bool force)
        {
            if (force)
            {
                m_LuaEnv.FullGc();
            }
            else
            {
                m_LuaEnv.GC();
            }
        }
    }
}