using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GF.Asset.Build;
using GF.Common.Debug;
using GF.Core;
using GF.Core.Behaviour;
using LitJson;
using UnityEngine;

namespace GF.Asset.Address
{
    public class AssetManager : BaseBehaviour
    {
        private static string ms_BundlePath;

        private Dictionary<string, string[]> m_BundleMap;
        private Dictionary<string, AssetInfo> m_AssetMap;

        private Dictionary<string, BundleInfo> m_BundleInfos;

        private Stack<BundleActionRequest> m_BundleActionRequestsCahce;
        private Queue<BundleActionRequest> m_BundleActionRequests;

        public AssetManager()
             : base("AssetManager", (int)BehaviourPriority.AssetManager, BehaviourGroup.Default.ToString())
        {
            // 在初始化完成前停用Update
            SetEnable(false);
        }

        public IEnumerator InitializeAsync(KernelInitializeData initializeData)
        {
            m_BundleInfos = new Dictionary<string, BundleInfo>();
            m_BundleActionRequestsCahce = new Stack<BundleActionRequest>();
            m_BundleActionRequests = new Queue<BundleActionRequest>();
            ms_BundlePath = initializeData.BundlePath;

            //加载Bundle依赖文件
            string bundleMapJson = string.Empty;
            try
            {
                bundleMapJson = File.ReadAllText(initializeData.BundleMapFile);
                m_BundleMap = JsonMapper.ToObject<Dictionary<string, string[]>>(new JsonReader(bundleMapJson));
            }
            catch (Exception e)
            {
                MDebug.LogError("AssetBundle", $"解析BundleMap:({initializeData.BundleMapFile})失败。\nJson:({bundleMapJson})\n\n{e.ToString()}");
            }
            yield return null;

            //加载AssetMap
            string assetMapJson = string.Empty;
            try
            {
                assetMapJson = File.ReadAllText(initializeData.AssetMapFile);
                m_AssetMap = JsonMapper.ToObject<Dictionary<string, AssetInfo>>(new JsonReader(assetMapJson));
            }
            catch (Exception e)
            {
                MDebug.LogError("AssetBundle", $"解析AssetMap:({initializeData.AssetMapFile})失败。\nJson:({assetMapJson})\n\n{e.ToString()}");
            }
            yield return null;

            // 恢复Update
            SetEnable(true);
        }

        public void LoadAssetAsync(string key, Action<string, UnityEngine.Object> callback)
        {
            if (m_AssetMap.TryGetValue(key, out AssetInfo assetInfo))
            {
                string bundleName = assetInfo.BundleName;
                LoadBundleAndDependenciesAsync(bundleName);
            }
            else
            {
                MDebug.LogError("AssetBundle", $"Not exists asset Key:({key})");
                callback?.Invoke(key, null);
            }
        }

        private void LoadBundleAndDependenciesAsync(string bundleName)
        {
            InternalLoadBundleAndDependenciesAsync(bundleName);

            while(m_BundleActionRequestsCahce.Count > 0)
            {
                m_BundleActionRequests.Enqueue(m_BundleActionRequestsCahce.Pop());
            }
        }

        private void InternalLoadBundleAndDependenciesAsync(string bundleName)
        {
            if (!m_BundleInfos.TryGetValue(bundleName, out BundleInfo bundleInfo))
            {
                bundleInfo = new BundleInfo(bundleName);
                m_BundleInfos[bundleName] = bundleInfo;
            }

            TryAddBundleActionToCache(bundleName, bundleInfo.AddReference());

            if (m_BundleMap.TryGetValue(bundleName, out string[] dependenciesBundle))
            {
                for (int iBundle = 0; iBundle < dependenciesBundle.Length; iBundle++)
                {
                    string iterDependencyBundleName = dependenciesBundle[iBundle];
                    InternalLoadBundleAndDependenciesAsync(iterDependencyBundleName);
                }
            }
        }

        private void TryAddBundleActionToCache(string bundleName, BundleAction bundleAction)
        {
            if (bundleAction != BundleAction.Null)
            {
                m_BundleActionRequestsCahce.Push(new BundleActionRequest(bundleName, bundleAction));
            }
        }

