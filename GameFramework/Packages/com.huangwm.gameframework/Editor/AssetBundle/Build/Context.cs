using GF.Common.Debug;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Asset.AssetBundle.Build
{
    public class Context
    {
        private Dictionary<string, List<string>> m_BundleToAsset = new Dictionary<string, List<string>>();
        private Dictionary<string, string> m_AssetToBundle = new Dictionary<string, string>();
        private Dictionary<string, HashSet<string>> m_AssetDependenciesToBundle;

        public void SetAssetDependenciesToBundle(Dictionary<string, HashSet<string>> assetDependenciesToBundle)
        {
            m_AssetDependenciesToBundle = assetDependenciesToBundle;
        }

        public Dictionary<string, HashSet<string>> GetAssetDependenciesToBundle()
        {
            return m_AssetDependenciesToBundle;
        }

        public bool IncludedAsset(string assetPath)
        {
            return m_AssetToBundle.ContainsKey(assetPath);
        }

        public void AddAsset(string assetPath, string bundleName)
        {
            bundleName = bundleName.ToLower();
            if (!m_AssetToBundle.ContainsKey(assetPath))
            {
                m_AssetToBundle[assetPath] = bundleName;
                if (!m_BundleToAsset.TryGetValue(bundleName, out List<string> assets))
                {
                    assets = new List<string>();
                    m_BundleToAsset.Add(bundleName, assets);
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
            AssetBundleBuild[] assetBundleBuilds = new AssetBundleBuild[m_BundleToAsset.Count];
            int iAssetBundleBuild = 0;
            foreach (KeyValuePair<string, List<string>> kv in m_BundleToAsset)
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
            return m_AssetToBundle.Keys;
        }

        public Dictionary<string, string> GetAssetToBundle()
        {
            return m_AssetToBundle;
        }

        public Dictionary<string, List<string>> GetBundleToAsset()
        {
            return m_BundleToAsset;
        }
    }
}