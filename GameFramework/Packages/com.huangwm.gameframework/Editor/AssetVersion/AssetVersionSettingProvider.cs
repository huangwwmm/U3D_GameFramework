using GFEditor.Common.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace GFEditor.AssetVersion
{
	public class AssetVersionSettingProvider : SettingsProvider
	{
		private string m_FormatedVersionFilePath;

		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider()
		{
			return new AssetVersionSettingProvider("GF/AssetVersion"
				, SettingsScope.Project
				, new HashSet<string>(new[] { "AssetVersion" }));
		}

		public AssetVersionSettingProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
			: base(path, scopes, keywords)
		{
			m_FormatedVersionFilePath = AssetVersionSetting.GetInstance().GetFormatedAssetVersionFilePath();
		}


		public override void OnGUI(string searchContext)
		{
			base.OnGUI(searchContext);

			AssetVersionSetting setting = AssetVersionSetting.GetInstance();
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("保存"))
			{
				setting.Save();
			}
		
			if (GUILayout.Button("生成资源版本文件"))
			{
				AssetVersionBuilder.GenerateAssetVersionFile();
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			if (EGLUtility.Folder(out setting.AssetVersionFilePath
							, "资源版本文件路径"
							, setting.AssetVersionFilePath))
			{
				m_FormatedVersionFilePath = setting.GetFormatedAssetVersionFilePath();
			}
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("资源版本文件路径", m_FormatedVersionFilePath);
			if (GUILayout.Button("打开", GUILayout.Width(36)))
			{
				EditorUtility.RevealInFinder(m_FormatedVersionFilePath);
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
		}
	}
}

