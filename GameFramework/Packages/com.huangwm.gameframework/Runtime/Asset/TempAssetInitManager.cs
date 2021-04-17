using GF.Common.Data;
using GF.Common.Debug;
using GF.Common.Utility;
using GF.Core;
using GF.Core.Behaviour;
using GF.Core.Event;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GF.Asset
{
	public class TempEventAssetInitData : IUserData
	{
		public Action<string, float> InitAndDownloadingCallBack;
	}

	/// <summary>
	/// 网络请求这Unity封装比较好，所以还是用协程序，后续可以进一步优化
	/// </summary>
	public class TempAssetInitManager : BaseBehaviour
	{
		private const string LOG_TAG = "TempAssetInitManager";
		/// <summary>
		/// Data数据Field
		/// </summary>
		private readonly string[] m_DataParameters = { "FileName", "MD5", "Size" };
		/// <summary>
		/// 仅供这个类内部使用
		/// </summary>
		private static TempAssetInitManager ms_TempAssetInitManager;
		/// <summary>
		/// 需要下载的文件信息list
		/// </summary>
		private List<DownloadDataEntity> m_DataNeedToDownloadList;
		/// <summary>
		/// 考虑比较好操作
		/// </summary>
		private List<DownloadDataEntity> m_LocalDataList;
		/// <summary>
		/// 服务端所有文件信息list
		/// </summary>
		private List<DownloadDataEntity> m_ServerDataList;
		/// <summary>
		/// 服务端所有文件信息Dic
		/// </summary>
		private Dictionary<string, string> m_ServerDataDic;
		/// <summary>
		/// 本地MD5 dic
		/// </summary>
		public Dictionary<string, string> m_localDataDic;
		/// <summary>
		/// 外部获取信息接口,string:提示语，float:进度
		/// </summary>
		private Action<string, float> m_InitAndDownloading;
		/// <summary>
		/// 资源初始化标语
		/// </summary>
		private string m_InitStateTitle = string.Empty;
		/// <summary>
		/// 资源检查更新标语
		/// </summary>
		private string m_CheckUpdateStateTitle = string.Empty;
		

		public TempAssetInitManager()
			 : base("TempAssetInitManager", (int)BehaviourPriority.DownloadManager, BehaviourGroup.Default.ToString())
		{
			ms_TempAssetInitManager = this;
			// 在初始化完成前停用Update
			SetEnable(false);
		}

		/// <summary>
		/// 初始化相关配置，外部调用
		/// </summary>
		/// <param name="initializeData"></param>
		/// <returns></returns>
		public IEnumerator InitializeAsync(KernelInitializeData initializeData)
		{
			m_InitStateTitle = initializeData.InitStateTitle;
			m_CheckUpdateStateTitle = initializeData.CheckUpdateStateTitle;
			yield return null;
			// 恢复Update
			SetEnable(true);
		}

		public override void OnEnable()
		{
			if(Kernel.EventCenter != null)
			{
				Kernel.EventCenter.AddListen((int)EventName.GFAssetInit, OnEventAsset);
			}
		}

		public override void OnDisable()
		{
			if (Kernel.EventCenter != null)
			{
				Kernel.EventCenter.RemoveListen((int)EventName.GFAssetInit, OnEventAsset);
			}
		}
		/// <summary>
		/// 接受消息回调
		/// </summary>
		/// <param name="eventID"></param>
		/// <param name="isImmediately"></param>
		/// <param name="userData"></param>
		private void OnEventAsset(int eventID, bool isImmediately, IUserData userData)
		{
			switch(eventID)
			{
				case (int)EventName.GFAssetInit:
					TempEventAssetInitData initData = userData as TempEventAssetInitData;
					if(initData != null)
					{
						MDebug.Log(LOG_TAG, "Receive AssetInit Message Begin Init And Download Resources!");
						m_InitAndDownloading = initData.InitAndDownloadingCallBack;
						InitStreamingAssetData();
					}
					break;
			}
		}
		/// <summary>
		/// 存放在StreamingAssets下的本地初始文件
		/// </summary>
		private void InitStreamingAssetData()
		{
			if (!File.Exists(IPathUtility.GetLocalVersionFilePath()))
			{
				MDebug.Log(LOG_TAG, "No local resource file in PersistentPath ,Initialize the local resource file!");
				m_InitAndDownloading?.Invoke(m_InitStateTitle, 0);
				Kernel.Mono.StartCoroutine(WWWLoadStreamingVersionFile(IPathUtility.GetWWWStreamingVersionFile(), OnStreamingVersionFileLoadOver));
			}
			else
			{
				MDebug.Log(LOG_TAG, "local resource file in PersistentPath Exist! So Checking remote Data From Server!");
				CheckServerVersion();
			}
		}
		/// <summary>
		/// 开始WWW加载本地配置文件,Android平台只能WWW加载Streaming
		/// </summary>
		/// <param name="versionFilePath"></param>
		/// <returns></returns>
		private IEnumerator WWWLoadStreamingVersionFile(string versionFilePath, Action<string> loadVersionFileOverCallBack)
		{
			MDebug.Log(LOG_TAG, $"StreamingAsset VersionFilePath File Path!{versionFilePath}");
			using (UnityWebRequest data = UnityWebRequest.Get(versionFilePath))
			{
				yield return data.SendWebRequest();
				if (string.IsNullOrEmpty(data.error))
				{
					MDebug.Log(LOG_TAG, "Local Resource File In StreamingPath Loaded Success!");
					byte[] content = data.downloadHandler.data;
					loadVersionFileOverCallBack(Encoding.UTF8.GetString(content));
				}
				else
				{
					MDebug.Log(LOG_TAG, $"Local Resource File In StreamingPath Loaded Failed! May Be No Such File,Reason : {data.error}");
					loadVersionFileOverCallBack(string.Empty);
				}
			}
		}
		/// <summary>
		/// StreamingAsset下的VersionFile读取完成后回调
		/// </summary>
		/// <param name="versionFileStr"></param>
		private void OnStreamingVersionFileLoadOver(string versionFileString)
		{
			MDebug.Log(LOG_TAG, $"local resource file in StreamingPath Loaded ,FileContent:{versionFileString}");
			if (string.IsNullOrEmpty(versionFileString))
			{
				MDebug.Log(LOG_TAG, $"local resource file in Empty ,So Checking remote Data From Server!");
				m_InitAndDownloading?.Invoke(m_InitStateTitle, 1);
				CheckServerVersion();
			}
			else
			{
				List<DownloadDataEntity> dataList = GetDownloadDataListFromText(versionFileString);
				Kernel.Mono.StartCoroutine(LocalAssetDataLoad(dataList));
			}	
		}
		private IEnumerator LocalAssetDataLoad(List<DownloadDataEntity> dataList)
		{
			if (File.Exists(IPathUtility.GetLocalVersionFilePath()))
			{
				File.Delete(IPathUtility.GetLocalVersionFilePath());
			}
			if(dataList != null)
			{
				for (int i = 0; i < dataList.Count; i++)
				{
					//相对路径文件名
					string fileName = dataList[i].FileName;
					string fileLoadPath = IPathUtility.GetWWWStreamingPath() + fileName;
					string filePersistentPath = IPathUtility.GetLocalFilePathPrefix() + fileName;
					string versionText = $"{fileName} {dataList[i].MD5} {dataList[i].Size}";
					yield return LocalAssetDataLoad(fileLoadPath, filePersistentPath, versionText);
					m_InitAndDownloading?.Invoke(m_InitStateTitle, ((float)(i + 1) / dataList.Count));
					MDebug.Log(LOG_TAG, $"Initialization of the local resource file is complete! FileName:{fileName},Current:{i+1},Total:{dataList.Count}");
				}
			}
			CheckServerVersion();
		}
		/// <summary>
		/// 拷贝文件到本地
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="localPath"></param>
		/// <returns></returns>
		private IEnumerator LocalAssetDataLoad(string fileLoadPath, string filePersistentPath, string versionText)
		{
			MDebug.Log(LOG_TAG, $"LocalAssetDataLoad,fileLoadPath:{fileLoadPath},filePersistentPath:{filePersistentPath},versionText:{versionText}");
			using (UnityWebRequest data = UnityWebRequest.Get(fileLoadPath))
			{
				yield return data.SendWebRequest();
				if (string.IsNullOrEmpty(data.error))
				{
					byte[] content = data.downloadHandler.data;
					string filePersistentParentFolderPath = Path.GetDirectoryName(filePersistentPath);
					MDebug.Log(LOG_TAG, $"filePersistentParentFolderPath:{filePersistentParentFolderPath}");
					if (!Directory.Exists(filePersistentParentFolderPath))
					{
						Directory.CreateDirectory(filePersistentParentFolderPath);
					}
					using (FileStream fs = File.Create(filePersistentPath, content.Length))
					{
						fs.Write(content, 0, content.Length);
						fs.Flush();
						fs.Close();
					}
					using (FileStream fs = new FileStream(IPathUtility.GetLocalVersionFilePath(), FileMode.OpenOrCreate, FileAccess.Write))
					{
						using (StreamWriter sw = new StreamWriter(fs))
						{
							sw.BaseStream.Seek(0, SeekOrigin.End);
							sw.WriteLine(versionText);
							sw.Flush();
							sw.Close();
						}
						fs.Close();
					}
					MDebug.Log(LOG_TAG, $"LocalAssetDataLoad,filePersistentPath:{filePersistentPath} Generate Success!");
				}
			}
		}

		/// <summary>
		/// 初始化服务端检查更新
		/// </summary>
		private void CheckServerVersion()
		{
			MDebug.Log(LOG_TAG, $"Checking ServerVersionFile!");
			m_InitAndDownloading?.Invoke(m_CheckUpdateStateTitle, 0);
			Kernel.DownloadManager.InitServerVersion(ServerDownloadDataCallBack, m_InitAndDownloading,RoutineAssetFinishCallBack,SingleAssetDownloadSuccessCallBack);
		}

		/// <summary>
		/// 单个资源下载完成
		/// </summary>
		/// <param name="obj"></param>
		private void SingleAssetDownloadSuccessCallBack(DownloadDataEntity dataEntity)
		{
			MDebug.Log(LOG_TAG, $"SingleAssetDownloadSuccessCallBack!,FileName:{dataEntity.FileName},MD5:{dataEntity.MD5}");
			ModifyLocalAssetVersionFile(dataEntity);
			//处理需要下载list
			for(int i =0;i< m_DataNeedToDownloadList.Count;i++)
			{
				if(m_DataNeedToDownloadList[i].FileName.Equals(dataEntity.FileName))
				{
					m_DataNeedToDownloadList.RemoveAt(i);
					break;
				}
			}
		}
		/// <summary>
		/// 单个协程结束下载
		/// </summary>
		/// <param name="arg1"></param>
		/// <param name="arg2"></param>
		/// <param name="arg3"></param>
		private void RoutineAssetFinishCallBack(bool isSuccess, int downloadedCount, long downloadedSize)
		{
			//ToDo
			if (isSuccess)
			{
				MDebug.Log(LOG_TAG, $"RoutineAsset Finish!  Result:Success!,DownloadedCount:{downloadedCount},DownloadedSize:{downloadedSize}");
			}
			else
			{
				//考虑是否资源出错，重新下载
				MDebug.LogError(LOG_TAG, $"RoutineAsset Finish!  Result:Failed!,DownloadedCount:{downloadedCount},DownloadedSize:{downloadedSize}");
			}
		}
		/// <summary>
		/// 回调
		/// 解析服务端配置文件字符串
		/// </summary>
		/// <param name="obj"></param>
		private void ServerDownloadDataCallBack(string content)
		{
			MDebug.Log(LOG_TAG, $"Checking ServerVersionFile Complete! Content of VersionFile :{content}");
			m_ServerDataList = GetDownloadDataListFromText(content);
			m_ServerDataDic = GetFileNameToMD5Dic(m_ServerDataList);
			m_DataNeedToDownloadList = new List<DownloadDataEntity>();
			if (File.Exists(IPathUtility.GetLocalVersionFilePath()))
			{
				m_LocalDataList = GetDownloadDataListFromText(File.ReadAllText(IPathUtility.GetLocalVersionFilePath()));
				m_localDataDic = GetFileNameToMD5Dic(m_LocalDataList);
				if (m_ServerDataList != null)
				{
					for (int i = 0; i < m_ServerDataList.Count; i++)
					{
						if (!m_localDataDic.ContainsKey(m_ServerDataList[i].FileName))
						{
							m_DataNeedToDownloadList.Add(m_ServerDataList[i]);
						}
						else
						{
							if (!m_localDataDic[m_ServerDataList[i].FileName].Equals(m_ServerDataList[i].MD5))
							{
								m_DataNeedToDownloadList.Add(m_ServerDataList[i]);
							}
						}
					}
				}
			}
			else
			{
				m_LocalDataList = new List<DownloadDataEntity>(m_ServerDataList.Count);
				m_localDataDic = new Dictionary<string, string>();
				if (m_ServerDataList != null)
				{
					for (int i = 0; i < m_ServerDataList.Count; i++)
					{
						m_DataNeedToDownloadList.Add(m_ServerDataList[i]);
					}
				}
			}
			//重置
			Kernel.DownloadManager.ClearRoutineArray();
			//开始下载
			Kernel.DownloadManager.DownloadData(m_DataNeedToDownloadList.ToArray());
		}

		/// <summary>
		/// 获取Key:文件名 value:MD5值 的Dictionary 
		/// </summary>
		/// <param name="list"></param>
		/// <returns></returns>
		private Dictionary<string, string> GetFileNameToMD5Dic(List<DownloadDataEntity> dataList)
		{
			Dictionary<string, string> tmpDic = null;
			if (dataList != null)
			{
				tmpDic = new Dictionary<string, string>();
				for (int i = 0; i < dataList.Count; i++)
				{
					tmpDic[dataList[i].FileName] = dataList[i].MD5;
				}
			}
			return tmpDic;
		}

		/// <summary>
		/// 从返回字符串转换下载数据列表
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		private List<DownloadDataEntity> GetDownloadDataListFromText(string text)
		{
			List<DownloadDataEntity> downloadDataList = null;
			if (!string.IsNullOrEmpty(text))
			{
				string[] splitData = text.Split('\n');
				if (splitData != null)
				{
					MDebug.Log(LOG_TAG, $"GetDownloadDataListFromText: DataCount:{splitData.Length}");
					downloadDataList = new List<DownloadDataEntity>(splitData.Length);
					for (int i = 0; i < splitData.Length; i++)
					{
						MDebug.Log(LOG_TAG, $"GetDownloadDataListFromText:  DataString:{splitData[i]}");
						if(!string.IsNullOrEmpty(splitData[i]))
						{
							DownloadDataEntity dataEntity = GetDataEntity(splitData[i]);
							if(IsDownloadEntityValid(dataEntity))
							{
								downloadDataList.Add(dataEntity);
							}
						}
					}
				}
			}
			return downloadDataList;
		}

		private bool IsDownloadEntityValid(DownloadDataEntity dataEntity)
		{
			if(!string.IsNullOrEmpty(dataEntity.FileName)
				&& !string.IsNullOrEmpty(dataEntity.MD5))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// 根据string，获取数据,但是会传值
		/// </summary>
		/// <param name="dataText"></param>
		/// <returns></returns>
		private DownloadDataEntity GetDataEntity(string dataText)
		{
			if (!string.IsNullOrEmpty(dataText))
			{
				string[] paramStr = dataText.Split(' ');
				if (paramStr != null && paramStr.Length >= m_DataParameters.Length)
				{
					MDebug.Log(LOG_TAG, $"Data FileName:{paramStr[0]},MD5:{paramStr[1]},Size:{paramStr[2]}");
					DownloadDataEntity dataEntity = new DownloadDataEntity(paramStr[0], paramStr[1], long.Parse(paramStr[2]));
					return dataEntity;
				}
				else
				{
					MDebug.LogError(LOG_TAG, $"Data No Enough Parameters,DataText:{dataText}");
				}
			}
			return new DownloadDataEntity();
		}

		/// <summary>
		/// 修改内存以及本地版本文件
		/// </summary>
		private void ModifyLocalAssetVersionFile(DownloadDataEntity entity)
		{
			if (m_localDataDic.ContainsKey(entity.FileName))
			{
				for (int i = 0; i < m_LocalDataList.Count; i++)
				{
					if (m_LocalDataList[i].FileName.Equals(entity.FileName))
					{
						m_LocalDataList[i] = entity;
					}
				}
				MDebug.Log(LOG_TAG, $"ModifyLocalAssetVersionFile,Update MD5! FileName:{entity.FileName},NewMD5:{entity.MD5}");
				m_localDataDic[entity.FileName] = entity.MD5;
			}
			else
			{
				m_LocalDataList.Add(entity);
				m_localDataDic[entity.FileName] = entity.MD5;
				MDebug.Log(LOG_TAG, $"ModifyLocalAssetVersionFile,Add New! FileName:{entity.FileName},NewMD5:{entity.MD5}");
			}
			SaveLocalFileFromLocalList(m_LocalDataList);
		}

		/// <summary>
		/// 更新本地资源版本文件
		/// </summary>
		/// <param name="mlist"></param>
		private void SaveLocalFileFromLocalList(List<DownloadDataEntity> mlist)
		{
			if (mlist != null)
			{
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < mlist.Count; i++)
				{
					sb.AppendLine($"{mlist[i].FileName} {mlist[i].MD5} {mlist[i].Size}");
				}
				File.WriteAllText(IPathUtility.GetLocalVersionFilePath(), sb.ToString());
			}
		}

		/// <summary>
		/// DownloadData数据
		/// </summary>
		public struct DownloadDataEntity
		{
			/// <summary>
			/// 文件名
			/// </summary>
			public readonly string FileName;
			/// <summary>
			/// MD5值
			/// </summary>
			public readonly string MD5;
			/// <summary>
			/// 文件大小
			/// </summary>
			public readonly long Size;

			public DownloadDataEntity(string fileName,string md5,long size)
			{
				FileName = fileName;
				MD5 = md5;
				Size = size;
			}
		}


		public override void OnRelease()
		{
			if(m_DataNeedToDownloadList != null)
			{
				m_DataNeedToDownloadList.Clear();
				m_DataNeedToDownloadList = null;
			}
			if (m_ServerDataList != null)
			{
				m_ServerDataList.Clear();
				m_ServerDataList = null;
			}
			if (m_ServerDataDic != null)
			{
				m_ServerDataDic.Clear();
				m_ServerDataDic = null;
			}
			if (m_LocalDataList != null)
			{
				m_LocalDataList.Clear();
				m_LocalDataList = null;
			}
			if (m_localDataDic != null)
			{
				m_localDataDic.Clear();
				m_localDataDic = null;
			}
			m_InitAndDownloading = null;
		}
	}
}

