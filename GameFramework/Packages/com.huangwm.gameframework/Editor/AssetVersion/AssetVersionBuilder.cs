using GF.Common.Debug;
using GFEditor.Common.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GFEditor.AssetVersion
{
	public static class AssetVersionBuilder
	{
		private const string LOG_NAME = "AssetVersionBuilder";
		/// <summary>
		/// 根据平台生成平台包下的所有文件的配置信息
		/// </summary>
		public static void GenerateAssetVersionFile()
		{
			AssetVersionSetting setting = AssetVersionSetting.GetInstance();

			string directory = string.Empty;
			directory = Path.GetDirectoryName(setting.GetFormatedAssetVersionFilePath());
			DirectoryInfo directoryInfo = new DirectoryInfo(directory);
			directory = directoryInfo.FullName;
			directory = directory.Replace("\\", "/");
			if (string.IsNullOrEmpty(directory))
			{
				MDebug.LogError(LOG_NAME, "Target AssetVersionFile Path invalid !");
				return;
			}
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
			string assetVersionFilePath = setting.GetFormatedAssetVersionFilePath();
			StringBuilder str = new StringBuilder();
			FileInfo[] files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
			if (files != null)
			{
				string assetVersionFileName = setting.GetAssetVersionFileName();
				for (int i = 0; i < files.Length; i++)
				{
					FileInfo fileInfo = files[i];
					string fileFullName = fileInfo.FullName;
					fileFullName = fileFullName.Replace("\\", "/");
					string fileName = fileFullName.Substring(fileFullName.LastIndexOf("/") + 1);
					string fileRelativePath = fileFullName.Substring(directory.Length + 1);
					if (fileName.Contains(".meta") 
						|| fileName.Equals(assetVersionFileName))
					{
						continue;
					}
					string md5 = EncrypUtil.GetFileMD5(fileFullName);
					string size = Math.Ceiling(fileInfo.Length / 1024f).ToString();
					string commitStr = $"{fileRelativePath} {md5} {size}";
					str.AppendLine(commitStr);
				}			
			}
			File.WriteAllText(assetVersionFilePath, str.ToString());
			EditorUtility.DisplayDialog(LOG_NAME
				, "生成资源版本文件完成！"
				, "OK");
		}
	}
}

