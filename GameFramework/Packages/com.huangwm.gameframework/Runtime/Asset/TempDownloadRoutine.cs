using GF.Asset;
using GF.Common.Debug;
using GF.Common.Utility;
using GF.Core.Behaviour;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace GF.Asset
{
	/// <summary>
	/// 临时用，多协程模拟资源下载
	/// </summary>
	class TempDownloadRoutine : BaseBehaviour
	{
		private const string LOG_TAG = "TempAssetDownloadRoutine";

		private Queue<TempAssetInitManager.DownloadDataEntity> m_DownLoadDataQueue;
		/// <summary>
		/// 所有需要下载的总大小
		/// </summary>
		public long TotalDownloadSize
		{
			private set;
			get;
		}
		/// <summary>
		/// 已经下载完成任务的数据
		/// </summary>
		private long m_SizeOfDownloadedData;
		/// <summary>
		/// 正在下载中任务已经下载的数据
		/// </summary>
		private long m_SizeOfDownloadingData;
		/// <summary>
		/// 已经下载完成的大小
		/// </summary>
		public long TotalFinishSize
		{
			get
			{
				return m_SizeOfDownloadedData + m_SizeOfDownloadingData;
			}
		}
		/// <summary>
		/// 所有需要下载的任务个数
		/// </summary>
		public int TotalDownloadCount
		{
			private set;
			get;
		}
		/// <summary>
		/// 已经下载完成的个数
		/// </summary>
		public int TotalFinishCount
		{
			private set;
			get;
		}
		/// <summary>
		/// 是否正在下载
		/// </summary>
		private bool m_IsDownloading = true;
		/// <summary>
		/// 当前正在下载协程
		/// </summary>
		private Coroutine m_CurrentDownloadCoroutine;
		/// <summary>
		/// 下载任务中止或者结束，0,失败 1，成功 ————完成任务数量-----已经下载的数据量
		/// </summary>
		private Action<bool, int, long> m_RoutineAssetFinishCallBack;

		private Action<TempAssetInitManager.DownloadDataEntity> m_SingleAssetDownloadSuccessCallBack;
		/// <summary>
		/// 所有数据是否下载完成
		/// </summary>
		public bool IsAllDataDownloadFinish
		{
			private set; get;
		} = false;
		/// <summary>
		/// 最近一次下载完成的数量
		/// </summary>
		public int LastestDownloadTotalCount
		{
			private set;
			get;
		}
		/// <summary>
		/// 最近一次下载完成的总Size
		/// </summary>
		public long LastestDownloadTotalSize
		{
			private set;
			get;
		}

		private MonoBehaviour m_Mono;

		public TempDownloadRoutine()
			 : base("TempDownloadRoutine", (int)BehaviourPriority.DownloadManager, BehaviourGroup.Default.ToString())
		{
			m_DownLoadDataQueue = new Queue<TempAssetInitManager.DownloadDataEntity>();
			SetEnable(false);
		}

		/// <summary>
		/// 添加一个新的下载数据
		/// </summary>
		/// <param name="data"></param>
		public void AddNewDownLoadData(TempAssetInitManager.DownloadDataEntity data)
		{
			m_DownLoadDataQueue.Enqueue(data);
			TotalDownloadCount++;
			TotalDownloadSize += data.Size;
		}
		/// <summary>
		/// 启动总下载任务
		/// </summary>
		public void StartDownLoad(MonoBehaviour mono)
		{
			m_Mono = mono;
			SetEnable(true);
			if (m_CurrentDownloadCoroutine == null && m_DownLoadDataQueue.Count > 0)
			{
				m_IsDownloading = false;
				IsAllDataDownloadFinish = false;
			}
		}
		/// <summary>
		/// 所有DownloadData资源下载结束回到，成功或者失败
		/// </summary>
		/// <param name="callBack"></param>
		public void SetRountineAssetFinishCallBack(Action<bool, int, long> callBack)
		{
			m_RoutineAssetFinishCallBack = callBack;
		}
	
		/// <summary>
		/// 单个DownloadData资源下载成功回调
		/// </summary>
		/// <param name="callBack"></param>
		public void SetSingleAssetDownloadSuccessCallBack(Action<TempAssetInitManager.DownloadDataEntity> callBack)
		{
			m_SingleAssetDownloadSuccessCallBack = callBack;
		}

		// Update is called once per frame
		public override void OnUpdate(float deltaTime)
		{
			if (m_Mono != null
				&&!m_IsDownloading 
				&& m_DownLoadDataQueue.Count > 0)
			{
				m_IsDownloading = true;
				m_CurrentDownloadCoroutine = m_Mono.StartCoroutine(DownloadData(m_DownLoadDataQueue.Peek()));
			}
		}

		public IEnumerator DownloadData(TempAssetInitManager.DownloadDataEntity data)
		{
			MDebug.Log(LOG_TAG, $"Data Begin to be Download! FileName:{data.FileName},MD5:{data.MD5},Size:{data.Size}");
			string fileDownLoadUrl = IPathUtility.GetServerDownloadPath() + data.FileName;
			using (UnityWebRequest request = UnityWebRequest.Get(fileDownLoadUrl))
			{
				UnityWebRequestAsyncOperation asyOperation = request.SendWebRequest();
				while (asyOperation != null 
					&& !asyOperation.isDone)
				{
					long calculateDownloadSize = (long)Math.Ceiling(data.Size * asyOperation.progress);
					m_SizeOfDownloadingData = calculateDownloadSize;
					yield return null;
				}
				yield return asyOperation;
				if (string.IsNullOrEmpty(request.error))
				{				
					string targetFilePath = IPathUtility.GetLocalFilePathPrefix() + data.FileName;
					FileUtility.CreateFile(targetFilePath, request.downloadHandler.data);
					string newFileMD5 = FileUtility.GetFileMD5(targetFilePath);
					//MD5
					if(!newFileMD5.Equals(data.MD5))
					{
						MDebug.LogError(LOG_TAG,$"New File MD5 Is Not Equals With DownloadData! FileName:{data.FileName},FileMD5:{newFileMD5},DataMD5:{data.MD5}");
						m_CurrentDownloadCoroutine = null;
						m_SizeOfDownloadingData = data.Size;
						IsAllDataDownloadFinish = false;
						m_RoutineAssetFinishCallBack?.Invoke(false, TotalFinishCount, TotalFinishSize);
						m_IsDownloading = true;
						MDebug.LogError(LOG_TAG, $"DownloadData file stop! FileName:{data.FileName} Reason:{request.error}");
						yield break; 
					}
					TotalFinishCount++;
					m_SizeOfDownloadingData = data.Size;
					m_SizeOfDownloadedData += m_SizeOfDownloadingData;
					m_SizeOfDownloadingData = 0;
					m_DownLoadDataQueue.Dequeue();
					MDebug.Log(LOG_TAG, $"DownloadData has been Downloaded Success! FileName:{data.FileName},TargetFilePath:{targetFilePath}");
					m_SingleAssetDownloadSuccessCallBack?.Invoke(data);
					if (m_DownLoadDataQueue.Count > 0)
					{
						m_IsDownloading = false;
						IsAllDataDownloadFinish = false;
						m_CurrentDownloadCoroutine = null;
					}
					else
					{
						LastestDownloadTotalCount = TotalFinishCount;
						LastestDownloadTotalSize = TotalFinishSize;
						m_RoutineAssetFinishCallBack?.Invoke(true, TotalFinishCount, TotalFinishSize);
						ClearAllState();
						IsAllDataDownloadFinish = true;
						MDebug.Log(LOG_TAG,"All AssetData has been Downloaded!");
					}
				}
				else
				{
					m_CurrentDownloadCoroutine = null;
					m_SizeOfDownloadingData = data.Size;
					IsAllDataDownloadFinish = false;
					m_RoutineAssetFinishCallBack?.Invoke(false, TotalFinishCount, TotalFinishSize);
					m_IsDownloading = true;
					MDebug.LogError(LOG_TAG,$"DownloadData file stop! FileName:{data.FileName} Reason:{request.error}");
				}
			}
		}
		
		public void ClearAllState()
		{
			MDebug.Log(LOG_TAG, "Clean TempDownloadRoutine All State!");
			m_RoutineAssetFinishCallBack = null;
			m_SingleAssetDownloadSuccessCallBack = null;
			m_CurrentDownloadCoroutine = null;
			m_IsDownloading = true;
			m_DownLoadDataQueue.Clear();
			m_DownLoadDataQueue = null;
			m_SizeOfDownloadingData = 0;
			m_SizeOfDownloadedData = 0;
			TotalDownloadCount = 0;
			TotalFinishCount = 0;
			TotalDownloadSize = 0;
			IsAllDataDownloadFinish = false;
		
		}
	}

}
