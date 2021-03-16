using System;
using UnityEngine;

namespace GF.Core.Lua
{
    public interface ILuaManager
    {
        void ExecuteString(string luaScript);
        int GetUsingMemory();
        void GC(bool force);
    }
}