using GF.Common.Debug;
using GFEditor.Common.Data;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Asset.AssetBundle.Build
{
    [Serializable]
    public class BuildSetting : BaseProjectSetting
    {
        private const string SETTING_NAME = "AssetBundleSetting";

        private static BuildSetting ms_Instance;

        public string BuildOutput = string.Empty;
        public string BundleBuildsPath = string.Empty;
        public string AssetKeyToAssetMapPath = string.Empty;
        public BuildAssetBundleOptions BuildAssetBundleOptions;
        public bool UseCachedBuild;
        public bool ResetBundleName;
        public bool BuildAssetBuild;
        public bool DeleteOutputBeforeBuild;

        public static BuildSetting GetInstance()
        {
            if (ms_Instance == null)
            {
                ms_Instance = Load<BuildSetting>(SETTING_NAME);
            }

            return ms_Instance;
        }

        public string GetFormatedBuildOutput()
        {
            return FormatPath(BuildOutput);
        }

        public string GetFormatedBundleBuildsPath()
        {
            return FormatPath(BundleBuildsPath);
        }

        public string GetFormateAssetKeyToAssetMapPath()
        {
            return FormatPath(AssetKeyToAssetMapPath);
        }

        protected string FormatPath(string path)
        {
            return path.Replace("{Application.dataPath}", Application.dataPath)
                .Replace("{BuildTarget}", EditorUserBuildSettings.activeBuildTarget.ToString());
        }

        protected override string GetSettingName()
        {
            return SETTING_NAME;
        }
    }
}