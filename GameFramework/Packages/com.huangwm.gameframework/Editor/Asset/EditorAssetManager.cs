using System;
using GF.Asset;
using GF.Core.Behaviour;
using UnityEngine;
using GF.Core;
using System.Collections;
using System.IO;
using LitJson;
using GF.Common.Debug;
using GF.Common.Data;
using System.Collections.Generic;
using UnityEditor;

namespace GFEditor.Asset
{
	/// <summary>
	/// Editor模式下加载资源
	/// </summary>
	public class EditorAssetManager : BaseBehaviour, IAssetManager
	{
		private const string LOG_TAG = "EditorAssetManager";

		private static EditorAssetManager ms_EditorAssetManager;
		private string m_RootBundlePath;

		
		#region Asset
		private AssetInfo[] m_AssetInfos;
		private Dictionary<int, Action<AssetKey, UnityEngine.Object>> m_AssetCallBackDic;
		#endregion
		private Dictionary<string, Queue<GameObjectInstantiateData>> m_AssetToGameObjectInstantiateData;

		[InitializePackage("EditorAssetManager", (int)PackageProiority.EditorAssetManager)]
		internal static IEnumerator InitializePackage(KernelInitializeData initializeData)
		{
#if UNITY_EDITOR
			EditorAssetManager editorAssetManager = new EditorAssetManager();
			Kernel.AssetManager = editorAssetManager;
			yield return editorAssetManager.InitializeAsync(initializeData);
#else
			yield return null;
#endif
		}

		public EditorAssetManager()
			: base("EditorAssetManager", (int)BehaviourPriority.AssetManager, BehaviourGroup.Default.ToString())
		{
			ms_EditorAssetManager = this;
			SetEnable(false);
		}

		private IEnumerator InitializeAsync(KernelInitializeData initializeData)
		{
			//加载AssetMap
			string assetInfosJson = string.Empty;
			try
			{
				assetInfosJson = File.ReadAllText(initializeData.AssetInfosFile);
				m_AssetInfos = JsonMapper.ToObject<AssetInfo[]>(new JsonReader(assetInfosJson));
			}
			catch (Exception e)
			{
				MDebug.LogError(LOG_TAG, $"解析AssetInfos:({initializeData.AssetInfosFile})失败。\nJson:({assetInfosJson})\n\n{e.ToString()}");
			}
			m_AssetCallBackDic = new Dictionary<int, Action<AssetKey, UnityEngine.Object>>();
			m_AssetToGameObjectInstantiateData = new Dictionary<string, Queue<GameObjectInstantiateData>>();
			yield return null;
			SetEnable(true);
		}

		public void LoadAsset()
		{
			List<int> tmpKeys = new List<int>(m_AssetCallBackDic.Keys);
			for (int i =0;i< tmpKeys.Count;i++)
			{
				int tmpAssetIndex = tmpKeys[i];
				AssetKey tmpKey = (AssetKey)tmpAssetIndex;
				UnityEngine.Object assetLoaded = AssetDatabase.LoadMainAssetAtPath(m_AssetInfos[tmpKeys[i]].AssetPath);
				m_AssetCallBackDic[tmpAssetIndex]?.Invoke(tmpKey, assetLoaded);
				m_AssetCallBackDic[tmpAssetIndex] = null;
			}
			
		}

