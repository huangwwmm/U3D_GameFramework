using GF.Common.Utility;
using GFEditor.Common.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace GFEditor.AssetVersion
{
	/// <summary>
	/// todo 继续完善规则
	/// </summary>
	public class AssetVersionSetting : BaseProjectSetting
	{
		private const string SETTING_NAME = "AssetVersionSetting";

		private static AssetVersionSetting ms_Instance;

		public string VersionNumber = string.Empty;

		public string AssetVersionFilePath = string.Empty;

		public static AssetVersionSetting GetInstance()
		{
			if (ms_Instance == null)
			{
				ms_Instance = Load<AssetVersionSetting>(SETTING_NAME);
			}

			return ms_Instance;
		}

		/// <summary>
		/// 文件名
		/// </summary>
		/// <returns></returns>
		public string GetAssetVersionFileName()
		{
			string formatPath = GetFormatedAssetVersionFilePath();
			return formatPath.Substring(formatPath.LastIndexOf("/")+1);
		}

		public string GetFormatedAssetVersionFilePath()
		{
			return FormatPath(AssetVersionFilePath);
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
