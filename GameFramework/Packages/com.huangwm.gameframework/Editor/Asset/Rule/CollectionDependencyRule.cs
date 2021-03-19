using System.Collections;
using System.Collections.Generic;
using System.IO;
using GF.Common;
using GFEditor.Asset.Build;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Asset.Rule
{
    [CTCreateAssetMenuItem("AssetBundle/创建规则/收集依赖", "E_1_CollectionDependency")]
    public class CollectionDependencyRule : BaseRule
    {
        private const string HELP_TEXT = "收集Bundle中依赖到的资源";

        private Dictionary<string, HashSet<string>> m_AssetDependenciesToBundle;

        public override void Execute(Context context)
        {
            m_AssetDependenciesToBundle = new Dictionary<string, HashSet<string>>();

            Dictionary<string, List<string>> bundleToAsset = context.GetBundleToAsset();
            int assetCount = context.GetIncludedAssetPath().Count;
            int handledAsset = 0;
            foreach (KeyValuePair<string, List<string>> kv in bundleToAsset)
            {
                string bundle = kv.Key;
                List<string> assets = kv.Value;
                for (int iAsset = 0; iAsset < assets.Count; iAsset++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Execute Rule " + name
                        , $"Collection dependencies {handledAsset}/{assetCount}"
                        , (float)handledAsset / assetCount))
                    {
                        throw new CancelException();
                    }
                    string asset = assets[iAsset];
                    CollectionDependencies(bundle, asset);
                    handledAsset++;
                }
            }

            context.SetAssetDependenciesToBundle(m_AssetDependenciesToBundle);
            m_AssetDependenciesToBundle = null;
        }

        private void CollectionDependencies(string bundle, string asset)
        {
            string[] dependencies = AssetDatabase.GetDependencies(asset, true);
            for (int iDependency = 0; iDependency < dependencies.Length; iDependency++)
            {
                string dependency = dependencies[iDependency];
                string dependencyExtension = Path.GetExtension(dependency).ToLower();

                if (!m_AssetDependenciesToBundle.TryGetValue(dependency, out HashSet<string> bundles))
                {
                    bundles = new HashSet<string>();
                    m_AssetDependenciesToBundle.Add(dependency, bundles);
                }
                bundles.Add(bundle);
            }
        }

        public override string GetHelpText()
        {
            return HELP_TEXT;
        }
    }

    [CustomEditor(typeof(CollectionDependencyRule))]
    public class CollectionDependencyEditor : Editor
    {
        private CollectionDependencyRule m_Rule;

        public override void OnInspectorGUI()
        {
            m_Rule._OnInspectorGUI();

            GUI.enabled = true;
            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        protected void OnEnable()
        {
            m_Rule = target as CollectionDependencyRule;
        }

        protected void OnDisable()
        {
            m_Rule = null;
        }
    }
}