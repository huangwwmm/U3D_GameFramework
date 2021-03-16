using GF.Common.Debug;
using GF.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;
using XLua.LuaDLL;

namespace GF.XLuaFramework.Lib
{
    /// <summary>
    /// 这个类存是为了防止将来lua或者xlua版本更新导致某些API改变
    /// 
    /// 创建一个Lib的流程
    ///     <see cref="BeginCreateLib"/>
    ///     <see cref="RegistFunction"/>
    ///     ...
    ///     <see cref="RegistFunction"/>
    ///     <see cref="EndCreateLib"/>
    /// </summary>
    public static class LibUtility
    {
        private static string ms_CurrntLibName = null;

        public static void BeginCreateLib(IntPtr luaState, string libName)
        {
            MDebug.Assert(ms_CurrntLibName == null
                , "XLua"
                , "ms_CurrntLibName == null");

            ms_CurrntLibName = libName;

            Lua.lua_newtable(luaState);
        }

        public static void RegistFunction(IntPtr luaState, string libName, string functionName, lua_CSFunction function)
        {
            MDebug.Assert(ms_CurrntLibName == libName
                , "XLua"
                , "ms_CurrntLibName == libName");

            Lua.xlua_pushasciistring(luaState, functionName);
            Lua.lua_pushstdcallcfunction(luaState, function);
            Lua.lua_rawset(luaState, -3);
        }

        public static void EndCreateLib(IntPtr luaState, string libName)
        {
            MDebug.Assert(ms_CurrntLibName == libName
               , "XLua"
               , "ms_CurrntLibName == libName");
            ms_CurrntLibName = null;

            if (0 != Lua.xlua_setglobal(luaState, libName))
            {
                throw new Exception("call xlua_setglobal fail!");
            }
        }
    }
}