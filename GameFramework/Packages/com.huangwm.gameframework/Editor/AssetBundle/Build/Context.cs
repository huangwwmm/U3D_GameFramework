using GF.Asset.AssetBundle.Build;
using GF.Common.Debug;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Asset.AssetBundle.Build
{
    public class Context
    {
        private Dictionary<string, List<string>> m_BundleToAssetPath = new Dictionary<string, List<string>>();
        private Dictionary<string, string> m_AssetPathToBundle = new Dictionary<string, string>();
        private Dictionary<string, HashSet<string>> m_AssetPathDependenciesToBundle;
        private Dictionary<string, AssetInfo> m_AssetKeyToAsset = new Dictionary<string, AssetInfo>();
        

        public void SetAssetDependenciesToBundle(Dictionary<string, HashSet<string>> assetDependenciesToBundle)
        {
            m_AssetPathDependenciesToBundle = assetDependenciesToBundle;
        }

        public Dictionary<string, HashSet<string>> GetAssetDependenciesToBundle()
        {
            return m_AssetPathDependenciesToBundle;
        }

        public bool IncludedAsset(string assetPath)
        {
            return m_AssetPathToBundle.ContainsKey(assetPath);
        }

        public void AddAsset(string assetPath, string assetKey, string bundleName)
        {
            bundleName = bundleName.ToLower();
            if (!string.IsNullOrEmpty(assetKey))
            {
                if (!m_AssetKeyToAsset.ContainsKey(assetKey))
                {
                    m_AssetKeyToAsset.Add(assetKey, new AssetInfo(assetPath, bundleName));
                }
                else
                {
                    AssetInfo alreadyIncludedAsset = m_AssetKeyToAsset[assetKey];
                    throw new Exception($"Already included asset Key({assetKey}):Path({assetPath})\nIncluded asset:({alreadyIncludedAsset.AssetPath})");
                }
            }

            if (!m_AssetPathToBundle.ContainsKey(assetPath))
            {
                m_AssetPathToBundle[assetPath] = bundleName;
                if (!m_BundleToAssetPath.TryGetValue(bundleName, out List<string> assets))
                {
                    assets = new List<string>();
                    m_BundleToAssetPath.Add(bundleName, assets);
                }

                assets.Add(assetPath);
            }
            else
            {
                throw new Exception("Already included: " + assetPath);
            }
        }

        public AssetBundleBuild[] GenerateAssetBundleBuild()
        {
            AssetBundleBuild[] assetBundleBuilds = new AssetBundleBuild[m_BundleToAssetPath.Count];
            int iAssetBundleBuild = 0;
            foreach (KeyValuePair<string, List<string>> kv in m_BundleToAssetPath)
            {
                string bundleName = kv.Key;
                List<string> assetPaths = kv.Value;

                AssetBundleBuild assetBundleBuild = new AssetBundleBuild();
                assetBundleBuild.assetBundleName = bundleName;
                assetBundleBuild.assetNames = assetPaths.ToArray();

                assetBundleBuilds[iAssetBundleBuild] = assetBundleBuild;
                iAssetBundleBuild++;
            }
            return assetBundleBuilds;
        }

        public void LogDebugInfo()
        {
            AssetBundleBuild[] assetBundleBuilds = GenerateAssetBundleBuild();

            bool prettyPrint = LitJson.JsonMapper.GetStaticJsonWriter().PrettyPrint;
            LitJson.JsonMapper.GetStaticJsonWriter().PrettyPrint = true;
            string debugInfo = LitJson.JsonMapper.ToJson(assetBundleBuilds);
            MDebug.Log("AssetBundle", debugInfo);
            LitJson.JsonMapper.GetStaticJsonWriter().PrettyPrint = prettyPrint;

            string filePath = Application.dataPath + "/../../ABBuilder_DebugInfo.json";
            System.IO.File.WriteAllText(filePath, debugInfo);
            EditorUtility.RevealInFinder(filePath);
        }

        public Dictionary<string, string>.KeyCollection GetIncludedAssetPath()
        {
            return m_AssetPathToBundle.Keys;
        }

        public Dictionary<string, string> GetAssetToBundle()
        {
            return m_AssetPathToBundle;
        }

        public Dictionary<string, List<string>> GetBundleToAsset()
        {
            return m_BundleToAssetPath;
        }

        public Dictionary<string, AssetInfo> GetAssetKeyToAsset()
        {
            return m_AssetKeyToAsset;
        }
    }
}