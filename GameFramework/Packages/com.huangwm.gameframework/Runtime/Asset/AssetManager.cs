using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GF.Common.Data;
using GF.Common.Debug;
using GF.Core;
using GF.Core.Behaviour;
using LitJson;
using UnityEngine;

namespace GF.Asset
{
	/// <summary>
	/// AssetBundle加载资源类
	/// </summary>
    public class AssetManager : BaseBehaviour,IAssetManager
    {
        private const string LOG_TAG = "AssetBundle";

        /// <summary>
        /// 仅供这个类内部使用
        /// </summary>
        private static AssetManager ms_AssetManager;

        private string m_RootBundlePath;

        #region Bundle
        private BundleInfo[] m_BundleInfos;
        private BundleHandler[] m_BundleHandlers;
        private Queue<BundleActionRequest> m_BundleActionRequests;
        private ObjectPool<BundleHandler> m_BundleHandlerPool;
        #endregion

        #region Asset
        private AssetInfo[] m_AssetInfos;
        private AssetHandler[] m_AssetHandlers;
        private Queue<AssetActionRequest> m_AssetActionRequests;
        private ObjectPool<AssetHandler> m_AssetHandlerPool;
        #endregion

        private Dictionary<UnityEngine.Object, AssetHandler> m_AssetToAssetHandlerMap;
		/// <summary>
		/// 实例化GameObject缓存数据
		/// </summary>
		private Dictionary<string, Queue<GameObjectInstantiateData>> m_AssetToGameObjectInstantiateData;

		public AssetManager()
             : base("AssetManager", (int)BehaviourPriority.AssetManager, BehaviourGroup.Default.ToString())
        {
            ms_AssetManager = this;

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
            //加载Bundle依赖文件
            string bundleInfosJson = string.Empty;
            try
            {
                bundleInfosJson = File.ReadAllText(initializeData.BundleMapFile);
                m_BundleInfos = JsonMapper.ToObject<BundleInfo[]>(new JsonReader(bundleInfosJson));
            }
            catch (Exception e)
            {
                MDebug.LogError(LOG_TAG, $"解析BundleMap:({initializeData.BundleMapFile})失败。\nJson:({bundleInfosJson})\n\n{e.ToString()}");
            }

            yield return null;

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
            yield return null;

            m_RootBundlePath = initializeData.BundlePath;

            m_BundleActionRequests = new Queue<BundleActionRequest>();
            m_BundleHandlerPool = new ObjectPool<BundleHandler>();
            m_BundleHandlers = new BundleHandler[m_BundleInfos.Length];

            m_AssetActionRequests = new Queue<AssetActionRequest>();
            m_AssetHandlerPool = new ObjectPool<AssetHandler>();
            m_AssetHandlers = new AssetHandler[m_AssetInfos.Length];
            m_AssetToAssetHandlerMap = new Dictionary<UnityEngine.Object, AssetHandler>(m_AssetInfos.Length);
			m_AssetToGameObjectInstantiateData = new Dictionary<string, Queue<GameObjectInstantiateData>>();

			// 恢复Update
			SetEnable(true);
        }

		/// <summary>
		/// Bundle加载完成后，根据assetIndex异步加载资源
		/// </summary>
		/// <param name="assetIndex"></param>
		/// <param name="callback"></param>
		public void LoadAssetAsync(int assetIndex, Action<AsyncOperation> callback)
		{
			m_BundleHandlers[m_AssetInfos[assetIndex].BundleIndex].LoadAssetAsync(m_AssetInfos[assetIndex].AssetPath, callback);
		}

		#region 生命周期函数