		/// <summary>
		/// 实例化资源接口中间回调
		/// </summary>
		/// <param name="assetKey"></param>
		/// <param name="initObject"></param>
		private void OnLoadAssetCallBack(AssetKey assetKey,UnityEngine.Object initObject)
		{
			string assetKeyName = assetKey.ToString();
			if(m_AssetToGameObjectInstantiateData.ContainsKey(assetKeyName))
			{
				Queue<GameObjectInstantiateData> mGameObjectInstantiateQueue = m_AssetToGameObjectInstantiateData[assetKeyName];
				while (mGameObjectInstantiateQueue.Count > 0)
				{
					GameObjectInstantiateData gameObjectInstantiateData = mGameObjectInstantiateQueue.Dequeue();
					GameObject gameObject = null;

					if (initObject is GameObject)
					{
						gameObject = GameObject.Instantiate(initObject) as GameObject;
						if(!gameObjectInstantiateData.BasicData.IsWorldSpace)
						{
							if(gameObjectInstantiateData.BasicData.Parent != null)
							{
								gameObject.transform.SetParent(gameObjectInstantiateData.BasicData.Parent);
							}
						}
						gameObject.transform.localPosition = gameObjectInstantiateData.BasicData.Position;
						gameObject.transform.localScale = Vector3.one;
					}

					gameObjectInstantiateData.GameObjectCallback(assetKey, gameObject);

				}
			}
		}

		#region 资源加载接口
		public void InstantiateGameObjectAsync(AssetKey assetKey, Action<AssetKey, UnityEngine.Object> callback, InstantiateBasicData instantiateBasicData = default)
		{
			GameObjectInstantiateData gameObjectInstantiateData = new GameObjectInstantiateData() {BasicData = instantiateBasicData,GameObjectCallback = callback};
			string assetKeyName = assetKey.ToString();
			if (!m_AssetToGameObjectInstantiateData.ContainsKey(assetKeyName))
			{
				Queue<GameObjectInstantiateData> queue = new Queue<GameObjectInstantiateData>();
				m_AssetToGameObjectInstantiateData.Add(assetKeyName, queue);
			}

			m_AssetToGameObjectInstantiateData[assetKeyName].Enqueue(gameObjectInstantiateData);
			LoadAssetAsync(assetKey, OnLoadAssetCallBack);
		}

		public void LoadAssetAsync(AssetKey assetKey, Action<AssetKey, UnityEngine.Object> callback)
		{
			MDebug.Assert(callback != null, LOG_TAG, "callback == null");
			MDebug.Log(LOG_TAG, $"LoadAssetAsync({assetKey})");
			int assetIndex = (int)assetKey;
			if(!m_AssetCallBackDic.ContainsKey(assetIndex))
			{
				Action<AssetKey, UnityEngine.Object> m_CallBack = null;
				m_AssetCallBackDic.Add(assetIndex, m_CallBack);
			}
			m_AssetCallBackDic[assetIndex] += callback;
		}

		public void ReleaseGameObjectAsync(GameObject asset)
		{
			MDebug.Assert(asset != null, LOG_TAG, "GameObject You Want To Release Is Null!");
			GameObject.Destroy(asset);
		}

		public void UnloadAssetAsync(UnityEngine.Object asset)
		{
			MDebug.Assert(asset != null, LOG_TAG, "Asset You Want To Release Is Null!");
			if (!(asset is GameObject) && !(asset is Component))
			{
				Resources.UnloadAsset(asset);
			}
		}

		public void LoadAssetBundleForFairyGUIAsync(string assetBundleName, Action<AssetBundle> callback)
		{
			//do nothing
		}
		#endregion

		public override void OnLateUpdate(float deltaTime)
		{
			base.OnLateUpdate(deltaTime);
			LoadAsset();
		}

		public override void OnRelease()
		{
			if(m_AssetCallBackDic != null)
			{
				m_AssetCallBackDic.Clear();
				m_AssetCallBackDic = null;
			}
			
			if(m_AssetToGameObjectInstantiateData !=null)
			{
				m_AssetToGameObjectInstantiateData.Clear();
				m_AssetToGameObjectInstantiateData = null;
			}
			ms_EditorAssetManager = null;
			m_AssetInfos = null;

		}


		/// <summary>
		/// 用于缓存需要实例化GameObject的数据
		/// </summary>
		private struct GameObjectInstantiateData
		{
			public InstantiateBasicData BasicData;
			public Action<AssetKey, UnityEngine.Object> GameObjectCallback;
		}
	}
}


