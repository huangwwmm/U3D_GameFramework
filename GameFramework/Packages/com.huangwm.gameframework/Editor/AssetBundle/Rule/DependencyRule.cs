using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GF.Common;
using GF.Common.Utility;
using GFEditor.Asset.AssetBundle.Build;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Asset.AssetBundle.Rule
{
    [CTCreateAssetMenuItem("AssetBundle/创建规则/依赖", "E_1_Dependency")]
    public class DependencyRule : BaseRule
    {
        private const string HELP_TEXT = "把被复数Bundle依赖的资源提取出来单独打包";

        public bool ExcludeShader;
        public AutoDependenciesBunleType AutoDependenciesBunleType;

        public override void Execute(Context context)
        {
            Dictionary<string, HashSet<string>> assetDependenciesToBundle = context.GetAssetDependenciesToBundle();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<string, HashSet<string>> kv in assetDependenciesToBundle)
            {
                string asset = kv.Key;
                if (kv.Value.Count < 2
                    || context.IncludedAsset(asset))
                {
                    continue;
                }

                string extension = Path.GetExtension(asset).ToLower();
                if (extension == ".cs")
                {
                    continue;
                }
                if (ExcludeShader
                    && (extension == ".shader"
                      || extension == ".compute"))
                {
                    continue;
                }

                string bundleName;
                switch(AutoDependenciesBunleType)
                {
                    case AutoDependenciesBunleType.AllAsset:
                        bundleName = _Name;
                        break;
                    case AutoDependenciesBunleType.SingleAsset:
                        bundleName = $"{_Name}_{AssetDatabase.AssetPathToGUID(asset)}";
                        break;
                    case AutoDependenciesBunleType.DependencyRepeatedBundle:
                        HashSet<string> bundles = kv.Value;
                        foreach(string bundle in bundles)
                        {
                            stringBuilder.Append(bundle);
                        }
                        bundleName = $"{_Name}_{StringUtility.CalculateMD5Hash(stringBuilder.ToString())}";
                        stringBuilder.Clear();
                        break;
                    default:
                        throw new Exception("Not Handle AutoDependenciesBunleType: " + AutoDependenciesBunleType);
                }

                context.AddAsset(asset, null, StringUtility.FormatToVariableName(bundleName));
            }
        }


        public override string GetHelpText()
        {
            return HELP_TEXT;
        }
    }

    [CustomEditor(typeof(DependencyRule))]
    public class DependencyRuleEditor : Editor
    {
        private DependencyRule m_Rule;

        public override void OnInspectorGUI()
        {
            m_Rule._OnInspectorGUI();

            m_Rule.ExcludeShader = EditorGUILayout.Toggle("排除Shader", m_Rule.ExcludeShader);
            m_Rule.AutoDependenciesBunleType = (AutoDependenciesBunleType)EditorGUILayout.EnumPopup("分组方式"
                , m_Rule.AutoDependenciesBunleType);

            GUI.enabled = true;
            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        protected void OnEnable()
        {
            m_Rule = target as DependencyRule;
        }

        protected void OnDisable()
        {
            m_Rule = null;
        }
    }
}