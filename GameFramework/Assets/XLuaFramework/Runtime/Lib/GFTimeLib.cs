using GF.Common.Debug;
using System;
using XLua;
using XLua.LuaDLL;

namespace GF.XLuaFramework.Lib
{
    public class GFTimeLib
    {
        private static readonly lua_CSFunction m_GetMillisecondsSinceStartupFunction = GetMillisecondsSinceStartup;
        private static readonly lua_CSFunction m_GetRealtimeSinceStartupFunction = GetRealtimeSinceStartup;

        public static void OpenLib(IntPtr luaState, string libName)
        {
            LibUtility.BeginCreateLib(luaState, libName);
            LibUtility.RegistFunction(luaState, libName, "getMillisecondsSinceStartup", m_GetMillisecondsSinceStartupFunction);
            LibUtility.RegistFunction(luaState, libName, "getRealtimeSinceStartup", m_GetRealtimeSinceStartupFunction);
            LibUtility.EndCreateLib(luaState, libName);
        }

        [MonoPInvokeCallback(typeof(lua_CSFunction))]
        private static int GetMillisecondsSinceStartup(IntPtr luaState)
        {
            int top = Lua.lua_gettop(luaState);

            if (top != 0)
            {
                return Lua.luaL_error(luaState, "top != 0");
            }

            Lua.lua_pushint64(luaState, MDebug.GetMillisecondsSinceStartup());
            return 1;
        }

        [MonoPInvokeCallback(typeof(lua_CSFunction))]
        private static int GetRealtimeSinceStartup(IntPtr luaState)
        {
            int top = Lua.lua_gettop(luaState);

            if (top != 0)
            {
                return Lua.luaL_error(luaState, "top != 0");
            }

            Lua.lua_pushnumber(luaState, UnityEngine.Time.realtimeSinceStartup);
            return 1;
        }
    }
}