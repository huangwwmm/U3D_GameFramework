#if UNITY_EDITOR
using GF.Common.Debug;
using GFEditor.Common.Data;
using System;
using System.IO;
using UnityEngine;

namespace GF.XLuaFramework
{
    [Serializable]
    public class XLuaSetting : BaseProjectSetting
    {
        public const string SETTING_NAME = "XLuaSetting";

        public const string LUA_SCRIPT_FOLDER = "LuaScript";
        public const string LUA_SCRIPT_SIGN = "/" + LUA_SCRIPT_FOLDER + "/";
        public const string LUA_SOURCE_EXTENSION = ".lua";
        public const string LUA_EXPORTED_EXTENSION = ".txt";

        private static XLuaSetting ms_Instance;

        public string ExportedLuaRoot;

        public static XLuaSetting GetInstance()
        {
            if (ms_Instance == null)
            {
                ms_Instance = Load<XLuaSetting>(SETTING_NAME);
            }
            return ms_Instance;
        }

        protected override string GetSettingName()
        {
            return SETTING_NAME;
        }
    }
}
#endif