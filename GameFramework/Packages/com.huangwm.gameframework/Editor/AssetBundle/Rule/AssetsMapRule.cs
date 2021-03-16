using System.Collections.Generic;
using System.IO;
using System.Text;
using GF.Common;
using GFEditor.Asset.AssetBundle.Build;
using GFEditor.Common.Utility;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Asset.AssetBundle.Rule
{
    [CTCreateAssetMenuItem("AssetBundle/创建规则/AssetsMap", "E_1_AssetsMap")]
    public class AssetsMapRule : BaseRule
    {
        private const string HELP_TEXT = "生成一个所有资源的对应关系并保存到文件";

        public string BundleName = string.Empty;
        public string AssetMapPath = string.Empty;

        public override void Execute(Context context)
        {
            Dictionary<string, List<string>> bundleToAsset = context.GetBundleToAsset();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<string, List<string>> kv in bundleToAsset)
            {
                string bundle = kv.Key;
                List<string> assets = kv.Value;
                for (int iAsset = 0; iAsset < assets.Count; iAsset++)
                {
                    string asset = assets[iAsset];
                    stringBuilder.AppendLine($"{bundle},{asset}");
                }
            }

            File.WriteAllText(AssetMapPath, stringBuilder.ToString());
            AssetDatabase.Refresh();

            context.AddAsset(AssetMapPath, BundleName);
        }

        public override string GetHelpText()
        {
            return HELP_TEXT;
        }
    }

    [CustomEditor(typeof(AssetsMapRule))]
    public class AssetsMapRuleeEditor : Editor
    {
        private AssetsMapRule m_Rule;

        public override void OnInspectorGUI()
        {
            m_Rule._OnInspectorGUI();

            m_Rule.BundleName = EditorGUILayout.TextField("Bundle名字", m_Rule.BundleName).ToLower();
            m_Rule.AssetMapPath = EGLUtility.File("AssetMap路径", m_Rule.AssetMapPath);

            GUI.enabled = true;
            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        protected void OnEnable()
        {
            m_Rule = target as AssetsMapRule;
        }

        protected void OnDisable()
        {
            m_Rule = null;
        }
    }
}