using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using GF.Common;
using GFEditor.Asset.AssetBundle.Build;
using GFEditor.Common.Utility;

namespace GFEditor.Asset.AssetBundle.Rule
{
    [CTCreateAssetMenuItem("AssetBundle/创建规则/默认", "E_1_Default")]
    public class DefaultRule : BaseRule
    {
        private const string HELP_TEXT = "默认的规则";

        public string RootPath;

        public bool AssetCollectionFoldout;
        public AssetCollectionType AssetCollectionType;

        public bool AssetFilterFoldout;
        public ExtensionFilterType ExtensionFilterType;
        public List<string> ExtensionFilterList;

        public bool BundleNameAndAssetKeyFoldout;
        public string BundleNameFormat = string.Empty;
        public string AssetKeyFormat = string.Empty;

        private AssetInfoHelper m_AssetInfoHelper;

        public override void Execute(Context context)
        {
            m_AssetInfoHelper = new AssetInfoHelper();
            switch (AssetCollectionType)
            {
                case AssetCollectionType.All:
                    CollectionAsset_Folder(context, RootPath);
                    break;
                case AssetCollectionType.ChildFolders:
                    {
                        string[] folders = System.IO.Directory.GetDirectories(RootPath);
                        for (int iFolder = 0; iFolder < folders.Length; iFolder++)
                        {
                            string iterFolder = folders[iFolder].Replace("\\", "/");
                            CollectionAsset_Folder(context, iterFolder);
                        }
                    }
                    break;
                case AssetCollectionType.Childern:
                    CollectionAsset_Folder(context, RootPath);
                    break;
                default:
                    throw new Exception("Not supprot " + AssetCollectionType.ToString());
            }
        }

        public override string GetHelpText()
        {
            return HELP_TEXT;
        }

        private void CollectionAsset_Folder(Context context, string rootPath)
        {
            string[] assets = AssetDatabase.FindAssets("", new string[] { rootPath });

            for (int iAsset = 0; iAsset < assets.Length; iAsset++)
            {
                string iterAssetPath = AssetDatabase.GUIDToAssetPath(assets[iAsset]);
                if (Filter(context, iterAssetPath))
                {
                    m_AssetInfoHelper.SetAssetInfo(iterAssetPath, rootPath);
                    context.AddAsset(iterAssetPath, m_AssetInfoHelper.Format(AssetKeyFormat), m_AssetInfoHelper.Format(BundleNameFormat));
                }
            }
        }

        private bool Filter(Context context, string assetPath)
        {
            if (System.IO.Directory.Exists(assetPath))
            {
                return false;
            }
            if (context.IncludedAsset(assetPath))
            {
                return false;
            }

            string assetPathLower = assetPath.ToLower();

            string extension = System.IO.Path.GetExtension(assetPathLower);
            for (int iExtension = 0; iExtension < ExtensionFilterList.Count; iExtension++)
            {
                ExtensionFilterList[iExtension] = ExtensionFilterList[iExtension].ToLower();
            }
            switch (ExtensionFilterType)
            {
                case ExtensionFilterType.Disable:
                    return true;
                case ExtensionFilterType.WhiteList:
                    return ExtensionFilterList.Contains(extension);
                case ExtensionFilterType.BlackList:
                    return !ExtensionFilterList.Contains(extension);
                default:
                    throw new Exception("Not supprot " + ExtensionFilterType.ToString());
            }
        }
    }

    [CustomEditor(typeof(DefaultRule))]
    public class DefaultRuleEditor : Editor
    {
        private DefaultRule m_Rule;

        public override void OnInspectorGUI()
        {
            m_Rule._OnInspectorGUI();
            m_Rule.RootPath = EGLUtility.Folder("目录", m_Rule.RootPath);

            m_Rule.AssetCollectionFoldout = EditorGUILayout.Foldout(m_Rule.AssetCollectionFoldout, "收集资源");
            if (m_Rule.AssetCollectionFoldout)
            {
                EditorGUI.indentLevel++;
                OnInspectorGUI_AssetCollection();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            m_Rule.AssetFilterFoldout = EditorGUILayout.Foldout(m_Rule.AssetFilterFoldout, "筛选资源");
            if (m_Rule.AssetFilterFoldout)
            {
                EditorGUI.indentLevel++;
                OnInspectorGUI_AssetFilter();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            m_Rule.BundleNameAndAssetKeyFoldout = EditorGUILayout.Foldout(m_Rule.BundleNameAndAssetKeyFoldout, "BundleName & AssetKey");
            if (m_Rule.BundleNameAndAssetKeyFoldout)
            {
                EditorGUI.indentLevel++;
                OnInspectorGUI_BundleNameAndAssetKey();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            GUI.enabled = true;
            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        private void OnInspectorGUI_BundleNameAndAssetKey()
        {
            EditorGUILayout.HelpBox("Bundle Name和AssetKey支持以下Format：\n"
                    + "\t{P}\t资源所在父目录名\n"
                    + "\t{AN}\t资源名不包括扩展名 \n"
                    + "\t{E}\t资源扩展名\n"
                    + "例如：Assets/Test/Prefabs/TestAssets1.prefab\n"
                    + "\t名称为 xxx, 包名为:  xxx\n"
                    + "\t名称为 {P}_xxx, 包名为: prefabs_xxx\n"
                    + "\t名称为{P}_{AN}, 包名为: prefabs_testassets1\n"
                    + "\t名称为{P}_{AN}_{E}: 包名为: prefabs_testassets1_prefab"
                , MessageType.Info);
            m_Rule.BundleNameFormat = EditorGUILayout.TextField("名字Format", m_Rule.BundleNameFormat).ToLower();
            m_Rule.AssetKeyFormat = EditorGUILayout.TextField("Key Format", m_Rule.AssetKeyFormat);
        }

        protected void OnEnable()
        {
            m_Rule = target as DefaultRule;
        }

        protected void OnDisable()
        {
            m_Rule = null;
        }

        private void OnInspectorGUI_AssetCollection()
        {
            m_Rule.AssetCollectionType = (AssetCollectionType)EditorGUILayout.EnumPopup("收集方式", m_Rule.AssetCollectionType);
            switch (m_Rule.AssetCollectionType)
            {
                case AssetCollectionType.All:
                case AssetCollectionType.ChildFolders:
                case AssetCollectionType.Childern:
                    break;
                default:
                    EditorGUILayout.HelpBox("不支持 " + m_Rule.AssetCollectionType.ToString(), MessageType.Error);
                    break;
            }
        }

        private void OnInspectorGUI_AssetFilter()
        {
            m_Rule.ExtensionFilterType = (ExtensionFilterType)EditorGUILayout.EnumPopup("扩展名筛选方式", m_Rule.ExtensionFilterType);
            switch (m_Rule.ExtensionFilterType)
            {
                case ExtensionFilterType.Disable:
                    break;
                case ExtensionFilterType.WhiteList:
                case ExtensionFilterType.BlackList:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ExtensionFilterList"), EditorGUIUtility.TrTextContent("扩展名名单"), true);
                    break;
                default:
                    EditorGUILayout.HelpBox("不支持 " + m_Rule.ExtensionFilterType.ToString(), MessageType.Error);
                    break;
            }
        }
    }
}