using GF.Common;
using GF.Common.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Common
{
	public class CreateAssetWindow : EditorWindow
	{
		/// <summary>
		/// 所有程序集名字
		/// </summary>
		private string[] m_AssemblyNames;

		/// <summary>
		/// 所有类名字
		/// </summary>
		private List<string> m_ClassNames;
		private List<string> m_StringCahce;

		private PrefsValue<bool> m_SearchClassFoldout;
		private PrefsValue<string> m_SearchClass;
		private List<string> m_SearchResults;

		private PrefsValue<string> m_AssemblyName;
		private PrefsValue<string> m_AssemblyNameFilter;
		private PrefsValue<string> m_ClassName;
		private PrefsValue<string> m_ClassNameFilter;
		private PrefsValue<string> m_ClassNameBaseTypeFilter;
		private PrefsValue<string> m_AssetDirection;
		private PrefsValue<string> m_AssetFile;
		private PrefsValue<bool> m_AssetCover;

		[CTMenuItem("工具/创建Asset")]
		public static void ShowWindow()
		{
			GetWindow<CreateAssetWindow>("创建Asset", true);
		}

		protected void OnEnable()
		{
			m_SearchClassFoldout = new PrefsValue<bool>("CreateAssetWindowm_SearchClassFoldout", false);
			m_SearchClass = new PrefsValue<string>("CreateAssetWindowm_m_SearchClass");
			m_SearchResults = new List<string>();

			m_AssemblyName = new PrefsValue<string>("CreateAssetWindow_AssemblyName", "Assembly-CSharp");
			m_AssemblyNameFilter = new PrefsValue<string>("CreateAssetWindow_AssemblyNameFilter");
			m_ClassName = new PrefsValue<string>("CreateAssetWindow_ClassName");
			m_ClassNameFilter = new PrefsValue<string>("CreateAssetWindow_ClassNameFilter");
			m_ClassNameBaseTypeFilter = new PrefsValue<string>("CreateAssetWindow_ClassNameBaseTypeFilter", "UnityEngine.ScriptableObject");
			m_AssetDirection = new PrefsValue<string>("CreateAssetWindow_AssetDirection");
			m_AssetFile = new PrefsValue<string>("CreateAssetWindow_AssetFile");
			m_AssetCover = new PrefsValue<bool>("CreateAssetWindow_AssetCover");

			Assembly[] assemblys = AppDomain.CurrentDomain.GetAssemblies();
			m_AssemblyNames = new string[assemblys.Length];
			for (int iAssembly = 0; iAssembly < assemblys.Length; iAssembly++)
			{
				m_AssemblyNames[iAssembly] = assemblys[iAssembly].GetName().Name;
			}

			m_ClassNames = new List<string>();
			m_StringCahce = new List<string>();
		}

		protected void OnDisable()
		{
			m_StringCahce.Clear();
			m_StringCahce = null;

			m_ClassNames.Clear();
			m_ClassNames = null;

			m_AssemblyNames = null;
		}

		protected void OnGUI()
		{
			OnGUI_SearchClass();
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			OnGUI_CreateAsset();
			return;
		}

		private void OnGUI_SearchClass()
		{
			m_SearchClassFoldout.Set(EditorGUILayout.Foldout(m_SearchClassFoldout.Get(), "Search Class"));
			if (!m_SearchClassFoldout.Get())
			{
				return;
			}

			EditorGUILayout.BeginHorizontal();
			m_SearchClass.Set(EditorGUILayout.TextField("ClassName", m_SearchClass));
			if (GUILayout.Button("Search"))
			{
				m_SearchResults.Clear();
				for (int iAssembly = 0; iAssembly < m_AssemblyNames.Length; iAssembly++)
				{
					Assembly assembly = Assembly.Load(m_AssemblyNames[iAssembly]);
					if (assembly == null)
					{
						continue;
					}

					Type[] types = assembly.GetTypes();
					for (int iType = 0; iType < types.Length; iType++)
					{
						if (types[iType].FullName.Contains(m_SearchClass.Get()))
						{
							m_SearchResults.Add($"{m_AssemblyNames[iAssembly]}, {types[iType].FullName}");
						}
					}
				}
			}
			EditorGUILayout.EndHorizontal();

			if (m_SearchResults.Count > 0)
			{
				EditorGUILayout.LabelField("Result:");
				for(int iResult = 0; iResult < m_SearchResults.Count; iResult++)
				{
					EditorGUILayout.TextField(m_SearchResults[iResult]);
				}
			}
		}

		private void OnGUI_CreateAsset()
		{
			PopupItem("程序集", ref m_AssemblyName, m_AssemblyNames, ref m_AssemblyNameFilter);
			EditorGUILayout.Space();

			if (string.IsNullOrEmpty(m_AssemblyName))
			{
				return;
			}

			Assembly assembly = Assembly.Load(m_AssemblyName);
			if (assembly == null)
			{
				return;
			}

			Type[] types = assembly.GetTypes();
			m_ClassNames = new List<string>();
			for (int iType = 0; iType < types.Length; iType++)
			{
				bool isObjectType = string.IsNullOrEmpty(m_ClassNameBaseTypeFilter);
				Type tp = types[iType];
				while (!isObjectType && tp.BaseType != null)
				{
					tp = tp.BaseType;
					if (tp.FullName == m_ClassNameBaseTypeFilter)
					{
						isObjectType = true;
					}
				}
				if (isObjectType)
				{
					m_ClassNames.Add(types[iType].FullName);
				}
			}

			if (m_ClassNames.Count < 1)
			{
				return;
			}

			m_ClassNameBaseTypeFilter.Set(EditorGUILayout.TextField("父类", m_ClassNameBaseTypeFilter));

			PopupItem("类", ref m_ClassName, m_ClassNames.ToArray(), ref m_ClassNameFilter);
			EditorGUILayout.Space();
			EditorGUILayout.Space();

			if (string.IsNullOrEmpty(m_ClassName))
			{
				return;
			}

			EditorGUILayout.BeginHorizontal();
			m_AssetDirection.Set(EditorGUILayout.TextField("asset路径", m_AssetDirection));
			if (GUILayout.Button("选择", GUILayout.Width(120)))
			{
				m_AssetDirection.Set(EditorUtility.OpenFolderPanel("asset路径", m_AssetDirection, ""));
			}
			EditorGUILayout.EndHorizontal();

			if (string.IsNullOrEmpty(m_AssetDirection))
			{
				return;
			}
			if (m_AssetDirection.Get().Contains("Assets"))
			{
				m_AssetDirection.Set(m_AssetDirection.Get().Substring(m_AssetDirection.Get().IndexOf("Assets")));
			}

			EditorGUILayout.BeginHorizontal();
			m_AssetFile.Set(EditorGUILayout.TextField("asset文件", m_AssetFile));
			m_AssetCover.Set(EditorGUILayout.Toggle("覆盖文件", m_AssetCover));
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			if (!string.IsNullOrEmpty(m_AssetDirection)
				&& !string.IsNullOrEmpty(m_AssetFile)
				&& m_AssetDirection.Get().Substring(0, 6) == "Assets")
			{
				bool canCreate = m_AssetCover;
				string assetFile = m_AssetDirection + "/" + m_AssetFile + ".asset";
				if (!canCreate)
				{
					canCreate = !File.Exists(Application.dataPath.Remove(Application.dataPath.Length - 6) + assetFile);
				}
				if (canCreate && GUILayout.Button("创建"))
				{
                    ScriptableObject obj = CreateInstance(m_ClassName);
                    AssetDatabase.CreateAsset(obj, assetFile);
					Selection.activeObject = obj;
				}
			}
		}

		private void PopupItem(string label, ref PrefsValue<string> value, string[] strs, ref PrefsValue<string> filter)
		{
			filter.Set(EditorGUILayout.TextField("筛选", filter.Get()));
			if (!string.IsNullOrEmpty(filter))
			{
				m_StringCahce.Clear();
				for (int i = 0; i < strs.Length; i++)
				{
					if (strs[i].ToLower().Contains(filter.Get().ToLower()))
					{
						m_StringCahce.Add(strs[i]);
					}
				}
				strs = m_StringCahce.ToArray();
			}

			if (strs.Length < 1)
			{
				value.Set("");
			}
			else
			{
				int selectIdx = 0;
				if (value.Get() == null || value.Get().Length < 1 || Array.IndexOf(strs, value.Get()) < 0)
				{
					selectIdx = EditorGUILayout.Popup(label, 0, strs);
				}
				else
				{
					selectIdx = EditorGUILayout.Popup(label, Array.IndexOf<string>(strs, value.Get()), strs);
				}

				if (selectIdx >= strs.Length)
				{
					selectIdx = 0;
				}

				value.Set(strs[selectIdx]);
			}
		}
	}
}