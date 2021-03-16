using GF.Common;
using UnityEditor;
using UnityEditor.Build.Player;

namespace GFEditor.Common.Utility
{
    public static class CompilePlayerScriptsUtility
    {
        /// <summary>
        /// 有多个地方会用到TypeDB，所以Cache在这里
        /// </summary>
        public static TypeDB s_CurrentTargetTypeDBCache;

        [CTMenuItem("打包/检查代码/当前平台")]
        public static void CheckCompilePlayerScripts_CurrentTarget()
        {
            CheckCompilePlayerScripts(EditorUserBuildSettings.activeBuildTarget, ScriptCompilationOptions.None, true);
        }

        [CTMenuItem("打包/检查代码/Win64")]
        public static void CheckCompilePlayerScripts_Win64()
        {
            CheckCompilePlayerScripts(BuildTarget.StandaloneWindows64, ScriptCompilationOptions.None, true);
        }

        [CTMenuItem("打包/检查代码/Android")]
        public static void CheckCompilePlayerScripts_Android()
        {
            CheckCompilePlayerScripts(BuildTarget.Android, ScriptCompilationOptions.None, true);
        }

        [CTMenuItem("打包/检查代码/iOS")]
        public static void CheckCompilePlayerScripts_iOS()
        {
            CheckCompilePlayerScripts(BuildTarget.iOS, ScriptCompilationOptions.None, true);
        }

        [CTMenuItem("打包/检查代码/XboxOne")]
        public static void CheckCompilePlayerScripts_XboxOne()
        {
            CheckCompilePlayerScripts(BuildTarget.XboxOne, ScriptCompilationOptions.None, true);
        }

        [CTMenuItem("打包/检查代码/PS4")]
        public static void CheckCompilePlayerScripts_PS4()
        {
            CheckCompilePlayerScripts(BuildTarget.PS4, ScriptCompilationOptions.None, true);
        }

        /// <summary>
        /// 检查打包代码是否成功，会把代码打包到临时目录
        /// </summary>
        /// <returns>是否成功</returns>
        public static bool CheckCompilePlayerScripts(BuildTarget buildTarget, ScriptCompilationOptions options, bool displayUI)
        {
            ScriptCompilationSettings settings = new ScriptCompilationSettings();
            settings.target = buildTarget;
            settings.group = BuildPipeline.GetBuildTargetGroup(buildTarget);
            settings.options = options;
            return CompilePlayerScripts(settings, GetTempFolder(), displayUI, out ScriptCompilationResult result);
        }

        public static bool CompilePlayerScripts(BuildTarget buildTarget, ScriptCompilationOptions options, string outputFolder, bool displayUI, out ScriptCompilationResult result)
        {
            ScriptCompilationSettings settings = new ScriptCompilationSettings();
            settings.target = buildTarget;
            settings.group = BuildPipeline.GetBuildTargetGroup(buildTarget);
            settings.options = options;
            return CompilePlayerScripts(settings, outputFolder, displayUI, out result);
        }

        /// <returns>是否成功</returns>
        public static bool CompilePlayerScripts(ScriptCompilationSettings settings, string outputFolder, bool displayUI, out ScriptCompilationResult result)
        {
            if (displayUI)
            {
                EditorUtility.DisplayProgressBar($"Compile Player Scripts ({settings.target})", "Do not operate the computer", 0);
            }
            result = PlayerBuildInterface.CompilePlayerScripts(settings, outputFolder);
            EditorUtility.ClearProgressBar();

            if (result.assemblies != null
                && result.assemblies.Count > 0
                && result.typeDB != null)
            {
                if (settings.target == EditorUserBuildSettings.activeBuildTarget)
                {
                    s_CurrentTargetTypeDBCache = result.typeDB;
                }

                if (displayUI)
                {
                    EditorUtility.DisplayDialog("Check Compile Player Scripts"
                        , $"Success\n{settings.target}: {settings.options}"
                        , "OK");
                }
                return true;
            }
            else
            {
                if (displayUI)
                {
                    EditorUtility.DisplayDialog("Check Compile Player Scripts"
                        , $"Failed\n{settings.target}: {settings.options}\nSee Console Window"
                        , "OK");
                }
                return false;
            }
        }

        public static string GetTempFolder()
        {
            return $"{System.IO.Path.GetTempPath()}/Unity_{GUID.Generate()}/";
        }

        /// <summary>
        /// 编译当然当前平台的TypeDB
        /// </summary>
        public static bool GetOrCompileCurrentTargetTypeDB(bool displayUI, out TypeDB typeDB)
        {
            typeDB = null;

            if (s_CurrentTargetTypeDBCache != null)
            {
                typeDB = s_CurrentTargetTypeDBCache;
                return true;
            }

            if (CompilePlayerScripts(EditorUserBuildSettings.activeBuildTarget, ScriptCompilationOptions.DevelopmentBuild, GetTempFolder(), displayUI, out ScriptCompilationResult result))
            {
                s_CurrentTargetTypeDBCache = result.typeDB;
                typeDB = s_CurrentTargetTypeDBCache;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}