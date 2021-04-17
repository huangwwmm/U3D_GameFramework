using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GF.Common.Utility
{
	public static class IPathUtility
	{
		/// <summary>
		/// 服务器CDN资源地址
		/// </summary>
		public static string ServerDownloadUrl = string.Empty;
		/// <summary>
		/// 默认AB文件名
		/// </summary>
		public static string VersionFilePath = string.Empty;

		/// <summary>
		/// 统一对应GFEditor.Common.Data.BaseProjectSetting
		/// </summary>
		private static string[] ms_PlatformName = new string[] { "Windows", "Android", "IOS" };


		public static string GetPlatformFolderName(RuntimePlatform runningPlatfrom)
		{
			switch(runningPlatfrom)
			{
				case RuntimePlatform.WindowsEditor:
				case RuntimePlatform.WindowsPlayer:
					return ms_PlatformName[0];
				case RuntimePlatform.Android:
					return ms_PlatformName[1];
				case RuntimePlatform.IPhonePlayer:
				case RuntimePlatform.OSXEditor:
				case RuntimePlatform.OSXPlayer:
					return ms_PlatformName[2];
			}
			return string.Empty;
		}

		/// <summary>
		/// 资源服务端版本文件夹
		/// </summary>
		/// <returns></returns>
		public static string GetServerDownloadPath()
		{
			return $"{ServerDownloadUrl}/{GetPlatformFolderName(Application.platform)}/";
		}

		/// <summary>
		/// 资源服务端版本文件路径
		/// </summary>
		/// <returns></returns>
		public static string GetServerDownloadVersionFilePath()
		{
			return $"{ServerDownloadUrl}/{GetPlatformFolderName(Application.platform)}/{VersionFilePath}";
		}

		/// <summary>
		/// 本地可读写版本文件地址
		/// </summary>
		public static string GetLocalVersionFilePath()
		{
			return $"{GetLocalFilePathPrefix()}/{VersionFilePath}";
		}

		/// <summary>
		/// 本地可访读资源文件夹
		/// </summary>
		public static string GetLocalFilePathPrefix()
		{
			return $"{Application.persistentDataPath}/";
		}

		/// <summary>
		/// Android平台，只能WWW,UnityWebRequest加载读取文件，其他平台上C#和WWW都可以,因为Android平台，本地打包文件进行了压缩
		/// </summary>
		/// <returns></returns>
		public static string GetWWWStreamingPath()
		{

#if UNITY_ANDROID && !UNITY_EDITOR
            return $"{Application.streamingAssetsPath}/";
#else
			return $"file://{Application.streamingAssetsPath}/";
#endif
		}


		public static string GetWWWStreamingVersionFile()
		{
			return $"{GetWWWStreamingPath()}{VersionFilePath}";
		}

	}
}

