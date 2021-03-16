using GF.Common.Debug;
using System;
using XLua;
using XLua.LuaDLL;

namespace GF.XLuaFramework.Lib
{
    public class GFLogInternalLib
    {
        private static readonly lua_CSFunction m_LogFunction = Log;
        private static readonly lua_CSFunction m_WarningFunction = Warning;
        private static readonly lua_CSFunction m_ErrorFunction = Error;
        private static readonly lua_CSFunction m_VerboseFunction = Verbose;

        public static void OpenLib(IntPtr luaState, string libName)
        {
            LibUtility.BeginCreateLib(luaState, libName);
            LibUtility.RegistFunction(luaState, libName, "log", m_LogFunction);
            LibUtility.RegistFunction(luaState, libName, "warning", m_WarningFunction);
            LibUtility.RegistFunction(luaState, libName, "error", m_ErrorFunction);
            LibUtility.RegistFunction(luaState, libName, "verbose", m_VerboseFunction);
            LibUtility.EndCreateLib(luaState, libName);
        }

        [MonoPInvokeCallback(typeof(lua_CSFunction))]
        private static int Log(IntPtr luaState)
        {
            int success = GetLog(luaState, out string tag, out string message);

            if (success == 1)
            {
                MDebug.Log(tag, message);
            }

            return success;
        }

        [MonoPInvokeCallback(typeof(lua_CSFunction))]
        private static int Warning(IntPtr luaState)
        {
            int success = GetLog(luaState, out string tag, out string message);

            if (success == 1)
            {
                MDebug.LogWarning(tag, message);
            }

            return success;
        }

        [MonoPInvokeCallback(typeof(lua_CSFunction))]
        private static int Error(IntPtr luaState)
        {
            int success = GetLog(luaState, out string tag, out string message);

            if (success == 1)
            {
                MDebug.LogError(tag, message);
            }

            return success;
        }

        [MonoPInvokeCallback(typeof(lua_CSFunction))]
        private static int Verbose(IntPtr luaState)
        {
            int success = GetLog(luaState, out string tag, out string message);

            if (success == 1)
            {
                MDebug.LogVerbose(tag, message);
            }

            return success;
        }

        private static int GetLog(IntPtr luaState, out string tag, out string message)
        {
            int top = Lua.lua_gettop(luaState);

            if (top != 2)
            {
                tag = null;
                message = null;
                return Lua.luaL_error(luaState, "top != 2");
            }

            tag = Lua.lua_tostring(luaState, 1);
            message = Lua.lua_tostring(luaState, 2);

            return 1;
        }
    }
}