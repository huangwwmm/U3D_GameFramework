using GF.Common.Debug;
using System.Runtime.InteropServices;
using XLua;

namespace GF.XLuaFramework
{
    public static class OpenLuaLibrary
    {
        public const string LUADLL = "xlua";

        #region cjson
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaopen_cjson(System.IntPtr L);

        [MonoPInvokeCallback(typeof(XLua.LuaDLL.lua_CSFunction))]
        public static int LoadCJson(System.IntPtr L)
        {
            return luaopen_cjson(L);
        }
        #endregion

        #region gflog
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaopen_gfloginternal(System.IntPtr L);

        [MonoPInvokeCallback(typeof(XLua.LuaDLL.lua_CSFunction))]
        public static int LoadGFLogInternal(System.IntPtr L)
        {
            int success = luaopen_gfloginternal(L);
            if (success == 1)
            {
                new HighPerformanceLog();
            }

            return success;
        }
        #endregion
    }
}