        public override void OnLateUpdate(float deltaTime)
        {
            base.OnLateUpdate(deltaTime);


            //需要等待依赖包加载完成，才可以启动加载， 这里同帧内全部加载是有问题的
            while (m_BundleActionRequests.Count > 0)
            {
                BundleActionRequest bundleActionRequest = m_BundleActionRequests.Dequeue();
                if (m_BundleInfos.TryGetValue(bundleActionRequest.BundleName, out BundleInfo bundleInfo))
                {
                    bundleInfo.TryExecuteAction(bundleActionRequest.BundleAction);
                }
                else
                {
                    MDebug.Assert(false, "AssetBundle", "m_BundleInfos.TryGetValue(bundleActionRequest.BundleName, out BundleInfo bundleInfo)");
                }
            }
        }

        private struct BundleInfo
        {
            private string m_BundleName;
            private AssetBundle m_Bundle;
            private BundleState m_BundleState;
            private int m_ReferenceCount;

            public BundleInfo(string bundleName)
            {
                m_BundleName = bundleName;
                m_Bundle = null;

                m_BundleState = BundleState.NotLoad;
                m_ReferenceCount = 0;
            }

            /// <summary>
            /// 执行一个Action
            /// </summary>
            /// <returns>是否执行</returns>
            public bool TryExecuteAction(BundleAction bundleAction)
            {
                switch(bundleAction)
                {
                    case BundleAction.Load:
                        return TryLoadBundle();
                    case BundleAction.Unload:
                        return TryUnloadBundle();
                    default:
                        MDebug.Assert(false, "AssetBundle", "Not support BundleAction: " + bundleAction);
                        return false;
                }
            }

            private bool TryUnloadBundle()
            {
                if (m_BundleState != BundleState.NeedUnload)
                {
                    return false;
                }

                MDebug.Assert(m_Bundle != null, "AssetBundle", "m_Bundle != null");
                m_Bundle.Unload(false);
                m_Bundle = null;

                return true;
            }

            private bool TryLoadBundle()
            {
                if (m_BundleState != BundleState.NeedLoad)
                {
                    return false;
                }

                MDebug.Assert(m_Bundle == null, "AssetBundle", "m_Bundle == null");
                AssetBundle.LoadFromFileAsync(Path.Combine(ms_BundlePath, m_BundleName)).completed += OnLoadedBundle;
                m_BundleState = BundleState.Loading;

                return true;
            }

            private void OnLoadedBundle(AsyncOperation obj)
            {
                MDebug.Assert(m_Bundle == null && m_BundleState == BundleState.Loading, "AssetBundle", "m_Bundle == null && m_BundleState == BundleState.Loading"); 
                m_Bundle = (obj as AssetBundleCreateRequest).assetBundle;
                m_BundleState = BundleState.Loaded;
            }

            public BundleAction AddReference()
            {
                m_ReferenceCount++;
                switch(m_BundleState)
                {
                    case BundleState.NotLoad:
                        m_BundleState = BundleState.NeedLoad;
                        return BundleAction.Load;
                    case BundleState.Loaded:
                    case BundleState.Loading:
                    case BundleState.NeedLoad:
                        return BundleAction.Null;
                    case BundleState.NeedUnload:
                        m_BundleState = BundleState.NeedLoad;
                        return BundleAction.Null;
                    default:
                        MDebug.Assert(false, "AssetBundle", "Not support BundleState: " + m_BundleState);
                        return BundleAction.Null;
                }
            }

            public BundleAction RemoveReference()
            {
                m_ReferenceCount--;
                MDebug.Assert(m_ReferenceCount >= 0, "AssetBundle", "m_ReferenceCount >= 0");
                switch (m_BundleState)
                {
                    case BundleState.NotLoad:
                    case BundleState.NeedLoad:
                        MDebug.Assert(false, "AssetBundle", "BundleState.NotLoad");
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
                        MDebug.Assert(m_ReferenceCount > 0, "AssetBundle", "m_ReferenceCount > 0");
                        return BundleAction.Null;
                    default:
                        MDebug.Assert(false, "AssetBundle", "Not support BundleState: " + m_BundleState);
                        return BundleAction.Null;
                }
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
            public string BundleName;
            public BundleAction BundleAction;

            public BundleActionRequest(string bundleName, BundleAction bundleAction)
            {
                BundleName = bundleName;
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
    }
}