using GF.Common.Data;

namespace GFEditor.OpenAsset
{
    public static class OpenAssetSetting
    {
        /// <summary>
        /// 是否启用自定义文本编辑器
        /// </summary>
        public static PrefsValue<bool> s_EnableCustomTextEditor;
        public static TextEditor s_LuaEditor;
        public static TextEditor s_CSharpEditor;
        public static TextEditor s_DefaultEditor;

        static OpenAssetSetting()
        {
            s_EnableCustomTextEditor = new PrefsValue<bool>("OpenAssetSetting s_EnableCustomTextEditor", false);
            s_LuaEditor = new TextEditor("Lua");
            s_CSharpEditor = new TextEditor("CSharp");
            s_DefaultEditor = new TextEditor("Default");
        }

        public class TextEditor
        {
            public PrefsValue<bool> Foldout;
            public PrefsValue<bool> ChangeSetting;
            public PrefsValue<string> Path;
            public PrefsValue<string> Arguments;
            
            public TextEditor(string name)
            {
                Foldout = new PrefsValue<bool>($"OpenAssetSetting {name} Foldout", false);
                ChangeSetting = new PrefsValue<bool>($"OpenAssetSetting {name} ChangeSetting", false);
                Path = new PrefsValue<string>($"OpenAssetSetting {name} Path", string.Empty);
                Arguments = new PrefsValue<string>($"OpenAssetSetting {name} Arguments", string.Empty);
            }
        }
    }
}