		public override void OnLateUpdate(float deltaTime)
        {
            base.OnLateUpdate(deltaTime);

            while (m_BundleActionRequests.Count > 0)
            {
                BundleActionRequest bundleActionRequest = m_BundleActionRequests.Dequeue();
                if (bundleActionRequest.BundleIndex >= 0 && bundleActionRequest.BundleIndex < m_BundleHandlers.Length)
                {
                    m_BundleHandlers[bundleActionRequest.BundleIndex].TryExecuteAction(bundleActionRequest.BundleAction);
                }
                else
                {
                    MDebug.Assert(false, LOG_TAG, "m_BundleHandlers.TryGetValue(bundleActionRequest.BundleName, out BundleHandler bundleHandler)");
                }
            }

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
		/// 释放,等同于OnDestroy()
		/// </summary>
		public override void OnRelease()
		{
			#region 释放Bundle相关
			if (m_BundleHandlers != null && m_BundleHandlerPool != null)
			{
				for (int i = 0; i < m_BundleHandlers.Length; i++)
				{
					if (m_BundleHandlers[i] != null)
					{
						m_BundleHandlerPool.Release(m_BundleHandlers[i]);
						m_BundleHandlers[i] = null;
					}

				}
				m_BundleHandlerPool = null;
				m_BundleHandlers = null;
			}

			if (m_BundleActionRequests != null)
			{
				m_BundleActionRequests.Clear();
				m_BundleActionRequests = null;
			}

			m_BundleInfos = null;

			#endregion
			#region 释放资源相关
			if (m_AssetHandlers != null && m_AssetHandlerPool != null)
			{
				for (int i = 0; i < m_AssetHandlers.Length; i++)
				{
					if (m_AssetHandlers[i] != null)
					{
						m_AssetHandlerPool.Release(m_AssetHandlers[i]);
						m_AssetHandlers[i] = null;
					}

				}
				m_AssetHandlerPool = null;
				m_AssetHandlers = null;
			}

			if (m_AssetActionRequests != null)
			{
				m_AssetActionRequests.Clear();
				m_AssetActionRequests = null;
			}

			if (m_AssetToAssetHandlerMap != null)
			{
				m_AssetToAssetHandlerMap.Clear();
				m_AssetToAssetHandlerMap = null;
			}

			if (m_AssetToGameObjectInstantiateData != null)
			{
				m_AssetToGameObjectInstantiateData.Clear();
				m_AssetToGameObjectInstantiateData = null;
			}
			#endregion
			ms_AssetManager = null;
			m_AssetInfos = null;

		}
		#endregion

		/// <summary>
		/// 包含Bundle是否需要加载判断，添加资源引用计数，并在尚未加载Bundle时，设置加载完成时回调
		/// </summary>
		/// <param name="bundleIndex"></param>
		/// <param name="assetIndex"></param>
		private void LoadBundleForLoadAsset(int bundleIndex, int assetIndex)
        {
            BundleHandler bundleHandler = m_BundleHandlers[bundleIndex];
            if (bundleHandler == null)
            {
                bundleHandler = m_BundleHandlerPool.Alloc();
                bundleHandler.SetBundleIndex(bundleIndex);
                m_BundleHandlers[bundleIndex] = bundleHandler;
            }

            BundleAction bundleAction = bundleHandler.AddReference();
            if (bundleAction == BundleAction.Load)
            {
                m_BundleActionRequests.Enqueue(new BundleActionRequest(bundleIndex, bundleAction));

                MDebug.LogVerbose(LOG_TAG, $"Add load bundle action. Bundle:({m_BundleInfos[bundleIndex].BundleName}) Asset:({(AssetKey)assetIndex})");
            }
            else if (bundleAction == BundleAction.Null)
            {
                // Dont need handle
            }
            else
            {
                MDebug.Assert(false, "AsestBundle", "Not support BundleAction: " + bundleAction);
            }

            bundleHandler.TryAddDependencyAsset(m_AssetHandlers[assetIndex]);
        }

		/// <summary>
		/// 资源操作请求，统一在LateUpdate中处理
		/// </summary>
		/// <param name="assetIndex"></param>
		/// <param name="assetAction"></param>
        private void AddAssetActionRequest(int assetIndex, AssetAction assetAction)
        {
            m_AssetActionRequests.Enqueue(new AssetActionRequest(assetIndex, assetAction));
        }

        private void AddAssetActionRequest(AssetKey assetKey, AssetAction assetAction)
        {
            AddAssetActionRequest((int)assetKey, assetAction);
        }

		/// <summary>
		/// Bundle包路径
		/// </summary>
		/// <param name="bundleIndex"></param>
		/// <returns></returns>
        private string GetBundlePath(int bundleIndex)
        {
            return Path.Combine(m_RootBundlePath, m_BundleInfos[bundleIndex].BundleName);
        }

		/// <summary>
		/// 卸载资源时，对其以来的bundle包减少依赖计数
		/// </summary>
		/// <param name="assetIndex"></param>
		public void RemoveAssetDependencyBundleReference(int assetIndex)
		{
			// TODO
			int bundleIndex = m_AssetInfos[assetIndex].BundleIndex;
			int[] dependencyBundleIndexs = m_BundleInfos[bundleIndex].DependencyBundleIndexs;
			for (int iBundle = 0; iBundle < dependencyBundleIndexs.Length; iBundle++)
			{
				int iterDependencyBundleIndex = dependencyBundleIndexs[iBundle];
				RemoveAssetDependency(iterDependencyBundleIndex, assetIndex);
			}
			RemoveAssetDependency(bundleIndex, assetIndex);

		}

		/// <summary>
		/// 减少指定Bundle包中的资源引用计数
		/// </summary>
		/// <param name="bundleIndex"></param>
		private void RemoveAssetDependency(int bundleIndex,int assetIndex)
		{
			BundleHandler bundleHandler = m_BundleHandlers[bundleIndex];
			if (bundleHandler != null)
			{
				BundleAction bundleAction = bundleHandler.RemoveReference();
				if (bundleAction == BundleAction.Unload)
				{
					m_BundleActionRequests.Enqueue(new BundleActionRequest(bundleIndex, bundleAction));

					MDebug.LogVerbose(LOG_TAG, $"Add remove bundle action. Bundle:({m_BundleInfos[bundleIndex].BundleName}) Asset:({(AssetKey)assetIndex})");
				}
				else if (bundleAction == BundleAction.Null)
				{
					// Dont need handle
				}
				else
				{
					MDebug.Assert(false, "AsestBundle", "Not support BundleAction: " + bundleAction);
				}
			}
		}

		/// <summary>
		/// 实例化资源接口中间回调
		/// </summary>
		/// <param name="assetKey"></param>
		/// <param name="initObject"></param>
		private void OnLoadAssetCallBack(AssetKey assetKey, UnityEngine.Object initObject)
		{
			string assetKeyName = assetKey.ToString();
			if (m_AssetToGameObjectInstantiateData.ContainsKey(assetKeyName))
			{
				Queue<GameObjectInstantiateData> mGameObjectInstantiateQueue = m_AssetToGameObjectInstantiateData[assetKeyName];
				while (mGameObjectInstantiateQueue.Count > 0)
				{
					GameObjectInstantiateData gameObjectInstantiateData = mGameObjectInstantiateQueue.Dequeue();
					GameObject gameObject = null;

					if (initObject is GameObject)
					{
						gameObject = GameObject.Instantiate(initObject) as GameObject;
						if (!gameObjectInstantiateData.basicData.IsWorldSpace)
						{
							if (gameObjectInstantiateData.basicData.Parent != null)
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

		#region 资源加载接口,外部调用

		/// <summary>
		/// 异步加载资源
		/// </summary>
		/// <param name="assetKey"></param>
		/// <param name="callback"></param>
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
					int[] dependencyBundleIndexs = m_BundleInfos[bundleIndex].DependencyBundleIndexs;
					for (int iBundle = 0; iBundle < dependencyBundleIndexs.Length; iBundle++)
					{
						int iterDependencyBundleIndex = dependencyBundleIndexs[iBundle];
						LoadBundleForLoadAsset(iterDependencyBundleIndex, assetIndex);
					}
					LoadBundleForLoadAsset(bundleIndex, assetIndex);

					//全部依赖Bundle加载完成?

					if (assetHandler.GetRemainLoadBundleCount() == 0)
					{
						AddAssetActionRequest(assetIndex, AssetAction.Load);
					}
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

		/// <summary>
		/// 卸载资源
		/// </summary>
		/// <param name="asset"></param>
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

		/// <summary>
		/// 实例化GameObject
		/// </summary>
		/// <param name="assetKey"></param>
		/// <param name="callback"></param>
		/// <param name="instantiateBasicData"></param>
		public void InstantiateGameObjectAsync(AssetKey assetKey, Action<AssetKey, UnityEngine.Object> callback, InstantiateBasicData instantiateBasicData = default)
		{
			GameObjectInstantiateData gameObjectInstantiateData = new GameObjectInstantiateData() { basicData = instantiateBasicData, gameObjectCallback = callback };
			string assetKeyName = assetKey.ToString();
			if (!m_AssetToGameObjectInstantiateData.ContainsKey(assetKeyName))
			{
				Queue<GameObjectInstantiateData> queue = new Queue<GameObjectInstantiateData>();
				m_AssetToGameObjectInstantiateData.Add(assetKeyName, queue);
			}

			m_AssetToGameObjectInstantiateData[assetKeyName].Enqueue(gameObjectInstantiateData);
			LoadAssetAsync(assetKey, OnLoadAssetCallBack);
		}

		/// <summary>
		/// 仅仅适用销毁实例化的GameObject
		/// </summary>
		/// <param name="asset"></param>
		public void ReleaseGameObjectAsync(GameObject asset)
		{
			MDebug.Assert(asset != null, LOG_TAG, "GameObject You Want To Release Is Null!");
			if (asset == null) return;
			GameObject.Destroy(asset);
		}



		#endregion
		/// <summary>
		/// 用于缓存需要实例化GameObject的数据
		/// </summary>
		public struct GameObjectInstantiateData
		{
			public InstantiateBasicData basicData;
			public Action<AssetKey, UnityEngine.Object> gameObjectCallback;
		}

		#region AssetBundle 相关管理类，请求，指令，状态
		private class BundleHandler : IObjectPoolItem
        {
            private int m_BundleIndex;
            private AssetBundle m_Bundle;
            private BundleState m_BundleState;
            private int m_ReferenceCount;
            private Action<string> m_OnBundleLoaded;

            public BundleHandler()
            {
                m_Bundle = null;

                m_BundleState = BundleState.NotLoad;
                m_ReferenceCount = 0;

                m_OnBundleLoaded = default;
            }

            public BundleHandler(int index)
            {
                m_BundleIndex = index;
                m_Bundle = null;

                m_BundleState = BundleState.NotLoad;
                m_ReferenceCount = 0;

                m_OnBundleLoaded = default;
            }

            public void SetBundleIndex(int index)
            {
                m_BundleIndex = index;
            }

            /// <summary>
            /// 执行一个Action
            /// </summary>
            /// <returns>是否执行</returns>
            public bool TryExecuteAction(BundleAction bundleAction)
            {
                switch (bundleAction)
                {
                    case BundleAction.Load:
                        return TryLoadBundle();
                    case BundleAction.Unload:
                        return TryUnloadBundle();
                    default:
                        MDebug.Assert(false, LOG_TAG, "Not support BundleAction: " + bundleAction);
                        return false;
                }
            }

            public BundleAction AddReference()
            {
                m_ReferenceCount++;

                MDebug.LogVerbose(LOG_TAG, $"Bundle Add Reference：{ms_AssetManager.m_BundleInfos[m_BundleIndex].BundleName}， Reference Count： {m_ReferenceCount}");

                switch (m_BundleState)
                {
                    case BundleState.NotLoad:
                        m_BundleState = BundleState.NeedLoad;
                        return BundleAction.Load;
                    case BundleState.Loaded:
                    case BundleState.Loading:
                    case BundleState.NeedLoad:
                        return BundleAction.Null;
                    case BundleState.NeedUnload:
                        m_BundleState = BundleState.Loaded;
                        return BundleAction.Null;
                    default:
                        MDebug.Assert(false, LOG_TAG, "Not support BundleState: " + m_BundleState);
                        return BundleAction.Null;
                }
            }

            public BundleAction RemoveReference()
            {         
                MDebug.LogVerbose(LOG_TAG, $"Bundle Remove Reference：{ms_AssetManager.m_BundleInfos[m_BundleIndex].BundleName}， Reference Count： {m_ReferenceCount}");
                MDebug.Assert(m_ReferenceCount > 0, LOG_TAG, "m_ReferenceCount = 0, Cant Remove!");
				m_ReferenceCount--;
				switch (m_BundleState)
                {
                    case BundleState.NotLoad:
                    case BundleState.NeedLoad:
                        MDebug.Assert(false, LOG_TAG, "BundleState.NotLoad");
                        return BundleAction.Null;
                    case BundleState.Loaded:
                        if (m_ReferenceCount == 0)
                        {
                            m_BundleState = BundleState.NeedUnload;
                            return BundleAction.Unload;
                        }
                        else
                        {
                            return BundleAction.Null;
                        }
                    case BundleState.Loading:
                        MDebug.Assert(m_ReferenceCount > 0, LOG_TAG, "m_ReferenceCount > 0");
                        return BundleAction.Null;
                    default:
                        MDebug.Assert(false, LOG_TAG, "Not support BundleState: " + m_BundleState);
                        return BundleAction.Null;
                }
            }

            public void TryAddDependencyAsset(AssetHandler assetHandler)
            {
                if (m_BundleState != BundleState.Loaded)
                {
                    m_OnBundleLoaded += assetHandler.OnBundleLoaded;
                    assetHandler.AddNeedLoadBundle();
                }
            }

            public void LoadAssetAsync(string assetPath, Action<AsyncOperation> callback)
            {
                MDebug.Assert(m_BundleState == BundleState.Loaded && m_Bundle != null, LOG_TAG, "m_BundleState == BundleState.Loaded && m_Bundle != null");
                m_Bundle.LoadAssetAsync(assetPath).completed += callback;
            }

            private bool TryUnloadBundle()
            {
                if (m_BundleState != BundleState.NeedUnload)
                {
                    return false;
                }

                MDebug.Assert(m_Bundle != null, LOG_TAG, "m_Bundle == null");
				UnloadBundleForce();
				return true;
            }

			private void UnloadBundleForce()
			{
				m_Bundle.Unload(false);
				m_Bundle = null;
			}

            private bool TryLoadBundle()
            {
                MDebug.Log(LOG_TAG, $"Start Load Bundle {ms_AssetManager.m_BundleInfos[m_BundleIndex].BundleName}");

                if (m_BundleState != BundleState.NeedLoad)
                {
                    return false;
                }

                MDebug.Assert(m_Bundle == null, LOG_TAG, "m_Bundle == null");
                AssetBundle.LoadFromFileAsync(ms_AssetManager.GetBundlePath(m_BundleIndex)).completed += OnLoadedBundle;
                m_BundleState = BundleState.Loading;

                return true;
            }

            private void OnLoadedBundle(AsyncOperation obj)
            {
                MDebug.Assert(m_Bundle == null && m_BundleState == BundleState.Loading, LOG_TAG, "m_Bundle == null && m_BundleState == BundleState.Loading");
                m_Bundle = (obj as AssetBundleCreateRequest).assetBundle;
                m_BundleState = BundleState.Loaded;

                MDebug.Log(LOG_TAG, $"Loaded Bundle {ms_AssetManager.m_BundleInfos[m_BundleIndex].BundleName}");

                m_OnBundleLoaded?.Invoke(ms_AssetManager.m_BundleInfos[m_BundleIndex].BundleName);
                m_OnBundleLoaded = null;
            }

            public void OnAlloc()
            {

            }

            public void OnRelease()
            {
                m_BundleIndex = -1;
				UnloadBundleForce();
				m_Bundle = null;
                m_BundleState = BundleState.NotLoad;
                m_ReferenceCount = 0;
                m_OnBundleLoaded = default;
            }

            private enum BundleState
            {
                /// <summary>
                /// 沒有加載
                /// </summary>
                NotLoad,
                /// <summary>
                /// 需要加載的
                /// </summary>
                NeedLoad,
                /// <summary>
                /// 正在加載的
                /// </summary>
                Loading,
                /// <summary>
                /// 已加載
                /// </summary>
                Loaded,
                /// <summary>
                /// 需要卸載
                /// </summary>
                NeedUnload,
            }
        }

        private struct BundleActionRequest
        {
            public int BundleIndex;
            public BundleAction BundleAction;

            public BundleActionRequest(int bundleIndex, BundleAction bundleAction)
            {
                BundleIndex = bundleIndex;
                BundleAction = bundleAction;
            }
        }

        private enum BundleAction
        {
            /// <summary>
            /// 无请求
            /// </summary>
            Null,
            /// <summary>
            /// 加载请求
            /// </summary>
            Load,
            /// <summary>
            /// 卸载请求
            /// </summary>
            Unload,
        }

		#endregion


		#region Asset管理相关类，状态，请求，指令
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

            public void OnBundleLoaded(string bundleName)
            {
                m_RemainLoadBundleCount--;
                MDebug.Assert(m_RemainLoadBundleCount >= 0, LOG_TAG, "m_RemainLoadBundleCount >= 0");
                if (m_RemainLoadBundleCount == 0)
                {
                    ms_AssetManager.AddAssetActionRequest(m_AssetKey, AssetAction.Load);
                }
            }

            public void AddNeedLoadBundle()
            {
                m_RemainLoadBundleCount++;
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

            public void OnAssetLoaded(AsyncOperation asyncOperation)
            {
                AssetBundleRequest assetBundleRequest = asyncOperation as AssetBundleRequest;
                m_Asset = assetBundleRequest.asset;
                m_AssetState = AssetState.Loaded;

                if (m_Asset)
                {
                    ms_AssetManager.m_AssetToAssetHandlerMap.Add(m_Asset, this);
                }

                ms_AssetManager.AddAssetActionRequest(m_AssetKey, AssetAction.LoadedCallback);

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
				if(m_Asset != null&& ms_AssetManager.m_AssetToAssetHandlerMap.ContainsKey(m_Asset))
				{
					ms_AssetManager.m_AssetToAssetHandlerMap.Remove(m_Asset);
				}
				//引用资源内存中可以Resources.UnloadAsset(XXXX)卸载,非引用类例如GameObject无引用使用unloadUnUsed();
				//todo,UnloadUnusedAsset比较消耗性能，需要配合GC使用，先GC最好两遍，在切换场景时使用最好
				if (m_Asset != null && !(m_Asset is GameObject) && !(m_Asset is Component))
				{
					Resources.UnloadAsset(m_Asset);
					
				}

				m_Asset = null;
				m_AssetState = AssetState.NotLoad;
				ms_AssetManager.RemoveAssetDependencyBundleReference((int)m_AssetKey);
			}

            private bool TryLoadAsset()
            {
                MDebug.Log(LOG_TAG, $"Start Load Asset : {m_AssetKey}");
                switch (m_AssetState)
                {
                    case AssetState.WaitLoad:
                        ms_AssetManager.LoadAssetAsync((int)m_AssetKey, OnAssetLoaded);
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