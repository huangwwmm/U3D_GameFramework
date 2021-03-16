using System;

namespace GF.Core.Lua
{
    public struct LuaLibItem
    {
        public string Name;
        /// <summary>
        /// 如果用的是XLua，Initier的类型是<see cref="LuaDLL.lua_CSFunction"/>
        /// </summary>
        public object Initer;
    }
}