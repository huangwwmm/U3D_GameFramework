using GFEditor.Common.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Asset.Build
{
    public class BuildSettingProvider : SettingsProvider
    {
        private string m_FormatedBuildOutput;
        private string m_FormatedBundleBuildsPath;
        private string m_FormatedAssetInfosPath;
        private string m_FormatedAssetKeyEnumFilePath;
        private string m_FormatedBundleInfoPath;

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new BuildSettingProvider("GF/AssetBundle"
                , SettingsScope.Project
                , new HashSet<string>(new[] { "AssetBundle" }));
        }

        public BuildSettingProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
            m_FormatedBuildOutput = BuildSetting.GetInstance().GetFormatedBuildOutput();
            m_FormatedBundleBuildsPath = BuildSetting.GetInstance().GetFormatedBundleBuildsPath();
            m_FormatedAssetInfosPath = BuildSetting.GetInstance().GetFormateAssetInfosPath();
            m_FormatedBundleInfoPath = BuildSetting.GetInstance().GetFormatedBundleInfoPath();
            m_FormatedAssetKeyEnumFilePath = BuildSetting.GetInstance().GetFormateAssetKeyEnumFilePath();
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            BuildSetting setting = BuildSetting.GetInstance();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("保存"))
            {
                BuildSetting.GetInstance().Save();
            }
            if (GUILayout.Button("清除BundleName"))
            {
                Builder.ClearAllBundleName();
            }
            if (GUILayout.Button("打包"))
            {
                BuildSetting.GetInstance().Save();
                Builder.Build();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("打包目录和BundleName路径支持Format: \n"
                    + "\t{Application.dataPath}\n"
                    + "\t{BuildTarget}"
                , MessageType.Info);

            if (EGLUtility.Folder(out setting.BuildOutput
                , "打包目录"
                , setting.BuildOutput))
            {
                m_FormatedBuildOutput = BuildSetting.GetInstance().GetFormatedBuildOutput();
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("打包目录", m_FormatedBuildOutput);
            if (GUILayout.Button("打开", GUILayout.Width(36)))
            {
                EditorUtility.RevealInFinder(m_FormatedBuildOutput);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (EGLUtility.Folder(out setting.BundleBuildsPath
                , "BundleName路径"
                , setting.BundleBuildsPath))
            {
                m_FormatedBundleBuildsPath = BuildSetting.GetInstance().GetFormatedBundleBuildsPath();
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("BundleName路径", m_FormatedBundleBuildsPath);
            if (GUILayout.Button("打开", GUILayout.Width(36)))
            {
                EditorUtility.RevealInFinder(m_FormatedBundleBuildsPath);
                EditorUtility.OpenWithDefaultApp(m_FormatedBundleBuildsPath);
            }
            EditorGUILayout.EndHorizontal();
            if (EGLUtility.Folder(out setting.BundleInfoPath
                , "BundleInfo路径"
                , setting.BundleInfoPath))
            {
                m_FormatedBundleInfoPath = BuildSetting.GetInstance().GetFormatedBundleInfoPath();
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("BundleInfo路径", m_FormatedBundleInfoPath);
            if (GUILayout.Button("打开", GUILayout.Width(36)))
            {
                EditorUtility.RevealInFinder(m_FormatedBundleInfoPath);
                EditorUtility.OpenWithDefaultApp(m_FormatedBundleInfoPath);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("开启这个选项可以加快打包速度\n"
                    + "如果本地有之前缓存的BundleName文件就不在打包前执行打包规则\n"
                    + "当需要打包的资源有增加或移除，则不应开启这个选项"
                , MessageType.Info);
            setting.UseCachedBuild = EditorGUILayout.Toggle("使用缓存的BundleName", setting.UseCachedBuild);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("关闭这个选项可以加快打包速度\n"
                    + "但是有些插件和调试工具必须设置BundleName才能使用\n\n"
                    + "例如AssetBundleBrowser\n"
                        + "可以通过这个工具设置BundleName，但是不勾选打包AB的选项。然后通过AssetBundleBrowser打包"
                , MessageType.Info);
            setting.ResetBundleName = EditorGUILayout.Toggle("设置BundleName", setting.ResetBundleName);
            EditorGUILayout.Space();

            if (EGLUtility.Folder(out setting.AssetInfosPath
                , "AssetInfos路径"
                , setting.AssetInfosPath))
            {
                m_FormatedAssetInfosPath = BuildSetting.GetInstance().GetFormateAssetInfosPath();
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AssetInfos路径", m_FormatedAssetInfosPath);
            if (GUILayout.Button("打开", GUILayout.Width(36)))
            {
                EditorUtility.RevealInFinder(m_FormatedAssetInfosPath);
                EditorUtility.OpenWithDefaultApp(m_FormatedAssetInfosPath);
            }
            EditorGUILayout.EndHorizontal();
            if (EGLUtility.Folder(out setting.AssetKeyEnumFilePath
                , "AssetKeyEnum路径"
                , setting.AssetKeyEnumFilePath))
            {
                m_FormatedAssetKeyEnumFilePath = BuildSetting.GetInstance().GetFormateAssetKeyEnumFilePath();
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AssetKeyEnum路径", m_FormatedAssetKeyEnumFilePath);
            if (GUILayout.Button("打开", GUILayout.Width(36)))
            {
                EditorUtility.RevealInFinder(m_FormatedAssetKeyEnumFilePath);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            setting.BuildAssetBundleOptions = (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField("打包选项", setting.BuildAssetBundleOptions);
            setting.BuildAssetBuild = EditorGUILayout.Toggle("打包AB", setting.BuildAssetBuild);
            setting.DeleteOutputBeforeBuild = EditorGUILayout.Toggle("打包前删除上次的AB", setting.DeleteOutputBeforeBuild);
        }
    }
}