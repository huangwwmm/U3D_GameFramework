using UnityEditor;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.IO;
using System.Text;
using GFEditor.Asset.Rule;
using GF.Common;
using GF.Common.Debug;
using System.Linq;
using LitJson;
using GF.Asset;

namespace GFEditor.Asset.Build
{
    public static class Builder
    {
        /// <summary>
        /// <see cref="BaseRule"/>文件名的分隔符
        /// </summary>
        private const char RULE_FILENAME_SPLIT = '_';
        /// <summary>
        /// <see cref="BaseRule"/>启用时的标记字符
        /// </summary>
        private const string RULE_ENABLE_SIGN = "E";

        private static Context ms_Context;

        public static void Build()
        {
            bool success = false;
            try
            {
                ms_Context = new Context();
                AssetBundleBuild[] assetBundleBuild = GenerateAssetBundle();
				success = BuildAB(assetBundleBuild, out AssetBundleManifest assetBundleManifest);

				if (success)
				{
					GenerateAssetMapAndAssetKeyEnumFile();
				}

				if (success && BuildSetting.GetInstance().BuildAssetBuild)
				{
					GenerateBundleInfosAndAssetInfosAndAssetKeyEnumFile(assetBundleManifest);
				}
			}
			catch (Exception e)
            {
                MDebug.LogError("AssetBundle", "Build AssetBundle Exception:\n" + e.ToString());
            }
            finally
            {
                ms_Context = null;
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("AssetBundle"
                    , "Build AB " + (success ? "Success" : "Failed")
                    , "OK");
            }
        }

