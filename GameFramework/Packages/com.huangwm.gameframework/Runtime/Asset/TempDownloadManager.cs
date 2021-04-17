using GF.Common.Debug;
using GF.Common.Utility;
using GF.Core;
using GF.Core.Behaviour;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace GF.Asset
{
	public class TempDownloadManager : BaseBehaviour
	{
		private const string LOG_TAG = "TempDownloadManager";
		/// <summary>
		/// CDN资源服务器
		/// </summary>
		private string m_ServerVersionFilePath;

		/// <summary>
		/// 下载超时时间
		/// </summary>
		private int m_DownloadTimeOut;
		/// <summary>
		/// 初始的Routine数量,不宜过多提高性能
		/// </summary>
		private int m_DownloadMaxRoutineCount;

		/// <summary>
		/// 解析服务端资源配置文件回调
		/// </summary>
		private Action<string> m_ServerVersionFileCallBack;
		private TempDownloadRoutine[] m_RoutineArray;
		private int m_RoutineIndex;
		/// <summary>
		/// 是否下载中
		/// </summary>
		private bool m_Downloading = false;
		/// <summary>
		/// 下载检测进度时间间隔
		/// </summary>
		private float m_CheckProgressDelta = 0.5f;
		/// <summary>
		/// 下一次检测进度时间
		/// </summary>
		private float m_NextCheckProgressTime;
		/// <summary>
		/// 单位KB
		/// </summary>
		public float DownLoadSpeed
		{
			private set;
			get;
		}
		/// <summary>
		/// 上次检查进度时候的下载量 KB
		/// </summary>
		private long m_LastCheckProgressFinishedSize;
		/// <summary>
		/// 所有需要下载的总大小
		/// </summary>
		public long TotalDownloadSize
		{
			get
			{
				long size = 0; ;
				for (int i = 0; i < m_RoutineArray.Length; i++)
				{
					if (m_RoutineArray[i] != null)
					{
						if (m_RoutineArray[i].IsAllDataDownloadFinish)
						{
							size += m_RoutineArray[i].LastestDownloadTotalSize;
						}
						else
						{
							size += m_RoutineArray[i].TotalDownloadSize;
						}
					}
				}
				return size;
			}
		}

		/// <summary>
		/// 已经下载完成的大小
		/// </summary>
		public long TotalFinishSize
		{
			get
			{
				long size = 0; ;
				for (int i = 0; i < m_RoutineArray.Length; i++)
				{
					if (m_RoutineArray[i] != null)
					{
						//下载完成
						if (m_RoutineArray[i].IsAllDataDownloadFinish)
						{
							size += m_RoutineArray[i].LastestDownloadTotalSize;
						}
						else
						{
							//未下载完成
							size += m_RoutineArray[i].TotalFinishSize;
						}
					}
				}
				return size;
			}
		}

		/// <summary>
		/// 所有需要下载的任务个数
		/// </summary>
		public int TotalDownloadCount
		{
			get
			{
				int count = 0;
				for (int i = 0; i < m_RoutineArray.Length; i++)
				{
					if (m_RoutineArray[i] != null)
					{
						if (m_RoutineArray[i].IsAllDataDownloadFinish)
						{
							count += m_RoutineArray[i].LastestDownloadTotalCount;
						}
						else
						{
							count += m_RoutineArray[i].TotalDownloadCount;
						}
					}
				}
				return count;
			}
		}

		/// <summary>
		/// 已经下载完成的个数
		/// </summary>
		public int TotalFinishCount
		{
			get
			{
				int count = 0;
				for (int i = 0; i < m_RoutineArray.Length; i++)
				{
					if (m_RoutineArray[i] != null)
					{
						//下载完成
						if (m_RoutineArray[i].IsAllDataDownloadFinish)
						{
							count += m_RoutineArray[i].LastestDownloadTotalCount;
						}
						else
						{
							//未下载完成
							count += m_RoutineArray[i].TotalFinishCount;
						}
					}
				}
				return count;
			}
		}

		private Action<string, float> m_DownloadingCallBack;

		private Action<bool, int, long> m_RoutineAssetFinishCallBack;

		private Action<TempAssetInitManager.DownloadDataEntity> m_SingleAssetDownloadSuccessCallBack;

		/// <summary>
		/// 正在下载更新中标语
		/// </summary>
		private string m_UpdatingStateTitle = string.Empty;
		/// <summary>
		/// 下载完成标语
		/// </summary>
		private string m_UpdateFinishTitle = string.Empty;

		public TempDownloadManager()
			 : base("TempDownloadManager", (int)BehaviourPriority.DownloadManager, BehaviourGroup.Default.ToString())
		{
			SetEnable(false);
		}

		/// <summary>
		/// 初始化相关配置，外部调用
		/// </summary>
		/// <param name="initializeData"></param>
		/// <returns></returns>
		public IEnumerator InitializeAsync(KernelInitializeData initializeData)
		{
			IPathUtility.ServerDownloadUrl = initializeData.ServerDownLoadUrl;
			IPathUtility.VersionFilePath = initializeData.AssetVersionFileName;
			m_UpdatingStateTitle = initializeData.UpdatingStateTitle;
			m_UpdateFinishTitle = initializeData.UpdateFinishTitle;
			m_ServerVersionFilePath = IPathUtility.GetServerDownloadVersionFilePath();
			m_DownloadTimeOut = initializeData.DownloadTimeOut;
			m_DownloadMaxRoutineCount = initializeData.DownloadMaxRoutineCount;
			m_RoutineArray = new TempDownloadRoutine[m_DownloadMaxRoutineCount];
			yield return null;
			// 恢复Update
			SetEnable(true);
		}

		/// <summary>
		/// 初始化服务端版本信息下载数据
		/// </summary>
		/// <param name="serverVersionUrl"></param>
		/// <param name="onServerDownloadDataCallBack"></param>
		public void InitServerVersion(Action<string> onServerDownloadDataCallBack,
			Action<string, float> onDownloadingCallBack, 
			Action<bool, int, long> onRoutineAssetFinishCallBack,
			Action<TempAssetInitManager.DownloadDataEntity> onSingleAssetDownloadSuccessCallBack
			)
		{
			m_ServerVersionFileCallBack = onServerDownloadDataCallBack;
			m_DownloadingCallBack = onDownloadingCallBack;
			m_SingleAssetDownloadSuccessCallBack = onSingleAssetDownloadSuccessCallBack;
			m_RoutineAssetFinishCallBack = onRoutineAssetFinishCallBack;
			Kernel.Mono.StartCoroutine(DownloadServerVersionFile(m_ServerVersionFilePath));
		}

		/// <summary>
		/// 开始下载数据
		/// </summary>
		public void DownloadData(TempAssetInitManager.DownloadDataEntity[] needDownloadDataArray)
		{
			if (needDownloadDataArray != null)
			{
				for (int i = 0; i < needDownloadDataArray.Length; i++)
				{
					m_RoutineIndex = i % m_DownloadMaxRoutineCount;
					if (m_RoutineArray[m_RoutineIndex] == null)
					{
						m_RoutineArray[m_RoutineIndex] = new TempDownloadRoutine();
					}
					m_RoutineArray[m_RoutineIndex].AddNewDownLoadData(needDownloadDataArray[i]);
					m_RoutineArray[m_RoutineIndex].SetRountineAssetFinishCallBack(m_RoutineAssetFinishCallBack);
					m_RoutineArray[m_RoutineIndex].SetSingleAssetDownloadSuccessCallBack(m_SingleAssetDownloadSuccessCallBack);
				}
				for (int j = 0; j < m_RoutineArray.Length; j++)
				{
					if (m_RoutineArray[j] != null)
					{
						m_RoutineArray[j].StartDownLoad(Kernel.Mono);
					}
				}
				m_Downloading = true;
				m_LastCheckProgressFinishedSize = 0;
				m_NextCheckProgressTime = Time.realtimeSinceStartup + m_CheckProgressDelta;
			}
		}

		/// <summary>
		/// 重置所有的下载器
		/// </summary>
		public void ClearRoutineArray()
		{
			for (int i = 0; i < m_RoutineArray.Length; i++)
			{
				if (m_RoutineArray[i] != null)
				{
					m_RoutineArray[i].ClearAllState();
				}
			}
		}

		// Update is called once per frame
		public override void OnUpdate(float deltaTime)
		{
			if (m_Downloading)
			{
				if (Time.realtimeSinceStartup > m_NextCheckProgressTime)
				{
					m_NextCheckProgressTime = Time.realtimeSinceStartup + m_CheckProgressDelta;
					long totalFinishSize = TotalFinishSize;
					long deltaSize = totalFinishSize - m_LastCheckProgressFinishedSize;
					DownLoadSpeed = Mathf.Max(0f, deltaSize / m_CheckProgressDelta);
					m_LastCheckProgressFinishedSize = totalFinishSize;
					int finishCount = 0;
					for (int i = 0; i < m_RoutineArray.Length; i++)
					{
						if (m_RoutineArray[i] != null)
						{
							if (m_RoutineArray[i].IsAllDataDownloadFinish)
							{
								finishCount++;
							}
						}
						else
						{
							finishCount++;
						}
					}
					if (finishCount >= m_RoutineArray.Length)
					{
						m_Downloading = false;
						DownLoadSpeed = 0;
						MDebug.Log(LOG_TAG, "All Asset Download Routine Download Finish!");
						m_DownloadingCallBack?.Invoke(m_UpdateFinishTitle, 1);
					}

					if (m_Downloading)
					{
						MDebug.Log(LOG_TAG,$"Downloading: TotalDownloadCount:{TotalDownloadCount},DownloadedCount:{TotalFinishCount},TotalDownloadSize:{TotalDownloadSize},DownloadedSize:{TotalFinishSize},DownloadSpeed/KB:{DownLoadSpeed}");
						float progress = (float)TotalFinishCount / TotalDownloadCount;
						m_DownloadingCallBack?.Invoke(string.Format(m_UpdatingStateTitle, TotalDownloadCount, TotalFinishCount), progress);
					}

				}

			}
		}


		private IEnumerator DownloadServerVersionFile(string filePath)
		{
			using (UnityWebRequest data = UnityWebRequest.Get(filePath))
			{
				data.timeout = m_DownloadTimeOut;
				yield return data.SendWebRequest();
				if (string.IsNullOrEmpty(data.error))
				{
					string content = data.downloadHandler.text;
					m_ServerVersionFileCallBack?.Invoke(content);
				}
				else
				{
					MDebug.LogError(LOG_TAG,"Download failed !" + data.error);
				}
			}

		}
	}
}