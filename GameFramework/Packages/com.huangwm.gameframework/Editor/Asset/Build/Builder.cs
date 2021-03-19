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

        public static void Build()
        {
            bool success = false;
            try
            {
                success = BuildAB(ExecuteAllRule());
            }
            catch (Exception e)
            {
                MDebug.LogError("AssetBundle", "Build AssetBundle Exception:\n" + e.ToString());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("AssetBundle"
                    , "Build AB " + (success ? "Success" : "Failed")
                    , "OK");
            }
        }

        public static AssetBundleBuild[] ExecuteAllRule()
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

            List<BaseRule> rules = CollectRule();
            Context context = new Context();

            AssetBundleBuild[] assetBundleBuilds = null;
            if (setting.UseCachedBuild)
            {
                try
                {
                    string json = File.ReadAllText(setting.GetFormatedBundleBuildsPath());
                    if (!string.IsNullOrEmpty(json))
                    {
                        assetBundleBuilds = LitJson.JsonMapper.ToObject<AssetBundleBuild[]>(json);
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
                if (setting.ResetBundleName)
                {
                    ResetBundleName(assetToBundle);
                }
                return assetBundleBuilds;
            }

            try
            {
                for (int iRule = 0; iRule < rules.Count; iRule++)
                {
                    rules[iRule].Execute(context);
                }

                if (setting.ResetBundleName)
                {
                    ResetBundleName(context.GetAssetToBundle());
                }
                else
                {
                    assetBundleBuilds = context.GenerateAssetBundleBuild();
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

            SaveAssetKeyToBundleMap(context);

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

                if (iAsset % 1000 == 0)
                {
                    AssetDatabase.SaveAssets();
                    Resources.UnloadUnusedAssets();
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            Resources.UnloadUnusedAssets();
            elapsedMS = MDebug.GetMillisecondsSinceStartup() - elapsedMS;
            MDebug.Log("AssetBundle"
                , $"Modify bundleName count ({modifyAssetCount}) elapsed {MDebug.FormatMilliseconds(elapsedMS)}");
        }

        public static bool BuildAB(AssetBundleBuild[] assetBundleBuilds)
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

                if (File.Exists(setting.BundleMapPath))
                {
                    File.Delete(setting.BundleMapPath);
                }
                string bundleMapDirectory = Path.GetDirectoryName(setting.BundleMapPath);
                if (!Directory.Exists(bundleMapDirectory))
                {
                    Directory.CreateDirectory(bundleMapDirectory);
                }

                AssetBundleManifest assetBundleManifest;
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

                if (assetBundleManifest != null)
                {
                    Dictionary<string, string[]> bundleMap = new Dictionary<string, string[]>();
                    for (int iBundle = 0; iBundle < assetBundleBuilds.Length; iBundle++)
                    {
                        string bundleName = assetBundleBuilds[iBundle].assetBundleName;
                        bundleMap.Add(bundleName, assetBundleManifest.GetDirectDependencies(bundleName));
                    }

                    File.WriteAllText(setting.BundleMapPath, JsonMapper.ToJson(bundleMap));
                }

                return assetBundleManifest != null;
            }
            else
            {
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

        /// <summary>
        /// 保存AssetKeyToAsset映射Json文件
        /// </summary>
        /// <param name="context"></param>
        public static void SaveAssetKeyToBundleMap(Context context)
        {
            BuildSetting setting = BuildSetting.GetInstance();
            string path = setting.GetFormateAssetKeyToAssetMapPath();

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            string json = JsonMapper.ToJson(context.GetAssetKeyToAsset());
            File.WriteAllText(path, json);
        }

    }
}