        public static AssetBundleBuild[] GenerateAssetBundle()
        {
            BuildSetting setting = BuildSetting.GetInstance();

            try
            {
                string directory = Path.GetDirectoryName(setting.GetFormatedBundleBuildsPath());
                if (!string.IsNullOrEmpty(directory)
                    && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception)
            {
                // 失败也不影响，不需要handle
            }

            AssetBundleBuild[] assetBundleBuilds = null;
            //启用根据AssetBundleBuilds.json文件，构建AssetBundleBuild数组，加速打包，适用于没有打包配置更改的情景
            if (setting.UseCachedBuild)
            {
                try
                {
                    string json = File.ReadAllText(setting.GetFormatedBundleBuildsPath());
                    if (!string.IsNullOrEmpty(json))
                    {
                        assetBundleBuilds = JsonMapper.ToObject<AssetBundleBuild[]>(json);
                    }
                }
                catch (Exception)
                {
                    // 没有cache就重新计算，不需要handle
                }
            }

            if (assetBundleBuilds != null)
            {
                Dictionary<string, string> assetToBundle = new Dictionary<string, string>();
                for (int iBundle = 0; iBundle < assetBundleBuilds.Length; iBundle++)
                {
                    AssetBundleBuild iterBundle = assetBundleBuilds[iBundle];
                    for (int iAsset = 0; iAsset < iterBundle.assetNames.Length; iAsset++)
                    {
                        assetToBundle[iterBundle.assetNames[iAsset]] = iterBundle.assetBundleName;
                    }
                }
                //启用设置AssetImporter更改文件BundleName和Varient
                if (setting.ResetBundleName)
                {
                    ResetBundleName(assetToBundle);
                }
                return assetBundleBuilds;
            }

            List<BaseRule> rules = CollectRule();
            try
            {
                for (int iRule = 0; iRule < rules.Count; iRule++)
                {
                    rules[iRule].Execute(ms_Context);
                }

                if (setting.ResetBundleName)
                {
                    ResetBundleName(ms_Context.GetAssetToBundle());
                }
                else
                {
                    assetBundleBuilds = ms_Context.GenerateAssetBundleBuild();
                }
            }
            catch (Exception e)
            {
                MDebug.LogError("AssetBundle", e.ToString());
                EditorUtility.DisplayDialog("Builder"
                    , "Build AB Failed\n" + e.ToString()
                    , "OK");
                throw e;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            try
            {
                File.WriteAllText(setting.GetFormatedBundleBuildsPath(), LitJson.JsonMapper.ToJson(assetBundleBuilds));
            }
            catch (Exception)
            {
                // 写失败了也不影响，不需要handle
            }

            return assetBundleBuilds;
        }

        public static void ClearAllBundleName()
        {
            string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();
            for (int iBundle = 0; iBundle < bundleNames.Length; iBundle++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Builder"
                    , $"Clear all bundle name {iBundle}/{bundleNames.Length}"
                    , (float)iBundle / bundleNames.Length))
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }

                AssetDatabase.RemoveAssetBundleName(bundleNames[iBundle], true);
            }
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Builder"
                , "清除BundleName完成"
                , "OK");
        }

        public static void ResetBundleName(Dictionary<string, string> assetToBundle)
        {
            long elapsedMS = MDebug.GetMillisecondsSinceStartup();
            string[] assetPaths = AssetDatabase.GetAllAssetPaths();
            int modifyAssetCount = 0;
            for (int iAsset = 0; iAsset < assetPaths.Length; iAsset++)
            {
                if (iAsset % 100 == 0
                    && EditorUtility.DisplayCancelableProgressBar("AssetBundle"
                    , $"Reset bundleName {iAsset}/{assetPaths.Length}. Modified {modifyAssetCount}", iAsset / (float)assetPaths.Length))
                {
                    throw new CancelException();
                }

                string iterAssetPath = assetPaths[iAsset];
                // 没有meta文件的不需要设置bundleName
                if (!File.Exists(iterAssetPath + ".meta"))
                {
                    continue;
                }
                AssetImporter iterAssetImporter = AssetImporter.GetAtPath(iterAssetPath);
                if (!assetToBundle.TryGetValue(iterAssetPath, out string bundleName))
                {
                    bundleName = string.Empty;
                }
				
				if (iterAssetImporter.assetBundleName != bundleName)
				{
					modifyAssetCount++;
					iterAssetImporter.assetBundleName = bundleName;
					EditorUtility.SetDirty(iterAssetImporter);
				}

				//1000间隔,执行一次IO操作，提高性能,如果中断可能会出现问题，比如BuildPipling获取不到一些已经赋值的BuildName
				if (iAsset % 1000 == 0)
                {
                    AssetDatabase.SaveAssets();
                    Resources.UnloadUnusedAssets();
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            Resources.UnloadUnusedAssets();
			AssetDatabase.Refresh();
            elapsedMS = MDebug.GetMillisecondsSinceStartup() - elapsedMS;
            MDebug.Log("AssetBundle"
                , $"Modify bundleName count ({modifyAssetCount}) elapsed {MDebug.FormatMilliseconds(elapsedMS)}");
        }

        public static bool BuildAB(AssetBundleBuild[] assetBundleBuilds, out AssetBundleManifest assetBundleManifest)
        {
            BuildSetting setting = BuildSetting.GetInstance();
            if (setting.BuildAssetBuild)
            {
                if (setting.DeleteOutputBeforeBuild
                    && Directory.Exists(setting.GetFormatedBuildOutput()))
                {
                    Directory.Delete(setting.GetFormatedBuildOutput(), true);
                }
                if (!Directory.Exists(setting.GetFormatedBuildOutput()))
                {
                    Directory.CreateDirectory(setting.GetFormatedBuildOutput());
                }
                if (File.Exists(setting.GetFormatedBundleInfoPath()))
                {
                    File.Delete(setting.GetFormatedBundleInfoPath());
                }
                string bundleMapDirectory = Path.GetDirectoryName(setting.GetFormatedBundleInfoPath());
                if (!Directory.Exists(bundleMapDirectory))
                {
                    Directory.CreateDirectory(bundleMapDirectory);
                }

                if (setting.ResetBundleName)
                {
                    EditorUtility.DisplayProgressBar("Builder", "BuildPipeline.BuildAssetBundles", 0);
                    assetBundleManifest = BuildPipeline.BuildAssetBundles(setting.GetFormatedBuildOutput()
                        , setting.BuildAssetBundleOptions
                        , EditorUserBuildSettings.activeBuildTarget);
                }
                else
                {
                    EditorUtility.DisplayProgressBar("Builder", "BuildPipeline.BuildAssetBundles", 0);
                    assetBundleManifest = BuildPipeline.BuildAssetBundles(setting.GetFormatedBuildOutput()
                        , assetBundleBuilds
                        , setting.BuildAssetBundleOptions
                        , EditorUserBuildSettings.activeBuildTarget);
                }

                return assetBundleManifest != null;
            }
            else
            {
                assetBundleManifest = null;
                return true;
            }
        }

        /// <summary>
        /// 收集Rule
        /// </summary>
        /// <param name="enabled">只收集开启的</param>
        /// <param name="sorted">收集后排序</param>
        public static List<BaseRule> CollectRule(bool enabled = true, bool sorted = true)
        {
            List<BaseRule> rules = new List<BaseRule>();

            string[] ruleGuids = AssetDatabase.FindAssets("t:BaseRule");
            for (int iRule = 0; iRule < ruleGuids.Length; iRule++)
            {
                string iterRuleGuid = ruleGuids[iRule];
                string iterRuleAssetPath = AssetDatabase.GUIDToAssetPath(iterRuleGuid);
                BaseRule iterRule = AssetDatabase.LoadMainAssetAtPath(iterRuleAssetPath) as BaseRule;
                if (iterRule == null)
                {
                    continue;
                }

                try
                {
                    string[] ruleNameSplits = iterRule.name.Split(RULE_FILENAME_SPLIT);
                    iterRule._Enable = ruleNameSplits[0] == RULE_ENABLE_SIGN;
                    iterRule._Order = int.Parse(ruleNameSplits[1]);
                    iterRule._Name = ruleNameSplits[2];
                }
                catch (Exception e)
                {
                    MDebug.LogError("AssetBundle"
                        , $"规则({iterRule.name})不符合的命名规则");
                    throw e;
                }

                if ((enabled && iterRule._Enable)
                    || !enabled)
                {
                    rules.Add(iterRule);
                }
            }

            rules.Sort(BaseRule.Comparison);
            return rules;
        }

        private static GF.Asset.AssetInfo[] GenerateAssetMapAndAssetKeyEnumFile(Dictionary<string, int> bundleNameToIndex = null)
        {
            StringBuilder assetKeyEnumBuilder = new StringBuilder();
            assetKeyEnumBuilder.Append("namespace GF.Asset\n");
            assetKeyEnumBuilder.Append("{\n");
            assetKeyEnumBuilder.Append("\tpublic enum AssetKey\n");
            assetKeyEnumBuilder.Append("\t{\n");
            Dictionary<string, AssetInfo> assetKeyToAssetInfo = ms_Context.GetAssetKeyToAssetInfo();
            GF.Asset.AssetInfo[] assetInfos = new GF.Asset.AssetInfo[assetKeyToAssetInfo.Count];
            int iAsset = 0;
            foreach (KeyValuePair<string, AssetInfo> kv in assetKeyToAssetInfo)
            {
                assetInfos[iAsset++] = new GF.Asset.AssetInfo(kv.Value.AssetPath
                    , bundleNameToIndex != null ? bundleNameToIndex[kv.Value.BundleName] : 0);

                assetKeyEnumBuilder.Append($"\t\t{GF.Common.Utility.StringUtility.FormatToVariableName(kv.Key)},\n");
            }

            assetKeyEnumBuilder.Append("\t}\n")
                .Append("}");

            File.WriteAllText(BuildSetting.GetInstance().GetFormateAssetInfosPath(), JsonMapper.ToJson(assetInfos));
            File.WriteAllText(BuildSetting.GetInstance().GetFormateAssetKeyEnumFilePath(), assetKeyEnumBuilder.ToString());

            return assetInfos;
        }

        private static void GenerateBundleInfosAndAssetInfosAndAssetKeyEnumFile(AssetBundleManifest assetBundleManifest)
        {
            string[] assetBundles = assetBundleManifest.GetAllAssetBundles();
            BundleInfo[] bundleInfos = new BundleInfo[assetBundles.Length];
            List<int> bundleDependenceIndexsCache = new List<int>();
            HashSet<int> alreadyAddedBundleDependencesCache = new HashSet<int>();
            Dictionary<string, string[]> bundleDependencesMap = new Dictionary<string, string[]>();
            Dictionary<string, int> bundleNameToIndex = new Dictionary<string, int>(assetBundles.Length);

            for (int assetBundlesIndex = 0; assetBundlesIndex < assetBundles.Length; assetBundlesIndex++)
            {
                bundleNameToIndex.Add(assetBundles[assetBundlesIndex], assetBundlesIndex);
            }

            for (int iBundle = 0; iBundle < assetBundles.Length; iBundle++)
            {
                string bundleName = assetBundles[iBundle];
                bundleDependencesMap.Add(bundleName, assetBundleManifest.GetDirectDependencies(bundleName));
            }

            for (int iBundle = 0; iBundle < assetBundles.Length; iBundle++)
            {
                alreadyAddedBundleDependencesCache.Clear();
                bundleDependenceIndexsCache.Clear();

                string bundleName = assetBundles[iBundle];
                AddBundleDependence(bundleName, bundleDependencesMap, bundleNameToIndex, bundleDependenceIndexsCache, alreadyAddedBundleDependencesCache);
                bundleDependenceIndexsCache.RemoveAt(bundleDependenceIndexsCache.Count - 1);
                bundleInfos[iBundle].BundleName = assetBundles[iBundle];
                bundleInfos[iBundle].DependencyBundleIndexs = bundleDependenceIndexsCache.ToArray();
            }

            GF.Asset.AssetInfo[] assetInfos = GenerateAssetMapAndAssetKeyEnumFile(bundleNameToIndex);
            Dictionary<int, List<int>> bundleToAssets = new Dictionary<int, List<int>>(assetBundles.Length);
            for (int iAsset = 0; iAsset < assetInfos.Length; iAsset++)
            {
                int bundleIndex = assetInfos[iAsset].BundleIndex;
                if (!bundleToAssets.TryGetValue(bundleIndex, out List<int> assets))
                {
                    assets = new List<int>();
                    bundleToAssets[bundleIndex] = assets;
                }

                assets.Add(iAsset);
            }
            foreach (KeyValuePair<int, List<int>> kv in bundleToAssets)
            {
                bundleInfos[kv.Key].DirectyReferenceAssets = kv.Value.ToArray<int>();
            }

            string formatedBundleMapPath = BuildSetting.GetInstance().GetFormatedBundleInfoPath();
            string bundleMapJson = JsonMapper.ToJson(bundleInfos);
            File.WriteAllText(formatedBundleMapPath, bundleMapJson);
        }

        private static void AddBundleDependence(string bundleName
            , Dictionary<string, string[]> bundleDependencesMap
            , Dictionary<string, int> bundleNameToIndex
            , List<int> bundleDependenceIndex
            , HashSet<int> alreadyAddedBundleDependences)
        {
            string[] dependences = bundleDependencesMap[bundleName];
            for (int bundleIndex = 0; bundleIndex < dependences.Length; bundleIndex++)
            {
                AddBundleDependence(dependences[bundleIndex], bundleDependencesMap, bundleNameToIndex, bundleDependenceIndex, alreadyAddedBundleDependences);
            }

            int currentBundelIndex = bundleNameToIndex[bundleName];

            if (alreadyAddedBundleDependences.Add(currentBundelIndex))
            {
                bundleDependenceIndex.Add(currentBundelIndex);
            }
        }
    }
}