using GF.Core;
using GF.Core.Event;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua.LuaDLL;

namespace GF.XLuaFramework.Lib
{
    public class GFEventCenterLib
    {
        private static readonly lua_CSFunction m_RegistEventFunction = RegistEvent;
        private static readonly lua_CSFunction m_UnregistEventFunction = UnregistEvent;

        public static void OpenLib(IntPtr luaState, string libName)
        {
            LibUtility.BeginCreateLib(luaState, libName);
            LibUtility.RegistFunction(luaState, libName, "registEvent", m_RegistEventFunction);
            LibUtility.RegistFunction(luaState, libName, "unregistEvent", m_UnregistEventFunction);
            LibUtility.EndCreateLib(luaState, libName);
        }

        private static int RegistEvent(IntPtr luaState)
        {
            int top = Lua.lua_gettop(luaState);

            if (top != 1)
            {
                return Lua.luaL_error(luaState, "top != 1");
            }

            int eventId = (int)Lua.lua_toint64(luaState, 1);
            Kernel.EventCenter.AddListen(eventId, OnEvent);

            return 1;
        }

        private static int UnregistEvent(IntPtr luaState)
        {
            int top = Lua.lua_gettop(luaState);

            if (top != 1)
            {
                return Lua.luaL_error(luaState, "top != 1");
            }

            int eventId = (int)Lua.lua_toint64(luaState, 1);
            Kernel.EventCenter.RemoveListen(eventId, OnEvent);

            return 1;
        }


        private static void OnEvent(int eventID, bool isImmediately, IUserData userData)
        {
        }
    }
}