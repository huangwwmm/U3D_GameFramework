using UnityEngine;
using System.IO;
using System;
using UnityEditor;
using System.Collections.Generic;
using System.Threading;
using GF.Common.Collection;
using GF.Common.Utility;
using GF.Common.Debug;

namespace GFEditor.AssetDB
{
	public class DB
	{
		/// <summary>
		/// 自动保存的时间间隔，毫秒
		/// </summary>
		private const int AUTOSAVE_MILLISECONDSTIMEOUT = 60 * 1000;

		private static DB ms_Instance;

		private readonly string m_DBPath;
		private readonly object m_Lock;
		private BetterDictionary<string, Asset> m_Assets;
		private bool m_IsDirty;
		private bool m_StopAutoSave;

		public static DB GetInstance()
		{
			if (ms_Instance == null)
			{
				ms_Instance = new DB();
			}

			return ms_Instance;
		}

		private DB()
		{
			m_DBPath = Directory.GetParent(Application.dataPath).FullName + @"\Library\AssetDB.bin";
			m_IsDirty = false;
			m_Lock = new object();

			LoadDB();

			m_StopAutoSave = false;
			new Thread(new ThreadStart(AutoSave_Thread)).Start();
		}

		~DB()
		{
			m_StopAutoSave = true;
			if (m_IsDirty)
			{
				SaveDB();
			}
		}

		public void LoadDB()
		{
			if (File.Exists(m_DBPath))
			{
				try
				{
					m_Assets = FileUtility.ReadFromBinaryFile(m_DBPath) as BetterDictionary<string, Asset>;
				}
				catch (Exception e)
				{
					MDebug.LogError("AssetDB", $"解析({m_DBPath})失败, Exception:\n{e.ToString()}");
					m_Assets = new BetterDictionary<string, Asset>();
				}
			}
			else
			{
				m_Assets = new BetterDictionary<string, Asset>();
			}
		}

		public void SaveDB()
		{
			lock (m_Lock)
			{
				try
				{
					System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
					stopwatch.Restart();
					FileUtility.WriteToBinaryFile(m_DBPath, m_Assets);
                    MDebug.Log("AssetDB", $"保存AssetDB耗时{stopwatch.ElapsedMilliseconds}ms");
					m_IsDirty = false;

				}
				catch (Exception e)
				{
                    MDebug.LogError("AssetDB", $"保存({m_DBPath})失败, Exception:\n{e.ToString()}");
				}
			}
		}

		public BetterDictionary<string, Asset> GetAssets()
		{
			return m_Assets;
		}

		public void RecaculateDBWithDialog()
		{
			if (!EditorUtility.DisplayDialog("AssetDB", "是否重新计算所有资源的引用关系？\n该操作预计需要数分钟", "OK", "Cancel"))
			{
				return;
			}

			m_Assets.Clear();
			string[] assetGUIDs = AssetDatabase.FindAssets("");
			float allCount = assetGUIDs.Length;
			HashSet<string> assetPaths = new HashSet<string>();
			for (int iAsset = 0; iAsset < assetGUIDs.Length; iAsset++)
			{
				string iterAssetGUID = assetGUIDs[iAsset];
				string iterAssetPath = AssetDatabase.GUIDToAssetPath(iterAssetGUID);
				if (string.IsNullOrEmpty(iterAssetPath)
					|| !assetPaths.Add(iterAssetPath))
				{
					continue;
				}

				Asset iterAsset = GetOrCreateAsset(iterAssetGUID);
				string[] dependencies = AssetDatabase.GetDependencies(iterAssetPath, false);
				for (int iDependence = 0; iDependence < dependencies.Length; iDependence++)
				{
					string iterDependenceAssetGUID = AssetDatabase.AssetPathToGUID(dependencies[iDependence]);
					iterAsset.Dependencies.Add(iterDependenceAssetGUID);
					GetOrCreateAsset(iterDependenceAssetGUID).BeDependencies.Add(iterAssetGUID);
				}

				if (EditorUtility.DisplayCancelableProgressBar("重新计算所有资源的引用关系", $"{iAsset}/{allCount}", iAsset / allCount))
				{
					break;
				}

			}

			SaveDB();
			EditorUtility.ClearProgressBar();
		}

