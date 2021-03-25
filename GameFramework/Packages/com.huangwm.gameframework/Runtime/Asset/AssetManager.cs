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
    public class AssetManager : BaseBehaviour
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

        public AssetManager()
             : base("AssetManager", (int)BehaviourPriority.AssetManager, BehaviourGroup.Default.ToString())
        {
            ms_AssetManager = this;

            // 在初始化完成前停用Update
            SetEnable(false);
        }

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

            // 恢复Update
            SetEnable(true);
        }

        public void LoadAssetAsync(AssetKey assetKey, Action<AssetKey, UnityEngine.Object> callback)
        {
            MDebug.Log(LOG_TAG, $"LoadAssetAsync({assetKey})");
            MDebug.Assert(callback != null, LOG_TAG, "callback != null");

            int assetIndex = (int)assetKey;

            if (m_AssetHandlers[assetIndex] == null)
            {
                m_AssetHandlers[assetIndex] = m_AssetHandlerPool.Alloc();
                m_AssetHandlers[assetIndex].SetAssetKey(assetKey);
            }

            AssetAction assetAction = m_AssetHandlers[assetIndex].AddReference(callback);
            int bundleIndex = m_AssetInfos[assetIndex].BundleIndex;
            int[] dependencyBundleIndexs = m_BundleInfos[bundleIndex].DependencyBundleIndexs;
            switch (assetAction)
            {
                case AssetAction.RequestLoadBundle:
                    
                    m_AssetHandlers[assetIndex].AddNeedLoadBundlesCount(dependencyBundleIndexs.Length + 1);
                    for (int iBundle = 0; iBundle < dependencyBundleIndexs.Length; iBundle++)
                    {
                        int iterDependenceBundleIndex = dependencyBundleIndexs[iBundle];
                        TryLoadBundleForLoadAsset(iterDependenceBundleIndex, assetIndex);
                    }
                    TryLoadBundleForLoadAsset(bundleIndex, assetIndex);
                    break;
                case AssetAction.Load:
                case AssetAction.Unload:
                case AssetAction.LoadedCallback:
                    AddAssetActionRequest(assetIndex, assetAction);
                    break;
                case AssetAction.Reload:
                    m_BundleHandlers[bundleIndex].AddReference();
                    for (int iBundle = 0; iBundle < dependencyBundleIndexs.Length; iBundle++)
                    {
                        m_BundleHandlers[dependencyBundleIndexs[iBundle]].AddReference();
                    }
                    
                    AddAssetActionRequest(assetIndex, AssetAction.LoadedCallback);
                    break;
                default:
                    MDebug.Assert(false, LOG_TAG, "Asset Not Support AssetAction: " + assetAction);
                    break;
            }
        }

        public void ReleaseAssetAsync(UnityEngine.Object asset)
        {
            if (!asset)
            {
                return;
            }

            if (m_AssetToAssetHandlerMap.TryGetValue(asset, out AssetHandler assetHandler))
            {
                AssetAction assetAction = assetHandler.RemoveReference();
                if (assetAction == AssetAction.Unload)
                {
                    int bundleIndex = m_AssetInfos[assetHandler.GetAssetIndex()].BundleIndex;
                    TryReleaseBundleForReleaseAsset(bundleIndex);
                }
            }
            else
            {
                MDebug.Assert(false, LOG_TAG, $"Release Error,  Not Contains Asset : {asset.name}");
            }
        }

        private void TryReleaseBundleForReleaseAsset(int bundleIndex)
        {
            BundleAction bundleAction = m_BundleHandlers[bundleIndex].RemoveReference();

            if (bundleAction == BundleAction.Unload)
            {
                m_BundleActionRequests.Enqueue(new BundleActionRequest(bundleIndex, bundleAction));
            }

            int[] dependenceBundleIndex = m_BundleInfos[bundleIndex].DependencyBundleIndexs;
            for (int iBundle = dependenceBundleIndex.Length - 1; iBundle > -1; iBundle--)
            {
                BundleAction dependenceBundleAction = m_BundleHandlers[dependenceBundleIndex[iBundle]].RemoveReference();
                if (bundleAction == BundleAction.Unload)
                {
                    m_BundleActionRequests.Enqueue(new BundleActionRequest(bundleIndex, bundleAction));
                }
            }
        }

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

        private BundleHandler TryLoadBundleForLoadAsset(int bundleIndex, int assetIndex)
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
                bundleHandler.AddLoadedCallback(m_AssetHandlers[assetIndex].OnBundleLoaded);
                m_BundleActionRequests.Enqueue(new BundleActionRequest(bundleIndex, bundleAction));

                MDebug.LogVerbose(LOG_TAG, $"Add load bundle action. Bundle:({m_BundleInfos[bundleIndex].BundleName}) Asset:({(AssetKey)assetIndex})");
            }
            else if (bundleAction == BundleAction.Null)
            {
                m_AssetHandlers[assetIndex].OnBundleLoaded(m_BundleInfos[bundleIndex].BundleName);
            }
            else
            {
                MDebug.Assert(false, "AsestBundle", "Not support BundleAction: " + bundleAction);
            }

            return bundleHandler;
        }

        private void AddAssetActionRequest(int assetIndex, AssetAction assetAction)
        {
            m_AssetActionRequests.Enqueue(new AssetActionRequest(assetIndex, assetAction));
        }

        private void AddAssetActionRequest(AssetKey assetKey, AssetAction assetAction)
        {
            AddAssetActionRequest((int)assetKey, assetAction);
        }

        private string GetBundlePath(int bundleIndex)
        {
            return Path.Combine(m_RootBundlePath, m_BundleInfos[bundleIndex].BundleName);
        }

        private void LoadAssetAsync(int assetIndex, Action<AsyncOperation> callback)
        {
            m_BundleHandlers[m_AssetInfos[assetIndex].BundleIndex].LoadAssetAsync(m_AssetInfos[assetIndex].AssetPath, callback);
        }

        private bool IsAssetDirectDependecneBundleLoaded(AssetKey assetKey)
        {
            BundleHandler bundleHandler = m_BundleHandlers[m_AssetInfos[(int)assetKey].BundleIndex];
            return bundleHandler != null ? bundleHandler.IsLoaded() : false;
        }

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

                m_ReferenceCount--;
                MDebug.LogVerbose(LOG_TAG, $"Bundle Remove Reference：{ms_AssetManager.m_BundleInfos[m_BundleIndex].BundleName}， Reference Count： {m_ReferenceCount}");
                MDebug.Assert(m_ReferenceCount >= 0, LOG_TAG, "m_ReferenceCount >= 0");
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

            public void AddLoadedCallback(Action<string> onBundleLoaded)
            {
                m_OnBundleLoaded += onBundleLoaded;
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

                MDebug.Assert(m_Bundle != null, LOG_TAG, "m_Bundle != null");
                m_Bundle.Unload(false);
                m_Bundle = null;

                return true;
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
                //Nothing To Do
            }

            public void OnRelease()
            {
                m_BundleIndex = -1;
                m_Bundle = null;
                m_BundleState = BundleState.NotLoad;
                m_ReferenceCount = 0;
                m_OnBundleLoaded = default;

                //TODO 释放相关资源
            }

            public bool IsLoaded()
            {
                return m_BundleState == BundleState.Loaded || m_BundleState == BundleState.NeedUnload;
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
                    m_AssetState = AssetState.WaitLoad;
                    ms_AssetManager.AddAssetActionRequest(m_AssetKey, AssetAction.Load);
                }
            }

            public void AddNeedLoadBundlesCount(int needLoadBundleCount = 1)
            {
                m_RemainLoadBundleCount += 1;
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
                        return AssetAction.RequestLoadBundle;
                    case AssetState.WaitLoad:
                    case AssetState.Loading:
                        return AssetAction.Null;
                    case AssetState.NeedUnload:
                        MDebug.Assert(m_Asset != null, LOG_TAG, "m_Asset != null");
                        m_AssetState = AssetState.Loaded;
                        return AssetAction.Reload;
                    default:
                        MDebug.Assert(false, LOG_TAG, "Asset Not Support AssetState");
                        return AssetAction.Null;
                }
            }

            public AssetAction RemoveReference()
            {
                m_ReferenceCount--;
                MDebug.LogVerbose(LOG_TAG, $"Asset Remove Reference：{m_AssetKey}， Reference Count： {m_ReferenceCount}");
                MDebug.Assert(m_ReferenceCount >= 0, LOG_TAG, "m_ReferenceCount >= 0");
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

                Resources.UnloadAsset(m_Asset);

                m_AssetState = AssetState.NotLoad;
                m_OnAssetLoaded = null;
                m_Asset = null;
                m_ReferenceCount = 0;
                m_RemainLoadBundleCount = 0;

                return true;
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
                //TODO
            }

            public void OnRelease()
            {
                //TODO
                m_ReferenceCount = 0;
                m_RemainLoadBundleCount = 0;

                m_AssetState = AssetState.NotLoad;
                m_Asset = null;
                m_OnAssetLoaded = default;
            }
        }

        private enum AssetState
        {
            NotLoad,
            WaitLoad,
            Loading,
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
            Null,
            Load,
            Unload,
            Reload,
            RequestLoadBundle,
            LoadedCallback,
        }
    }
}