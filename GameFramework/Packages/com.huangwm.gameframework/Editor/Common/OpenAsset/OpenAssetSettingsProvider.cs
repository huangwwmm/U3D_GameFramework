using GF.Common.Debug;
using GFEditor.Common.Utility;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GFEditor.OpenAsset
{
    public class OpenAssetSettingsProvider : SettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new OpenAssetSettingsProvider("GF/Open Asset"
                , SettingsScope.User
                , new HashSet<string>(new[] { "GF" }));
        }

        public OpenAssetSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            OpenAssetSetting.s_EnableCustomTextEditor.Set(EGLUtility.Toggle("使用自定义文本编辑器"
                , OpenAssetSetting.s_EnableCustomTextEditor));

            if (OpenAssetSetting.s_EnableCustomTextEditor)
            {
                DoGUI_TextEditor(OpenAssetSetting.s_LuaEditor, "Lua");
                DoGUI_TextEditor(OpenAssetSetting.s_CSharpEditor, "CSharp");
                DoGUI_TextEditor(OpenAssetSetting.s_DefaultEditor, "其他文本");
            }
        }

        private void DoGUI_TextEditor(OpenAssetSetting.TextEditor textEditor, string label)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            textEditor.Foldout.Set(EditorGUILayout.Foldout(textEditor.Foldout, label));
            EditorGUILayout.EndHorizontal();
            if (!textEditor.Foldout)
            {
                return;
            }
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            textEditor.Path.Set(EditorGUILayout.DelayedTextField(label, textEditor.Path));
            if (GUILayout.Button("使用当前编辑器", GUILayout.Width(100)))
            {
                textEditor.Path.Set(UnityEditorInternal.ScriptEditorUtility.GetExternalScriptEditor());
            }
            EditorGUILayout.EndHorizontal();
            textEditor.ChangeSetting.Set(EGLUtility.Toggle("更改Unity设置", textEditor.ChangeSetting));
            if (!textEditor.ChangeSetting)
            {
                textEditor.Arguments.Set(EditorGUILayout.DelayedTextField("启动参数", textEditor.Arguments));
            }
            EditorGUI.indentLevel--;
        }
    }
}