		public void OnPostprocessAllAssets(string[] importedAssets
			, string[] deletedAssets
			, string[] movedAssets
			, string[] movedFromAssetPaths)
		{
			System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
			stopwatch.Restart();
			float allCount = deletedAssets.Length + importedAssets.Length;
			int handledCount = 0;
			for (int iAsset = 0; iAsset < deletedAssets.Length; iAsset++)
			{
				if (EditorUtility.DisplayCancelableProgressBar("AssetPostprocess 计算资源的引用关系", $"{handledCount}/{allCount}", handledCount++ / allCount))
				{
					AbortPostprocessAllAssets();
					return;
				}

				OnPostProcessDeletedAsset(AssetDatabase.AssetPathToGUID(deletedAssets[iAsset]));
			}

			for (int iAsset = 0; iAsset < importedAssets.Length; iAsset++)
			{
				if (EditorUtility.DisplayCancelableProgressBar("AssetPostprocess 计算资源的引用关系", $"{handledCount}/{allCount}", handledCount++ / allCount))
				{
					AbortPostprocessAllAssets();
					return;
				}

				string assetPath = importedAssets[iAsset];
				OnPostProcessImportedAssets(assetPath, AssetDatabase.AssetPathToGUID(assetPath));
			}
			EditorUtility.ClearProgressBar();
            MDebug.Log("AssetDB", $"AssetPostprocess-AssetDB 处理了{importedAssets.Length}个导入、{deletedAssets.Length}个删除，耗时{stopwatch.ElapsedMilliseconds}ms");

			SetDirty();
		}

		public void SetDirty()
		{
			lock (m_Lock)
			{
				m_IsDirty = true;
			}
		}

		private void AbortPostprocessAllAssets()
		{
			EditorUtility.ClearProgressBar();
            MDebug.Log("AssetDB", $"AssetPostprocess-AssetDB 操作被中断");
			SetDirty();
		}

		private void OnPostProcessDeletedAsset(string assetGUID)
		{
			if (!m_Assets.TryGetValue(assetGUID, out Asset asset))
			{
				return;
			}
			m_Assets.Remove(assetGUID);

			for (int iAsset = 0; iAsset < asset.BeDependencies.Count; iAsset++)
			{
				if (m_Assets.TryGetValue(asset.BeDependencies[iAsset], out Asset beDependenceAsset))
				{
					beDependenceAsset.Dependencies.Remove(assetGUID);
				}
			}
		}

		private void OnPostProcessImportedAssets(string assetPath, string assetGUID)
		{
			Asset asset = GetOrCreateAsset(assetGUID);

			BetterList<string> oldDependencies = asset.Dependencies.CopyToList();
			string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);
			asset.Dependencies.Clear();
			for (int iDependence = 0; iDependence < dependencies.Length; iDependence++)
			{
				string iterDependenceAssetGUID = AssetDatabase.AssetPathToGUID(dependencies[iDependence]);
				asset.Dependencies.Add(iterDependenceAssetGUID);
				if (!oldDependencies.Remove(iterDependenceAssetGUID))
				{
					GetOrCreateAsset(iterDependenceAssetGUID).BeDependencies.Add(assetGUID);
				}
			}

			for (int iDependence = 0; iDependence < oldDependencies.Count; iDependence++)
			{
				string iterDependenceAssetGUID = AssetDatabase.AssetPathToGUID(oldDependencies[iDependence]);
				if (m_Assets.TryGetValue(iterDependenceAssetGUID, out Asset iterDependenceAsset))
				{
					iterDependenceAsset.BeDependencies.Remove(assetGUID);
				}
			}
		}

		private Asset GetOrCreateAsset(string assetGUID)
		{
			if (!m_Assets.TryGetValue(assetGUID, out Asset asset))
			{
				asset = new Asset();
				m_Assets.Add(assetGUID, asset);
			}

			return asset;
		}

		private void AutoSave_Thread()
		{
			while (true)
			{
				Thread.Sleep(AUTOSAVE_MILLISECONDSTIMEOUT);

				if (m_StopAutoSave)
				{
					return;
				}

				if (m_IsDirty)
				{
					SaveDB();
				}
			}
		}
	}
}