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
/*
* 备注：
* lamda导致闭包频繁分配内存
* int a ;
* xxx => ()
* {
* int b =a;
* }
* 
* 
*  xxx = new Temp();
* xxx.a = a;
* xxx.Handle
* 
* class Temp
* {
* int a ;
* 
* void Handle()
* {
* int b = a;
* }
* 
* }
* 
* */

// see XLuaManager.InitializePackage
// Start is called before the first frame update


//[Core.InitializePackage("XLuaManager", (int)Core.PackageProiority.XLuaManager)]
//internal static IEnumerator InitializePackage(Core.KernelInitializeData initializeData)
//{
// TODO check ini.use assetbundle
//	XLuaManager xluaManager = new XLuaManager();
//	Core.Kernel.LuaManager = xluaManager;
//	return xluaManager.InitializeAsync(initializeData);
//}

/* Init()
{
 load by key.

need (key to assetPath) file


}

LateUpdate()
{
handle callback or release
}

}
*/
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

		//为了保证Editor代码加载尽量和AssetManager保持一致
		#region Asset
		private AssetInfo[] m_AssetInfos;
		private AssetHandler[] m_AssetHandlers;
		private Queue<AssetActionRequest> m_AssetActionRequests;
		private ObjectPool<AssetHandler> m_AssetHandlerPool;
		#endregion

		private Dictionary<UnityEngine.Object, AssetHandler> m_AssetToAssetHandlerMap;
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

			m_AssetActionRequests = new Queue<AssetActionRequest>();
			m_AssetHandlerPool = new ObjectPool<AssetHandler>();
			m_AssetHandlers = new AssetHandler[m_AssetInfos.Length];
			m_AssetToAssetHandlerMap = new Dictionary<UnityEngine.Object, AssetHandler>(m_AssetInfos.Length);
			m_AssetToGameObjectInstantiateData = new Dictionary<string, Queue<GameObjectInstantiateData>>();
			yield return null;
			SetEnable(true);
		}

		public void LoadAssetAsync(int assetIndex, Action<UnityEngine.Object> callback)
		{
			UnityEngine.Object assetLoaded =  AssetDatabase.LoadMainAssetAtPath(m_AssetInfos[assetIndex].AssetPath);
			//模拟延时？
			callback?.Invoke(assetLoaded);
		}

		private void AddAssetActionRequest(int assetIndex, AssetAction assetAction)
		{
			m_AssetActionRequests.Enqueue(new AssetActionRequest(assetIndex, assetAction));
		}

		private void AddAssetActionRequest(AssetKey assetKey, AssetAction assetAction)
		{
			AddAssetActionRequest((int)assetKey, assetAction);
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
						if(!gameObjectInstantiateData.basicData.IsWorldSpace)
						{
							if(gameObjectInstantiateData.basicData.Parent != null)
							{
								gameObject.transform.SetParent(gameObjectInstantiateData.basicData.Parent);
							}
						}
						gameObject.transform.localPosition = gameObjectInstantiateData.basicData.Position;
						gameObject.transform.localScale = Vector3.one;
					}

					gameObjectInstantiateData.gameObjectCallback(assetKey, gameObject);

				}
			}
		}

		#region 资源加载接口
		public void InstantiateGameObjectAsync(AssetKey assetKey, Action<AssetKey, UnityEngine.Object> callback, InstantiateBasicData instantiateBasicData = default)
		{
			GameObjectInstantiateData gameObjectInstantiateData = new GameObjectInstantiateData() {basicData = instantiateBasicData,gameObjectCallback = callback};
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
			MDebug.Log(LOG_TAG, $"LoadAssetAsync({assetKey})");
			MDebug.Assert(callback != null, LOG_TAG, "callback != null");

			int assetIndex = (int)assetKey;

			AssetHandler assetHandler = m_AssetHandlers[assetIndex];
			if (assetHandler == null)
			{
				assetHandler = m_AssetHandlerPool.Alloc();
				assetHandler.SetAssetKey(assetKey);
				m_AssetHandlers[assetIndex] = assetHandler;
			}

			AssetAction assetAction = assetHandler.AddReference(callback);

			switch (assetAction)
			{
				case AssetAction.RequestLoadBundle:
					int bundleIndex = m_AssetInfos[assetIndex].BundleIndex;

					//Editor直接认为全部依赖加载完成
					AddAssetActionRequest(assetIndex, AssetAction.Load);
					break;
				case AssetAction.Load:
				case AssetAction.LoadedCallback:
					AddAssetActionRequest(assetIndex, assetAction);
					break;
				case AssetAction.Null:
					// Nothing To Do
					break;
				default:
					MDebug.Assert(false, LOG_TAG, "Asset Not Support AssetAction: " + assetAction);
					break;
			}

		}

		public void ReleaseGameObjectAsync(GameObject asset)
		{
			MDebug.Assert(asset != null, LOG_TAG, "GameObject You Want To Release Is Null!");
			if (asset == null) return;
			GameObject.Destroy(asset);
		}

		//和AssetManager保持一致
		public void UnloadAssetAsync(UnityEngine.Object asset)
		{
			if (asset == null || !m_AssetToAssetHandlerMap.ContainsKey(asset)) return;
			AssetHandler assetHandler = m_AssetToAssetHandlerMap[asset];

			if (assetHandler == null)
			{
				MDebug.Log(LOG_TAG, $"UnloadAssetAsync({assetHandler.GetAssetKey()})");

				AssetAction assetAction = assetHandler.RemoveReference();

				switch (assetAction)
				{
					case AssetAction.Unload:
						AddAssetActionRequest(assetHandler.GetAssetIndex(), AssetAction.Unload);
						break;
					default:
						MDebug.Log(LOG_TAG, "unload failed !");
						break;

				}
			}
		}
		#endregion

		#region 生命周期函数
		public override void OnLateUpdate(float deltaTime)
		{
			base.OnLateUpdate(deltaTime);
			while (m_AssetActionRequests.Count > 0)
			{
				AssetActionRequest assetActionRequest = m_AssetActionRequests.Dequeue();
				int assetHandlerIndex = assetActionRequest.AssetIndex;
				if (assetHandlerIndex >= 0 && assetHandlerIndex < m_AssetHandlers.Length)
				{
					m_AssetHandlers[assetHandlerIndex].TryExecuteAction(assetActionRequest.AssetAction);
				}
				else
				{
					MDebug.Assert(false, LOG_TAG, "m_BundleHandlers.TryGetValue(bundleActionRequest.BundleName, out BundleHandler bundleHandler)");
				}
			}
		}

		/// <summary>
		/// 释放
		/// </summary>
		public override void OnRelease()
		{
			
			if(m_AssetHandlers != null&&m_AssetHandlerPool != null)
			{
				for(int i = 0;i<m_AssetHandlers.Length;i++)
				{
					if(m_AssetHandlers[i] != null)
					{
						m_AssetHandlerPool.Release(m_AssetHandlers[i]);
						m_AssetHandlers[i] = null;
					}
					
				}
				m_AssetHandlerPool = null;
				m_AssetHandlers = null;
			}

			if(m_AssetActionRequests != null)
			{
				m_AssetActionRequests.Clear();
				m_AssetActionRequests = null;
			}

			if(m_AssetToAssetHandlerMap != null)
			{
				m_AssetToAssetHandlerMap.Clear();
				m_AssetToAssetHandlerMap = null;
			}

			if(m_AssetToGameObjectInstantiateData !=null)
			{
				m_AssetToGameObjectInstantiateData.Clear();
				m_AssetToGameObjectInstantiateData = null;
			}
			ms_EditorAssetManager = null;
			m_AssetInfos = null;

		}

		#endregion

		#region 内部类 AssetHandler,AssetAction,AssetActionRequest,AssetState
		/// <summary>
		/// 用于缓存需要实例化GameObject的数据
		/// </summary>
		public struct GameObjectInstantiateData
		{
			public InstantiateBasicData basicData;
			public Action<AssetKey, UnityEngine.Object> gameObjectCallback;
		}

		/// <summary>
		/// 同AssetManager一致
		/// </summary>
		private class AssetHandler : IObjectPoolItem
		{
			private AssetKey m_AssetKey;
			private int m_ReferenceCount;
			private int m_RemainLoadBundleCount;
			private AssetState m_AssetState;
			private UnityEngine.Object m_Asset;
			private Action<AssetKey, UnityEngine.Object> m_OnAssetLoaded;

			public AssetHandler()
			{
				m_ReferenceCount = 0;
				m_RemainLoadBundleCount = 0;

				m_AssetState = AssetState.NotLoad;
				m_Asset = null;
				m_OnAssetLoaded = default;
			}

			public void SetAssetKey(AssetKey assetKey)
			{
				m_AssetKey = assetKey;
			}



			public int GetRemainLoadBundleCount()
			{
				return m_RemainLoadBundleCount;
			}

			public AssetState GetAssetState()
			{
				return m_AssetState;
			}

			public AssetAction AddReference(Action<AssetKey, UnityEngine.Object> callback)
			{

				m_OnAssetLoaded += callback;
				m_ReferenceCount++;

				MDebug.LogVerbose(LOG_TAG, $"Asset Add Reference：{m_AssetKey}， Reference Count： {m_ReferenceCount}");

				switch (m_AssetState)
				{
					case AssetState.Loaded:
						MDebug.Assert(m_Asset != null, LOG_TAG, "m_Asset != null");
						return AssetAction.LoadedCallback;
					case AssetState.NotLoad:
						m_AssetState = AssetState.WaitLoad;
						return AssetAction.RequestLoadBundle;
					case AssetState.WaitLoad:
					case AssetState.Loading:
						return AssetAction.Null;
					case AssetState.NeedUnload:
						MDebug.Assert(m_Asset != null, LOG_TAG, "m_Asset != null");
						m_AssetState = AssetState.Loaded;
						return AssetAction.LoadedCallback;
					default:
						MDebug.Assert(false, LOG_TAG, "Asset Not Support AssetState");
						return AssetAction.Null;
				}
			}

			public AssetAction RemoveReference()
			{
				MDebug.LogVerbose(LOG_TAG, $"Asset Remove Reference：{m_AssetKey}， Reference Count： {m_ReferenceCount}");
				MDebug.Assert(m_ReferenceCount > 0, LOG_TAG, "m_ReferenceCount = 0 cant remove");
				if (m_ReferenceCount == 0) return AssetAction.Null;
				m_ReferenceCount--;
				switch (m_AssetState)
				{
					case AssetState.WaitLoad:
					case AssetState.Loading:
						MDebug.Assert(false, LOG_TAG, "Asset Not Load But Remove Reference");
						return AssetAction.Null;
					case AssetState.Loaded:
						if (m_ReferenceCount == 0)
						{
							m_AssetState = AssetState.NeedUnload;
							return AssetAction.Unload;
						}
						else
						{
							return AssetAction.Null;
						}
					default:
						MDebug.Assert(false, LOG_TAG, "Asset Not Support AssetState");
						return AssetAction.Null;
				}
			}

			public void OnAssetLoaded(UnityEngine.Object asset)
			{
				
				m_Asset = asset;
				m_AssetState = AssetState.Loaded;

				if (m_Asset)
				{
					ms_EditorAssetManager.m_AssetToAssetHandlerMap.Add(m_Asset, this);
				}

				ms_EditorAssetManager.AddAssetActionRequest(m_AssetKey, AssetAction.LoadedCallback);

				MDebug.Log(LOG_TAG, $"Loaded Asset : {m_AssetKey}");
			}

			public bool TryExecuteAction(AssetAction assetAction)
			{
				switch (assetAction)
				{
					case AssetAction.Load:
						return TryLoadAsset();
					case AssetAction.Unload:
						return TryUnloadAsset();
					case AssetAction.LoadedCallback:
						try
						{
							m_OnAssetLoaded?.Invoke(m_AssetKey, m_Asset);
						}
						catch (Exception e)
						{
							MDebug.LogError(LOG_TAG, "LoadedCallBack Error \n\n" + e.ToString());
						}
						finally
						{
							m_OnAssetLoaded = null;
						}
						return true;
					default:
						MDebug.Assert(false, LOG_TAG, "Not support AssetAction: " + assetAction);
						return false;
				}
			}

			public AssetKey GetAssetKey()
			{
				return m_AssetKey;
			}

			public int GetAssetIndex()
			{
				return (int)m_AssetKey;
			}

			private bool TryUnloadAsset()
			{
				if (m_AssetState != AssetState.NeedUnload)
				{
					return false;
				}

				MDebug.Assert(m_ReferenceCount == 0 && m_Asset && m_OnAssetLoaded == null
					, LOG_TAG
					, "m_ReferenceCount == 0 && m_Asset && m_OnAssetLoaded == null");

				UnloadAssetForce();
				return true;
			}

			/// <summary>
			/// 强制卸载
			/// </summary>
			private void UnloadAssetForce()
			{
				if (m_Asset != null && ms_EditorAssetManager.m_AssetToAssetHandlerMap.ContainsKey(m_Asset))
				{
					ms_EditorAssetManager.m_AssetToAssetHandlerMap.Remove(m_Asset);
				}
				//引用资源内存中可以Resources.UnloadAsset(XXXX)卸载,非引用类例如GameObject无引用使用unloadUnUsed();
				if (m_Asset != null && !(m_Asset is GameObject) && !(m_Asset is Component))
				{
					Resources.UnloadAsset(m_Asset);
				}
				m_Asset = null;
				m_AssetState = AssetState.NotLoad;
			}

			private bool TryLoadAsset()
			{
				MDebug.Log(LOG_TAG, $"Start Load Asset : {m_AssetKey}");
				switch (m_AssetState)
				{
					case AssetState.WaitLoad:
						ms_EditorAssetManager.LoadAssetAsync((int)m_AssetKey, OnAssetLoaded);
						return true;
					default:
						MDebug.Assert(false, LOG_TAG, $"Not Surpport AssetState : {m_AssetState}");
						return false;
				}
			}

			public void OnAlloc()
			{
			}

			public void OnRelease()
			{
				m_ReferenceCount = 0;
				m_RemainLoadBundleCount = 0;
				//强制卸载
				UnloadAssetForce();
				m_AssetState = AssetState.NotLoad;
				m_Asset = null;
				m_OnAssetLoaded = null;
			}
		}

		private enum AssetState
		{
			//尚未加载
			NotLoad,
			//加载进行中
			WaitLoad,
			Loading,
			//已经加载完成
			Loaded,
			NeedUnload,
		}

		private struct AssetActionRequest
		{
			public int AssetIndex;
			public AssetAction AssetAction;

			public AssetActionRequest(int assetIndex, AssetAction assetAction)
			{
				AssetIndex = assetIndex;
				AssetAction = assetAction;
			}
		}

		

		private enum AssetAction
		{
			//nothing to do
			Null,
			//需要的所有bundle包已经加载完成(包含所有依赖Bundle)，去加载Asset
			Load,
			//卸载资源
			Unload,
			//需要进行加载资源，但是不确认Bundle情况(依赖都加载完成了，或者没有)
			RequestLoadBundle,
			//该资源已经被加载完成，直接回调资源
			LoadedCallback,
		}

		#endregion
	}
}


