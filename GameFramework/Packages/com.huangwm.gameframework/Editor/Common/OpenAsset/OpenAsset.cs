using GF.Common;
using GF.Common.Debug;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
#if UNITY_2019_1_OR_NEWER
using Unity.CodeEditor;
#endif
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace GFEditor.OpenAsset
{
    public class OpenAsset
    {
        [OnOpenAsset(0)]
#if UNITY_2019_1_OR_NEWER
        private static bool OnOpenAsset(int instanceID, int line, int column)
#else
        private static bool OnOpenAsset(int instanceID, int line)
#endif
        {
            if (!OpenAssetSetting.s_EnableCustomTextEditor)
            {
                return false;
            }

#if !UNITY_2019_1_OR_NEWER
            int column = 0;
#endif

            UnityEngine.Object selected = EditorUtility.InstanceIDToObject(instanceID);
            string assetPath = AssetDatabase.GetAssetPath(selected);
            if (assetPath.EndsWith(".lua"))
            {
               return OpenText(OpenAssetSetting.s_LuaEditor, assetPath, line, column);
            }
            else if (assetPath.EndsWith(".cs"))
            {
               return OpenText(OpenAssetSetting.s_CSharpEditor, assetPath, line, column);
            }
            else if (selected is TextAsset textAsset)
            {
               return OpenText(OpenAssetSetting.s_DefaultEditor, assetPath, line, column);
            }

            return false;
        }

        private static bool OpenText(OpenAssetSetting.TextEditor textEditor
            , string path
            , int line
            , int column)
        {
            if (textEditor.ChangeSetting)
            {
                if (UnityEditorInternal.ScriptEditorUtility.GetExternalScriptEditor() != textEditor.Path)
                {
#if UNITY_2019_1_OR_NEWER
                    CodeEditor.SetExternalScriptEditor(textEditor.Path);
#else
                    UnityEditorInternal.ScriptEditorUtility.SetExternalScriptEditor(textEditor.Path);
#endif
                }
                return false;
            }
            else
            {
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = textEditor.Path,

#if UNITY_2019_1_OR_NEWER
                        Arguments = CodeEditor.ParseArgument(textEditor.Arguments, path, line, column),
#else
                        Arguments = ParseArgument(textEditor.Arguments, path, line, column),
#endif
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = true,
                    }
                };
                return process.Start();
            }
        }

#if !UNITY_2019_1_OR_NEWER
        public static string ParseArgument(string arguments, string path, int line, int column)
        {
            var newArgument = arguments.Replace("$(ProjectPath)", GF.Common.Utility.StringUtility.QuoteForProcessStart(Directory.GetParent(Application.dataPath).FullName));
            newArgument = newArgument.Replace("$(File)", GF.Common.Utility.StringUtility.QuoteForProcessStart(path));
            newArgument = newArgument.Replace("$(Line)", line >= 0 ? line.ToString() : "0");
            newArgument = newArgument.Replace("$(Column)", column >= 0 ? column.ToString() : "0");
            return newArgument;
        }
#endif
    }

}
