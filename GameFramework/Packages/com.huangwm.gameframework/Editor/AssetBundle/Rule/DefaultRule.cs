using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using GF.Common;
using GFEditor.Asset.AssetBundle.Build;
using GFEditor.Common.Utility;
using GF.Common.Utility;

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

        public bool BundleNameFoldout;
        public BundleNameType BundleNameType;
        public string SpecifyBundleName = string.Empty;
        public string BundleNameFormat = string.Empty;

        public override void Execute(Context context)
        {
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

        private void CollectionAsset_Folder(Context context, string parentPath)
        {
            string[] assets = AssetDatabase.FindAssets("", new string[] { parentPath });
            for (int iAsset = 0; iAsset < assets.Length; iAsset++)
            {
                string iterAssetPath = AssetDatabase.GUIDToAssetPath(assets[iAsset]);
                if (Filter(context, iterAssetPath))
                {
                    context.AddAsset(iterAssetPath, CaculateBundleName(parentPath, iterAssetPath));
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

        private string CaculateBundleName(string parentPath, string assetPath)
        {
            string bundleName;
            switch (BundleNameType)
            {
                case BundleNameType.Specify:
                    bundleName = SpecifyBundleName;
                    break;
                case BundleNameType.FormatParentFolderName:
                    string folderName = parentPath.Substring(parentPath.LastIndexOf('/') + 1);
                    bundleName = string.Format(BundleNameFormat, folderName);
                    break;
                case BundleNameType.FormatAssetName:
                    string assetName = System.IO.Path.GetFileName(assetPath);
                    bundleName = string.Format(BundleNameFormat, assetName);
                    break;
                case BundleNameType.FormatRelativePath:
                    string relativePath = assetPath.Substring(parentPath.Length + 1);
                    bundleName = string.Format(BundleNameFormat, relativePath);
                    break;
                default:
                    throw new Exception("Not supprot " + BundleNameType.ToString());
            }

            return StringUtility.FormatToVariableName(bundleName);
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

            m_Rule.BundleNameFoldout = EditorGUILayout.Foldout(m_Rule.BundleNameFoldout, "Bundle名字");
            if (m_Rule.BundleNameFoldout)
            {
                EditorGUI.indentLevel++;
                OnInspectorGUI_BundleName();
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

        private void OnInspectorGUI_BundleName()
        {
            m_Rule.BundleNameType = (BundleNameType)EditorGUILayout.EnumPopup("命名方式", m_Rule.BundleNameType);
            switch (m_Rule.BundleNameType)
            {
                case BundleNameType.Specify:
                    m_Rule.SpecifyBundleName = EditorGUILayout.TextField("指定名字", m_Rule.SpecifyBundleName).ToLower();
                    break;
                case BundleNameType.FormatParentFolderName:
                case BundleNameType.FormatAssetName:
                case BundleNameType.FormatRelativePath:
                    m_Rule.BundleNameFormat = EditorGUILayout.TextField("名字Format", m_Rule.BundleNameFormat).ToLower();
                    break;
                default:
                    EditorGUILayout.HelpBox("不支持 " + m_Rule.BundleNameType.ToString(), MessageType.Error);
                    break;
            }
        }
    }
}