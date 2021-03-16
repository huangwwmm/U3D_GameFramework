using GF.Common;
using GF.Common.Collection;
using GF.Common.Debug;
using GF.Common.Utility;
using GFEditor.Common.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GFEditor.AssetDB
{
    public class AssetDBWindow : EditorWindow, IHasCustomMenu
    {
        private const int NOTSET_TAB_INDEX = -1;

        private int m_ActiveTabIndex;
        private List<Tab> m_Tabs;

        [CTMenuItem("工具/AssetDB/主窗口")]
        public static void Open()
        {
            GetWindow<AssetDBWindow>("Asset查询");
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("重新计算所有资源的引用关系"), false, DB.GetInstance().RecaculateDBWithDialog);
            menu.AddItem(EditorGUIUtility.TrTextContent("保存AssetDB"), false, DB.GetInstance().SaveDB);
            menu.AddSeparator("");
#if UNITY_2019_1_OR_NEWER
            menu.AddItem(EditorGUIUtility.TrTextContent("打开新的AssetDB窗口"), false, CreateAssetDBWindow);
#endif
            menu.AddItem(EditorGUIUtility.TrTextContent("添加页签/关系查询"), false, AddDependencyTab);
            menu.AddItem(EditorGUIUtility.TrTextContent("添加页签/关系查询(高级)"), false, AddDependencyPlusTab);
            menu.AddItem(EditorGUIUtility.TrTextContent("添加页签/无用资源"), false, AddBeDependencyTab);
            menu.AddItem(EditorGUIUtility.TrTextContent("关闭当前页签"), false, CloseCurrentTab);
        }

        protected void OnEnable()
        {
            m_ActiveTabIndex = NOTSET_TAB_INDEX;
            m_Tabs = new List<Tab>();

            AddDependencyTab();
            AddDependencyPlusTab();
            AddBeDependencyTab();
        }

        protected void OnDisable()
        {
            for (int iTab = 0; iTab < m_Tabs.Count; iTab++)
            {
                if (m_Tabs[iTab].IsInitialized())
                {
                    m_Tabs[iTab].Release();
                }
            }
            m_Tabs = null;
        }

        protected void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            for (int iTab = 0; iTab < m_Tabs.Count; iTab++)
            {
                if (GUILayout.Toggle(iTab == m_ActiveTabIndex, m_Tabs[iTab].GetText(), EditorStyles.toolbarButton))
                {
                    m_ActiveTabIndex = iTab;
                }
                else if (m_ActiveTabIndex == iTab)
                {
                    m_ActiveTabIndex = NOTSET_TAB_INDEX;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (m_ActiveTabIndex != NOTSET_TAB_INDEX
                && m_ActiveTabIndex < m_Tabs.Count)
            {
                Tab activeTab = m_Tabs[m_ActiveTabIndex];
                if (!activeTab.IsInitialized())
                {
                    activeTab.Initialize();
                }
                activeTab.OnGUI();
            }
        }

#if UNITY_2019_1_OR_NEWER
        private void CreateAssetDBWindow()
        {
            CreateWindow<AssetDBWindow>("Asset DB");
        }
#endif

        private void AddDependencyTab()
        {
            m_Tabs.Add(new DependencyTab("关系查询"));
        }

        private void AddDependencyPlusTab()
        {
            m_Tabs.Add(new DependencyPlusTab("关系查询(高级)"));
        }

        private void AddBeDependencyTab()
        {
            m_Tabs.Add(new NonBeDependencyTab("无用资源"));
        }

        private void CloseCurrentTab()
        {
            if (m_ActiveTabIndex >= 0 && m_ActiveTabIndex < m_Tabs.Count)
            {
                if (m_Tabs[m_ActiveTabIndex].IsInitialized())
                {
                    m_Tabs[m_ActiveTabIndex].Release();
                }

                m_Tabs.RemoveAt(m_ActiveTabIndex);
            }
        }

        private class Tab
        {
            protected string m_Text;
            private bool m_IsInitialized;

            public Tab(string text)
            {
                m_IsInitialized = false;
                m_Text = text;
            }

            public bool IsInitialized()
            {
                return m_IsInitialized;
            }

            public virtual void Initialize()
            {
                m_IsInitialized = true;
            }

            public virtual void Release()
            {
                m_IsInitialized = false;
            }

            public virtual void OnGUI()
            {
            }

            public string GetText()
            {
                return m_Text;
            }
        }

        private class DependencyTab : Tab
        {
            private const string NOTSET_ASSET_GUID = null;

            private Config m_Config;
            private Config m_LastConfig;

            private string m_AssetGUID;
            private List<UnityEngine.Object> m_Dependencies;
            private bool m_DependenciesFoldout = true;
            private Vector2 m_DependenciesScrollPosition = Vector2.zero;
            private BaseAssetProcess m_DependenciesAssetProcess;

            private List<UnityEngine.Object> m_BeDependencies;
            private bool m_BeDependenciesFoldout = true;
            private Vector2 m_BeDependenciesScrollPosition = Vector2.zero;
            private BaseAssetProcess m_BeDependenciesAssetProcess;

            public DependencyTab(string text)
                : base(text)
            {

            }

            public override void Initialize()
            {
                base.Initialize();

                m_AssetGUID = NOTSET_ASSET_GUID;
                m_Dependencies = new List<UnityEngine.Object>();
                m_BeDependencies = new List<UnityEngine.Object>();
            }

            public override void Release()
            {
                m_Dependencies = null;
                m_BeDependencies = null;

                Resources.UnloadUnusedAssets();

                base.Release();
            }

            public override void OnGUI()
            {
                base.OnGUI();

                m_Config.Asset = EditorGUILayout.ObjectField("Asset", m_Config.Asset, typeof(UnityEngine.Object), false);
                if (m_Config.Asset == null)
                {
                    EditorGUILayout.HelpBox("拖拽Asset至上方\"Asset\" Field后，会显示Asset的引用关系", MessageType.Info);
                }
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                m_Config.BeDependencyDepth = GUILayout.Toggle(m_Config.BeDependencyDepth, "递归被引用", EditorStyles.toolbarButton);
                m_Config.DependencyDepth = GUILayout.Toggle(m_Config.DependencyDepth, "递归引用", EditorStyles.toolbarButton);
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
                {
                    FindAssetRelation();
                }
                GUILayout.EndHorizontal();

                if (m_Config != m_LastConfig)
                {
                    m_LastConfig = m_Config;
                    FindAssetRelation();
                }

                if (m_AssetGUID == NOTSET_ASSET_GUID)
                {
                    return;
                }

                EditorGUILayout.Space();
                m_DependenciesFoldout = EditorGUILayout.Foldout(m_DependenciesFoldout, "引用数量 数量：" + m_Dependencies.Count);
                if (m_DependenciesFoldout)
                {
                    EditorGUI.indentLevel++;
                    OnGUI_AssetProcess(ref m_DependenciesAssetProcess, m_Dependencies);

                    if (GUILayout.Button("导出Package"))
                    {
                        string exportPath = EditorUtility.OpenFolderPanel("选择导出目录", Application.dataPath, "Export") + $"/Export_{m_Config.Asset.name}.unitypackage";
                        if (!string.IsNullOrEmpty(exportPath))
                        {
                            List<string> exportAssetPaths = new List<string>();
                            exportAssetPaths.Add(AssetDatabase.GetAssetPath(m_Config.Asset));
                            for (int iAsset = 0; iAsset < m_Dependencies.Count; iAsset++)
                            {
                                exportAssetPaths.Add(AssetDatabase.GetAssetPath(m_Dependencies[iAsset]));
                            }
                            AssetDatabase.ExportPackage(exportAssetPaths.ToArray(), exportPath, ExportPackageOptions.Interactive);
                        }
                    }

                    m_DependenciesScrollPosition = EditorGUILayout.BeginScrollView(m_DependenciesScrollPosition);
                    for (int iDependencie = 0; iDependencie < m_Dependencies.Count; iDependencie++)
                    {
                        EditorGUILayout.ObjectField(m_Dependencies[iDependencie], typeof(UnityEngine.Object), false);
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUI.indentLevel--;
                }

                m_BeDependenciesFoldout = EditorGUILayout.Foldout(m_BeDependenciesFoldout, "被引用数量 数量：" + m_BeDependencies.Count);
                if (m_BeDependenciesFoldout)
                {
                    EditorGUI.indentLevel++;
                    OnGUI_AssetProcess(ref m_BeDependenciesAssetProcess, m_BeDependencies);

                    m_BeDependenciesScrollPosition = EditorGUILayout.BeginScrollView(m_BeDependenciesScrollPosition);
                    for (int iReference = 0; iReference < m_BeDependencies.Count; iReference++)
                    {
                        EditorGUILayout.ObjectField(m_BeDependencies[iReference], typeof(UnityEngine.Object), false);
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUI.indentLevel--;
                }

                GUILayout.FlexibleSpace();
            }

            private void OnGUI_AssetProcess(ref BaseAssetProcess assetProcess
                , List<UnityEngine.Object> assets)
            {
                EditorGUILayout.BeginHorizontal();
                assetProcess = EditorGUILayout.ObjectField("资源处理"
                    , assetProcess
                    , typeof(BaseAssetProcess)
                    , true) as BaseAssetProcess;
                if (GUILayout.Button("处理", GUILayout.Width(72)))
                {
                    assetProcess.ProcessAll(assets.ToArray());
                }
                EditorGUILayout.EndHorizontal();
            }

            private void FindAssetRelation()
            {
                m_AssetGUID = NOTSET_ASSET_GUID;
                m_Dependencies.Clear();
                m_BeDependencies.Clear();

                if (m_Config.Asset == null)
                {
                    return;
                }

                string assetPath = AssetDatabase.GetAssetPath(m_Config.Asset);
                if (string.IsNullOrEmpty(assetPath))
                {
                    return;
                }

                m_AssetGUID = AssetDatabase.AssetPathToGUID(assetPath);

                BetterDictionary<string, Asset> db = DB.GetInstance().GetAssets();
                HashSet<string> dependencies = new HashSet<string>();
                FindAssetDependencies(db, m_AssetGUID, dependencies);
                HashSet<string> beDependencies = new HashSet<string>();
                FindAssetBeDependencies(db, m_AssetGUID, beDependencies);

                AssetUtility.LoadMainAssetAtGUIDs(m_Dependencies, dependencies );
                AssetUtility.LoadMainAssetAtGUIDs(m_BeDependencies, beDependencies);
            }

            private void FindAssetDependencies(BetterDictionary<string, Asset> db, string assetGUID, HashSet<string> dependencies)
            {
                if (!db.TryGetValue(assetGUID, out Asset asset))
                {
                    return;
                }

                for (int iAsset = 0; iAsset < asset.Dependencies.Count; iAsset++)
                {
                    string iterAssetGUID = asset.Dependencies[iAsset];
                    if (dependencies.Add(iterAssetGUID)
                        && m_Config.DependencyDepth)
                    {
                        FindAssetDependencies(db, iterAssetGUID, dependencies);
                    }
                }
            }

            private void FindAssetBeDependencies(BetterDictionary<string, Asset> db, string assetGUID, HashSet<string> beDependencies)
            {
                if (!db.TryGetValue(assetGUID, out Asset asset))
                {
                    return;
                }

                for (int iAsset = 0; iAsset < asset.BeDependencies.Count; iAsset++)
                {
                    string iterAssetGUID = asset.BeDependencies[iAsset];
                    if (beDependencies.Add(iterAssetGUID)
                        && m_Config.BeDependencyDepth)
                    {
                        FindAssetBeDependencies(db, iterAssetGUID, beDependencies);
                    }
                }
            }

            public struct Config
            {
                public UnityEngine.Object Asset;
                public bool DependencyDepth;
                public bool BeDependencyDepth;

                public static bool operator ==(Config a, Config b)
                {
                    return a.Equals(b);
                }

                public static bool operator !=(Config a, Config b)
                {
                    return !(a == b);
                }

                public override bool Equals(object obj)
                {
                    if (!(obj is Config))
                    {
                        return false;
                    }

                    var config = (Config)obj;
                    return EqualityComparer<UnityEngine.Object>.Default.Equals(Asset, config.Asset) &&
                           DependencyDepth == config.DependencyDepth &&
                           BeDependencyDepth == config.BeDependencyDepth;
                }

                public override int GetHashCode()
                {
                    var hashCode = -145111102;
                    hashCode = hashCode * -1521134295 + EqualityComparer<UnityEngine.Object>.Default.GetHashCode(Asset);
                    hashCode = hashCode * -1521134295 + DependencyDepth.GetHashCode();
                    hashCode = hashCode * -1521134295 + BeDependencyDepth.GetHashCode();
                    return hashCode;
                }
            }
        }

        private class DependencyPlusTab : Tab
        {
            private bool m_DependenciesFoldout = true;
            private Vector2 m_DependenciesScrollPosition = Vector2.zero;
            private bool m_DisplayDependenciesObject = true;
            private bool m_DisplayDependenciesBuiltinObject = true;
            /// <summary>
            /// 引用的项目中的资源
            /// </summary>
            private List<UnityEngine.Object> m_DependenciesObject;
            /// <summary>
            /// 引用的内置资源
            /// </summary>
            private List<UnityEditor.Build.Content.ObjectIdentifier> m_DependenciesBuiltinObject;

            private bool m_BeDependenciesFoldout = true;
            private Vector2 m_BeDependenciesScrollPosition = Vector2.zero;
            private List<AssetBeDependency> m_AssetBeDependencies;
            /// <summary>
            /// 引用的项目中的资源
            /// </summary>
            private List<UnityEngine.Object> m_BeDependenciesObject;

            private Config m_Config;
            private Config m_LastConfig;

            public DependencyPlusTab(string text)
                : base(text)
            {
            }

            public override void OnGUI()
            {
                if (CompilePlayerScriptsUtility.s_CurrentTargetTypeDBCache == null)
                {
                    EditorGUILayout.HelpBox("可以查询内置资源的引用情况，但是查询速度慢于（关系查询），而且不支持实时更新关系表。\n"
                        + "因为是Unity内部使用的低级API，几乎没有文档资料，不支持深度查询，查询结果也和我想象中不太一样。\n"
                        + "结果仅供参考，一切实际为准"
                        , MessageType.Info);
                    if (GUILayout.Button("初始化"))
                    {
                        CompilePlayerScriptsUtility.GetOrCompileCurrentTargetTypeDB(true, out UnityEditor.Build.Player.TypeDB typeDB);
                    }
                    return;
                }
                m_Config.TargetAssetType = (TargetAssetType)EditorGUILayout.EnumPopup("目标类型", m_Config.TargetAssetType);
                switch (m_Config.TargetAssetType)
                {
                    case TargetAssetType.Asset:
                        m_Config.Asset = EditorGUILayout.ObjectField("Asset", m_Config.Asset, typeof(UnityEngine.Object), true);
                        break;
                    case TargetAssetType.ObjectIdentifier:
                        m_Config.ObjectIdentifier_GUID = EditorGUILayout.DelayedTextField("GUID", m_Config.ObjectIdentifier_GUID).ToLower();
                        m_Config.ObjectIdentifier_FileID = EditorGUILayout.DelayedIntField("FileID", (int)m_Config.ObjectIdentifier_FileID);
                        m_Config.ObjectIdentifier_FileType = (UnityEditor.Build.Content.FileType)EditorGUILayout.EnumPopup("FileType", m_Config.ObjectIdentifier_FileType);
                        m_Config.ObjectIdentifier_FilePath = EditorGUILayout.DelayedTextField("FilePath", m_Config.ObjectIdentifier_FilePath);
                        break;
                }

                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                if (m_Config.TargetAssetType == TargetAssetType.ObjectIdentifier)
                {
                    m_Config.ObjectIdentifier_SearchFlag = (ObjectIdentifierSearchFlag)EditorGUILayout.EnumFlagsField("搜索项"
                        , m_Config.ObjectIdentifier_SearchFlag
                        , EditorStyles.toolbarPopup);
                    if (GUILayout.Button("生成资源索引表", EditorStyles.toolbarButton))
                    {
                        GenerateAssetBeDependencies();
                    }
                }
                m_Config.AutoSearch = GUILayout.Toggle(m_Config.AutoSearch, "自动刷新", EditorStyles.toolbarButton);
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
                {
                    FindRelation();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                if (m_Config.TargetAssetType == TargetAssetType.ObjectIdentifier)
                {
                    if (m_AssetBeDependencies != null)
                    {
                        if (GUILayout.Button("保存资源索引表到文件", EditorStyles.toolbarButton))
                        {
                            SaveAssetBeDependenciesToFile();
                        }
                        if (GUILayout.Button("保存资源索引表到便于阅读的文件", EditorStyles.toolbarButton))
                        {
                            SaveAssetBeDependenciesToFileForRead();
                        }
                    }
                    if (GUILayout.Button("从文件加载资源索引表", EditorStyles.toolbarButton))
                    {
                        LoadAssetBeDependenciesFromFile();
                    }
                }
                GUILayout.EndHorizontal();

                if (m_Config != m_LastConfig)
                {
                    m_LastConfig = m_Config;
                    if (m_Config.AutoSearch)
                    {
                        FindRelation();
                    }
                }

                DoGUI_Dependency();
                DoGUI_BeDependency();
            }

            public override void Initialize()
            {
                base.Initialize();

                m_Config.TargetAssetType = TargetAssetType.Asset;
                m_Config.ObjectIdentifier_SearchFlag = (ObjectIdentifierSearchFlag)int.MaxValue;
                m_Config.ObjectIdentifier_GUID = string.Empty;
                m_Config.AutoSearch = true;

                m_DependenciesObject = new List<UnityEngine.Object>();
                m_DependenciesBuiltinObject = new List<UnityEditor.Build.Content.ObjectIdentifier>();

                m_BeDependenciesObject = new List<UnityEngine.Object>();
            }

            public override void Release()
            {
                m_Config.Asset = null;

                m_DependenciesObject = null;
                m_DependenciesBuiltinObject = null;

                m_AssetBeDependencies = null;
                m_BeDependenciesObject = null;

                Resources.UnloadUnusedAssets();

                base.Release();
            }

            private void DoGUI_Dependency()
            {
                if (m_Config.TargetAssetType != TargetAssetType.Asset)
                {
                    EditorGUILayout.HelpBox("只支持查询Asset引用的资源", MessageType.Info);
                    return;
                }

                m_DependenciesFoldout = EditorGUILayout.Foldout(m_DependenciesFoldout
                    , $"引用 项目中: {m_DependenciesObject.Count} -  内置: {m_DependenciesBuiltinObject.Count}");
                if (!m_DependenciesFoldout)
                {
                    return;
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                m_DisplayDependenciesObject = GUILayout.Toggle(m_DisplayDependenciesObject, "项目中资源", EditorStyles.toolbarButton);
                m_DisplayDependenciesBuiltinObject = GUILayout.Toggle(m_DisplayDependenciesBuiltinObject, "内置资源", EditorStyles.toolbarButton);
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                m_DependenciesScrollPosition = EditorGUILayout.BeginScrollView(m_DependenciesScrollPosition);

                if (m_DisplayDependenciesObject)
                {
                    for (int iObject = 0; iObject < m_DependenciesObject.Count; iObject++)
                    {
                        EditorGUILayout.ObjectField(m_DependenciesObject[iObject], typeof(UnityEngine.Object), false);
                    }
                }

                if (m_DisplayDependenciesBuiltinObject)
                {
                    for (int iObject = 0; iObject < m_DependenciesBuiltinObject.Count; iObject++)
                    {
                        UnityEditor.Build.Content.ObjectIdentifier iterObject = m_DependenciesBuiltinObject[iObject];
                        EditorGUILayout.TextField($"guid: {iterObject.guid}, fileID: {iterObject.localIdentifierInFile}, type: {iterObject.fileType}, path: {iterObject.filePath}");
                    }
                }

                EditorGUILayout.EndScrollView();
                EditorGUI.indentLevel--;
            }

            private void DoGUI_BeDependency()
            {
                if (m_Config.TargetAssetType != TargetAssetType.ObjectIdentifier)
                {
                    EditorGUILayout.HelpBox("只支持查询ObjectIdentifier被引用的资源", MessageType.Info);
                    return;
                }

                if (m_AssetBeDependencies == null)
                {
                    EditorGUILayout.HelpBox("查询之前需要先生成资源索引表", MessageType.Warning);
                    return;
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                m_BeDependenciesFoldout = EditorGUILayout.Foldout(m_BeDependenciesFoldout, $"被引用 - 数量:{m_BeDependenciesObject.Count}");
                GUILayout.FlexibleSpace();
                if (m_BeDependenciesObject.Count > 0
                    && GUILayout.Button("导出查询结果"))
                {
                    List<string> assetPaths = new List<string>();
                    for (int iObject = 0; iObject < m_BeDependenciesObject.Count; iObject++)
                    {
                        assetPaths.Add(AssetDatabase.GetAssetPath(m_BeDependenciesObject[iObject]));
                    }

                    string reportPath = Directory.GetParent(Application.dataPath).FullName + @"\Library\BeDependency.txt";
                    try
                    {
                        File.WriteAllText(reportPath, string.Join("\n", assetPaths));
                        EditorUtility.RevealInFinder(reportPath);
                        EditorUtility.OpenWithDefaultApp(reportPath);
                    }
                    catch (Exception e)
                    {
                        MDebug.LogError("Asset", $"Write to file ({reportPath}) Exception:\n{e.ToString()}");
                    }
                }
                EditorGUILayout.EndHorizontal();
                if (!m_BeDependenciesFoldout)
                {
                    return;
                }

                EditorGUI.indentLevel++;
                m_BeDependenciesScrollPosition = EditorGUILayout.BeginScrollView(m_BeDependenciesScrollPosition);

                for (int iObject = 0; iObject < m_BeDependenciesObject.Count; iObject++)
                {
                    EditorGUILayout.ObjectField(m_BeDependenciesObject[iObject], typeof(UnityEngine.Object), false);
                }

                EditorGUILayout.EndScrollView();
                EditorGUI.indentLevel--;
            }

            private void GenerateAssetBeDependencies()
            {
                m_AssetBeDependencies = new List<AssetBeDependency>();
                string[] assetGUIDs = AssetDatabase.FindAssets("");
                for (int iAsset = 0; iAsset < assetGUIDs.Length; iAsset++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Asset"
                        , $"生成资源索引表 {iAsset}/{assetGUIDs.Length}"
                        , iAsset / (float)assetGUIDs.Length))
                    {
                        break;
                    }

                    if (iAsset % 100 == 0)
                    {
                        GC.Collect();
                        Resources.UnloadUnusedAssets();
                        GC.Collect();
                    }

                    string iterAssetGUID = assetGUIDs[iAsset];
                    AssetBeDependency assetBeDependencies = new AssetBeDependency();
                    assetBeDependencies.AssetGUID = iterAssetGUID;
                    System.Collections.IEnumerable objectIdentifiers;
                    string assetPath = AssetDatabase.GUIDToAssetPath(iterAssetGUID);
                    if (Path.GetExtension(assetPath).ToLower() == ".unity")
                    {
                        UnityEditor.Build.Content.BuildSettings buildSettings = new UnityEditor.Build.Content.BuildSettings();
                        buildSettings.typeDB = CompilePlayerScriptsUtility.s_CurrentTargetTypeDBCache;
                        buildSettings.target = EditorUserBuildSettings.activeBuildTarget;
                        buildSettings.group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                        buildSettings.buildFlags = UnityEditor.Build.Content.ContentBuildFlags.None;

#if UNITY_2019_1_OR_NEWER
                        objectIdentifiers = UnityEditor.Build.Content.ContentBuildInterface.CalculatePlayerDependenciesForScene(assetPath
                            , buildSettings
                            , new UnityEditor.Build.Content.BuildUsageTagSet()).referencedObjects;
#else
                        objectIdentifiers = UnityEditor.Build.Content.ContentBuildInterface.PrepareScene(assetPath
                            , buildSettings
                            , new UnityEditor.Build.Content.BuildUsageTagSet()
                            , Application.dataPath + "/../Temp/PrepareScene").referencedObjects;
#endif
                    }
                    else
                    {
                        objectIdentifiers = UnityEditor.Build.Content.ContentBuildInterface.GetPlayerDependenciesForObjects(UnityEditor.Build.Content.ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(new GUID(iterAssetGUID)
                                , EditorUserBuildSettings.activeBuildTarget)
                            , EditorUserBuildSettings.activeBuildTarget
                            , CompilePlayerScriptsUtility.s_CurrentTargetTypeDBCache);
                    }
                    assetBeDependencies.Dependencies = MyObjectIdentifier.Convert(objectIdentifiers);

                    m_AssetBeDependencies.Add(assetBeDependencies);
                }

                GC.Collect();
                Resources.UnloadUnusedAssets();
                GC.Collect();
                EditorUtility.ClearProgressBar();

                if (m_Config.AutoSearch)
                {
                    FindBeDependencies();
                }
            }

            private void FindRelation()
            {
                FindDependencies();
                FindBeDependencies();
            }

            private void FindDependencies()
            {
                m_DependenciesObject.Clear();
                m_DependenciesBuiltinObject.Clear();

                if (m_Config.TargetAssetType != TargetAssetType.Asset
                    || m_Config.Asset == null)
                {
                    return;
                }

                UnityEditor.Build.Content.ObjectIdentifier[] dependenciesObjectIdentifier = AssetUtility.SearchAssetDependencies(m_Config.Asset, CompilePlayerScriptsUtility.s_CurrentTargetTypeDBCache);
                HashSet<string> hashSetTemp = new HashSet<string>();
                for (int iObject = 0; iObject < dependenciesObjectIdentifier.Length; iObject++)
                {
                    UnityEditor.Build.Content.ObjectIdentifier iterObjectIdentifier = dependenciesObjectIdentifier[iObject];
                    if (AssetUtility.IsBuiltinOrLibraryAsset(iterObjectIdentifier))
                    {
                        m_DependenciesBuiltinObject.Add(iterObjectIdentifier);
                    }
                    else
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(iterObjectIdentifier.guid.ToString());
                        if (hashSetTemp.Add(assetPath))
                        {
                            UnityEngine.Object iterObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
                            if (iterObject != null)
                            {
                                m_DependenciesObject.Add(iterObject);
                            }
                            MDebug.LogWarning("Asset", $"Load asset at path ({assetPath}) with guid({iterObjectIdentifier.guid}) failed.");
                        }
                    }
                }
            }

            private void FindBeDependencies()
            {
                m_BeDependenciesObject.Clear();

                if (m_AssetBeDependencies == null
                    || m_Config.TargetAssetType != TargetAssetType.ObjectIdentifier)
                {
                    return;
                }

                List<string> handledAssetGUIDs = new List<string>();
                ParallelLoopResult parallelLoopResult = Parallel.For(0, m_AssetBeDependencies.Count, (iAsset) =>
                {
                    AssetBeDependency assetBeDependency = m_AssetBeDependencies[iAsset];
                    bool found = false;

                    for (int iDependency = 0; iDependency < assetBeDependency.Dependencies.Length; iDependency++)
                    {
                        MyObjectIdentifier objectIdentifier = assetBeDependency.Dependencies[iDependency];
                        bool isTarget = true;
                        if (isTarget
                            && (m_Config.ObjectIdentifier_SearchFlag & ObjectIdentifierSearchFlag.GUID) > 0)
                        {
                            isTarget = m_Config.ObjectIdentifier_GUID == objectIdentifier.GUID;
                        }
                        if (isTarget
                            && (m_Config.ObjectIdentifier_SearchFlag & ObjectIdentifierSearchFlag.FileID) > 0)
                        {
                            isTarget = m_Config.ObjectIdentifier_FileID == objectIdentifier.FileID;
                        }
                        if (isTarget
                            && (m_Config.ObjectIdentifier_SearchFlag & ObjectIdentifierSearchFlag.FileType) > 0)
                        {
                            isTarget = m_Config.ObjectIdentifier_FileType == objectIdentifier.FileType;
                        }
                        if (isTarget
                            && (m_Config.ObjectIdentifier_SearchFlag & ObjectIdentifierSearchFlag.FilePath) > 0)
                        {
                            isTarget = m_Config.ObjectIdentifier_FilePath == objectIdentifier.FilePath;
                        }

                        if (isTarget)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        lock (handledAssetGUIDs)
                        {
                            handledAssetGUIDs.Add(assetBeDependency.AssetGUID);
                        }
                    }
                });
                while (!parallelLoopResult.IsCompleted)
                {
                }

                AssetUtility.LoadMainAssetAtGUIDs(m_BeDependenciesObject, handledAssetGUIDs);
            }

            private string GetAssetBeDependenciesPath()
            {
                return Directory.GetParent(Application.dataPath).FullName
                    + @"\Library\AssetBeDependency.bin";
            }

            private void SaveAssetBeDependenciesToFile()
            {
                string path = GetAssetBeDependenciesPath();
                try
                {
                    FileUtility.WriteToBinaryFile(path, m_AssetBeDependencies);
                    MDebug.Log("Asset", $"保存资源索引表到({MDebug.FormatPathToHyperLink(path)})成功");
                }
                catch (Exception e)
                {
                    MDebug.LogError("Asset", $"保存({path})失败, Exception:\n{e.ToString()}");
                }
            }

            private void SaveAssetBeDependenciesToFileForRead()
            {
                string path = Directory.GetParent(Application.dataPath).FullName
                    + @"\Library\AssetBeDependency.json";
                try
                {
                    List<AssetBeDependency> assetBeDependencies = new List<AssetBeDependency>();
                    for (int iAsset = 0; iAsset < m_AssetBeDependencies.Count; iAsset++)
                    {
                        AssetBeDependency originAsset = m_AssetBeDependencies[iAsset];
                        AssetBeDependency newAsset = new AssetBeDependency();
                        newAsset.AssetGUID = AssetDatabase.GUIDToAssetPath(originAsset.AssetGUID);
                        newAsset.Dependencies = new MyObjectIdentifier[originAsset.Dependencies.Length];

                        for (int iDependency = 0; iDependency < newAsset.Dependencies.Length; iDependency++)
                        {
                            MyObjectIdentifier originDependency = originAsset.Dependencies[iDependency];
                            MyObjectIdentifier newDependency = new MyObjectIdentifier();
                            newDependency.GUID = AssetUtility.IsBuiltinOrLibraryWithAssetGUID(originDependency.GUID)
                                ? originDependency.GUID
                                : AssetDatabase.GUIDToAssetPath(originDependency.GUID);
                            newDependency.FileID = originDependency.FileID;
                            newDependency.FileType = originDependency.FileType;
                            newDependency.FilePath = originDependency.FilePath;

                            newAsset.Dependencies[iDependency] = newDependency;
                        }

                        assetBeDependencies.Add(newAsset);
                    }

                    FileUtility.WriteToJsonFile(path, assetBeDependencies);
                    MDebug.Log("Asset", $"保存资源索引表到({MDebug.FormatPathToHyperLink(path)})成功");

                    EditorUtility.RevealInFinder(path);
                    EditorUtility.OpenWithDefaultApp(path);
                }
                catch (Exception e)
                {
                    MDebug.LogError("Asset", $"保存({path})失败, Exception:\n{e.ToString()}");
                }
            }

            private void LoadAssetBeDependenciesFromFile()
            {
                if (m_AssetBeDependencies == null)
                {
                    return;
                }

                string path = GetAssetBeDependenciesPath();
                try
                {
                    m_AssetBeDependencies = FileUtility.ReadFromBinaryFile(path) as List<AssetBeDependency>;
                    MDebug.Log("Asset", $"加载资源索引表({MDebug.FormatPathToHyperLink(path)})成功");

                    if (m_Config.AutoSearch)
                    {
                        FindBeDependencies();
                    }
                }
                catch (Exception e)
                {
                    MDebug.LogError("AssetDB", $"解析({path})失败, Exception:\n{e.ToString()}");
                }
            }

            public struct Config
            {
                /// <summary>
                /// 因为通过ObjectIdentifier查找的速度比较慢，所以添加这个选项
                /// </summary>
                public bool AutoSearch;

                public TargetAssetType TargetAssetType;

                public UnityEngine.Object Asset;

                public ObjectIdentifierSearchFlag ObjectIdentifier_SearchFlag;
                public string ObjectIdentifier_GUID;
                public long ObjectIdentifier_FileID;
                public UnityEditor.Build.Content.FileType ObjectIdentifier_FileType;
                public string ObjectIdentifier_FilePath;

                public override bool Equals(object obj)
                {
                    if (!(obj is Config))
                    {
                        return false;
                    }

                    var config = (Config)obj;
                    return AutoSearch == config.AutoSearch &&
                           TargetAssetType == config.TargetAssetType &&
                           EqualityComparer<UnityEngine.Object>.Default.Equals(Asset, config.Asset) &&
                           ObjectIdentifier_SearchFlag == config.ObjectIdentifier_SearchFlag &&
                           ObjectIdentifier_GUID == config.ObjectIdentifier_GUID &&
                           ObjectIdentifier_FileID == config.ObjectIdentifier_FileID &&
                           ObjectIdentifier_FileType == config.ObjectIdentifier_FileType &&
                           ObjectIdentifier_FilePath == config.ObjectIdentifier_FilePath;
                }

                public override int GetHashCode()
                {
                    var hashCode = 1557769856;
                    hashCode = hashCode * -1521134295 + AutoSearch.GetHashCode();
                    hashCode = hashCode * -1521134295 + TargetAssetType.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<UnityEngine.Object>.Default.GetHashCode(Asset);
                    hashCode = hashCode * -1521134295 + ObjectIdentifier_SearchFlag.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ObjectIdentifier_GUID);
                    hashCode = hashCode * -1521134295 + ObjectIdentifier_FileID.GetHashCode();
                    hashCode = hashCode * -1521134295 + ObjectIdentifier_FileType.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ObjectIdentifier_FilePath);
                    return hashCode;
                }

                public static bool operator ==(Config a, Config b)
                {
                    return a.Equals(b);
                }

                public static bool operator !=(Config a, Config b)
                {
                    return !(a == b);
                }
            }

            public enum TargetAssetType
            {
                Asset,
                ObjectIdentifier,
            }

            [Flags]
            public enum ObjectIdentifierSearchFlag
            {
                GUID = 1 << 0,
                FileID = 1 << 1,
                FileType = 1 << 2,
                FilePath = 1 << 3,
            }

            [Serializable]
            public struct AssetBeDependency
            {
                public string AssetGUID;
                public MyObjectIdentifier[] Dependencies;
            }

            [Serializable]
            public struct MyObjectIdentifier
            {
                public string GUID;
                public long FileID;
                public UnityEditor.Build.Content.FileType FileType;
                public string FilePath;

                public static MyObjectIdentifier[] Convert(System.Collections.IEnumerable objectIdentifiers)
                {
                    List<MyObjectIdentifier> myObjectIdentifiers = new List<MyObjectIdentifier>();
                    foreach (UnityEditor.Build.Content.ObjectIdentifier iterObject in objectIdentifiers)
                    {
                        myObjectIdentifiers.Add(new MyObjectIdentifier(iterObject));
                    }
                    return myObjectIdentifiers.ToArray();
                }

                public MyObjectIdentifier(UnityEditor.Build.Content.ObjectIdentifier objectIdentifier)
                {
                    GUID = objectIdentifier.guid.ToString();
                    FileID = objectIdentifier.localIdentifierInFile;
                    FileType = objectIdentifier.fileType;
                    FilePath = objectIdentifier.filePath;
                }
            }
        }

        private class NonBeDependencyTab : Tab
        {
            /// <summary>
            /// SerializedObject
            /// </summary>
            private SerializedObject m_SO_ForGUI;
            private SerializedProperty m_SP_TargetExtensions;
            /// <summary>
            /// 为了GUI显示
            /// </summary>
            private ForGUI m_ForGUI;
            /// <summary>
            /// null: 整个项目
            /// </summary>
            private string m_TargetDirectory;
            /// <summary>
            /// 是否打开R的Foldout
            /// </summary>
            private bool m_ReferencesFoldout = false;
            /// <summary>
            /// R的Object
            /// </summary>
            private List<UnityEngine.Object> m_ReferencesBuffer;
            /// <summary>
            /// R的ScrollPosition
            /// </summary>
            private Vector2 m_ReferenceScrollPosition = Vector2.zero;

            public NonBeDependencyTab(string text)
                : base(text)
            {
            }

            public override void Initialize()
            {
                base.Initialize();

                m_ForGUI = new ForGUI();
                m_SO_ForGUI = new SerializedObject(m_ForGUI);
                m_SP_TargetExtensions = m_SO_ForGUI.FindProperty("TargetExtensions");

                m_ReferencesBuffer = new List<UnityEngine.Object>();
            }

            public override void Release()
            {
                m_ForGUI = null;
                m_SO_ForGUI = null;

                m_ReferencesBuffer = null;

                base.Release();
            }

            public override void OnGUI()
            {
                EditorGUILayout.BeginHorizontal();
                m_TargetDirectory = EditorGUILayout.TextField("目录", m_TargetDirectory);
                if (GUILayout.Button("……", GUILayout.Width(24)))
                {
                    m_TargetDirectory = EditorUtility.OpenFolderPanel("选择目录", Application.dataPath, "");
                    m_TargetDirectory = m_TargetDirectory.Replace(Application.dataPath, "Assets/");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(m_SP_TargetExtensions, true);
                if (m_SP_TargetExtensions.isExpanded)
                {
                    EditorGUILayout.BeginHorizontal();
                    DoGUIButton_InsertValueStringArray(m_SP_TargetExtensions, "+ Shader", ".shader");
                    DoGUIButton_InsertValueStringArray(m_SP_TargetExtensions, "+ Material", ".mat");
                    DoGUIButton_InsertValueStringArray(m_SP_TargetExtensions, "+ Texture", ".tga");
                    DoGUIButton_InsertValueStringArray(m_SP_TargetExtensions, "+ Prefab", ".prefab");
                    DoGUIButton_InsertValueStringArray(m_SP_TargetExtensions, "+ Scene", ".unity");
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("移除重复和无效"))
                    {
                        RemoveEmptyAndRepeateInStringArray(m_SP_TargetExtensions);
                    }
                    if (GUILayout.Button("清空"))
                    {
                        m_SP_TargetExtensions.arraySize = 0;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                if (GUILayout.Button("查找"))
                {
                    DoSearch();
                }

                m_ReferencesFoldout = EditorGUILayout.Foldout(m_ReferencesFoldout, "无用资源数量：" + m_ReferencesBuffer.Count);
                if (m_ReferencesFoldout)
                {
                    EditorGUI.indentLevel++;
                    m_ReferenceScrollPosition = EditorGUILayout.BeginScrollView(m_ReferenceScrollPosition);
                    for (int iReference = 0; iReference < m_ReferencesBuffer.Count; iReference++)
                    {
                        EditorGUILayout.ObjectField(m_ReferencesBuffer[iReference], typeof(UnityEngine.Object), false);
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUI.indentLevel--;
                }

                m_SO_ForGUI.ApplyModifiedProperties();
                EditorUtility.SetDirty(m_ForGUI);
            }

            private void DoSearch()
            {
                HashSet<string> targetExtensions = new HashSet<string>(m_ForGUI.TargetExtensions);
                List<string> resultAssetPaths = new List<string>();

                BetterDictionary<string, Asset>.Entry[] assets = DB.GetInstance().GetAssets().GetEntries();
                for (int iAsset = 0; iAsset < assets.Length; iAsset++)
                {
                    BetterDictionary<string, Asset>.Entry iterEntry = assets[iAsset];
                    if (iterEntry.hashCode == -1)
                    {
                        continue;
                    }

                    string assetPath = AssetDatabase.GUIDToAssetPath(iterEntry.key);
                    if (!string.IsNullOrEmpty(m_TargetDirectory)
                        && !assetPath.StartsWith(m_TargetDirectory))
                    {
                        continue;
                    }

                    if (targetExtensions.Count > 0
                        && !targetExtensions.Contains(Path.GetExtension(assetPath)))
                    {
                        continue;
                    }

                    if (iterEntry.value.BeDependencies.Count == 0)
                    {
                        resultAssetPaths.Add(assetPath);
                    }
                }

                m_ReferencesBuffer.Clear();
                AssetUtility.LoadMainAssetAtPaths(m_ReferencesBuffer, resultAssetPaths);
            }

            private void DoGUIButton_InsertValueStringArray(SerializedProperty array, string display, string value)
            {
                if (GUILayout.Button(display))
                {
                    for (int iValue = 0; iValue < array.arraySize; iValue++)
                    {
                        SerializedProperty iterSP = array.GetArrayElementAtIndex(iValue);
                        if (string.IsNullOrEmpty(iterSP.stringValue))
                        {
                            iterSP.stringValue = value;
                            return;
                        }
                    }
                    array.arraySize++;
                    array.GetArrayElementAtIndex(array.arraySize - 1).stringValue = value;
                }
            }

            private void RemoveEmptyAndRepeateInStringArray(SerializedProperty array)
            {
                HashSet<string> hashset = new HashSet<string>();
                for (int iValue = 0; iValue < array.arraySize; iValue++)
                {
                    string iterValue = array.GetArrayElementAtIndex(iValue).stringValue;
                    if (string.IsNullOrEmpty(iterValue)
                        || !hashset.Add(iterValue))
                    {
                        array.DeleteArrayElementAtIndex(iValue);
                        iValue--;
                        continue;
                    }
                }
            }

            private class ForGUI : ScriptableObject
            {
                /// <summary>
                /// 为了GUI显示
                /// </summary>
                [Tooltip("目标的扩展名")]
                public List<string> TargetExtensions = new List<string>();
            }
        }
    }
}