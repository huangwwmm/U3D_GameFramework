using System;
using System.Runtime.InteropServices;

namespace GF.XLuaFramework
{
    /// <summary>
    /// Lua的API，由C#侧去调用
    /// </summary>
    public static class CallLuaAPI
    {
        private const string LUADLL = "xlua";

        #region GFLog
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_gfloginternal_swapbuffer();

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_gfloginternal_getsizetlength();

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_gfloginternal_getbuffer();

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_gfloginternal_getbufferlength();
        #endregion
    }
}