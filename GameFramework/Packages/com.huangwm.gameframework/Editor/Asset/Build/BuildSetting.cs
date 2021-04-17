using GF.Common.Debug;
using GF.Common.Utility;
using GFEditor.Common.Data;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Asset.Build
{
    [Serializable]
    public class BuildSetting : BaseProjectSetting
    {
        private const string SETTING_NAME = "AssetBundleSetting";

        private static BuildSetting ms_Instance;

        public string BuildOutput = string.Empty;
        public string BundleBuildsPath = string.Empty;
        public string BundleInfoPath = string.Empty;
        public string AssetInfosPath = string.Empty;
        public string AssetKeyEnumFilePath = string.Empty;
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

        public string GetFormateAssetInfosPath()
        {
            return FormatPath(AssetInfosPath);
        }

        public string GetFormatedBundleInfoPath()
        {
            return FormatPath(BundleInfoPath);
        }

        public string GetFormateAssetKeyEnumFilePath()
        {
            return FormatPath(AssetKeyEnumFilePath);
        }


		protected string FormatPath(string path)
        {
            return path.Replace("{Application.dataPath}", Application.dataPath)
                .Replace("{BuildTarget}", GetPlatformFolderName(EditorUserBuildSettings.activeBuildTarget));
        }

		protected override string GetSettingName()
        {
            return SETTING_NAME;
        }
    }
}