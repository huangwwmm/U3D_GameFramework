using GF.XLuaFramework;
using GFEditor.Common.Utility;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GFEditor.XLuaFramework
{
    public class XLuaSettingsProvider : SettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new XLuaSettingsProvider("GF/XLua"
                , SettingsScope.Project
                , new HashSet<string>(new[] { "XLua" }));
        }

        public XLuaSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            XLuaSetting setting = XLuaSetting.GetInstance();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("保存"))
            {
                XLuaSetting.GetInstance().Save();
            }
            if (GUILayout.Button("重新导入所有Lua脚本"))
            {
                LuaScriptPostprocessor.ReimportAllLuaScript();
            }
            EditorGUILayout.EndHorizontal();

            setting.ExportedLuaRoot = (EGLUtility.Folder("Lua的导出目录", setting.ExportedLuaRoot));
        }
    }
}