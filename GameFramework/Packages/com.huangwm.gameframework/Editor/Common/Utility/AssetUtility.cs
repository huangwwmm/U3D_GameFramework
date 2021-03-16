using GF.Common.Debug;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Player;

namespace GFEditor.Common.Utility
{
    public static class AssetUtility
    {
        public const string BUILTIN_GUID = "0000000000000000f000000000000000";
        public const string LIBRARY_GUID = "0000000000000000e000000000000000";

        /// <summary>
        /// 判断Asset是否是Builtin或Library目录下的
        /// </summary>
        public static bool IsBuiltinOrLibraryAsset(UnityEngine.Object asset)
        {
            return IsBuiltinOrLibraryWithAssetPath(AssetDatabase.GetAssetPath(asset));
        }

        /// <summary>
        /// 判断Asset是否是Builtin或Library目录下的
        /// </summary>
        public static bool IsBuiltinOrLibraryAsset(ObjectIdentifier objectIdentifier)
        {
            return IsBuiltinOrLibraryWithAssetGUID(objectIdentifier.guid.ToString());
        }

        /// <summary>
        /// 判断Asset是否是Builtin或Library目录下的
        /// </summary>
        public static bool IsBuiltinOrLibraryWithAssetPath(string assetPath)
        {
            return IsBuiltinOrLibraryWithAssetGUID(AssetDatabase.AssetPathToGUID(assetPath));
        }

        /// <summary>
        /// 判断Asset是否是Builtin或Library目录下的
        /// </summary>
        public static bool IsBuiltinOrLibraryWithAssetGUID(string assetGUID)
        {
            return assetGUID == BUILTIN_GUID
                || assetGUID == LIBRARY_GUID;
        }

        /// <param name="assets">加载出来的资源</param>
        /// <param name="paths">路径</param>
        public static void LoadMainAssetAtPaths(List<UnityEngine.Object> assets
            , IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null)
                {
                    MDebug.LogError("Asset", $"Load asset at ({path}) failed.");
                    continue;
                }

                assets.Add(asset);
            }
        }

        /// <param name="assets">加载出来的资源</param>
        /// <param name="guids">GUID</param>
        public static void LoadMainAssetAtGUIDs(List<UnityEngine.Object> assets, IEnumerable<string> guids)
        {
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (asset == null)
                {
                    MDebug.LogError("Asset", $"Load asset at ({assetPath}) failed.");
                    continue;
                }

                assets.Add(asset);
            }
        }


        /// <summary>
        /// <see cref="SearchAssetDependenciesWithAssetGUID"/>
        /// </summary>
        public static ObjectIdentifier[] SearchAssetDependencies(UnityEngine.Object asset, TypeDB typeDB = null, BuildTarget buildTarget = BuildTarget.NoTarget)
        {
            return SearchAssetDependenciesWithAssetPath(AssetDatabase.GetAssetPath(asset), typeDB, buildTarget);
        }

        /// <summary>
        /// <see cref="SearchAssetDependenciesWithAssetGUID"/>
        /// </summary>
        public static ObjectIdentifier[] SearchAssetDependenciesWithAssetPath(string assetPath, TypeDB typeDB = null, BuildTarget buildTarget = BuildTarget.NoTarget)
        {
            return SearchAssetDependenciesWithAssetGUID(AssetDatabase.AssetPathToGUID(assetPath), typeDB, buildTarget);
        }

        /// <summary>
        /// 查找某个资源引用到的资源
        /// </summary>
        /// <param name="typeDB">可以通过<see cref="EditorExtend.CompilePlayerScriptsUtility.CompileCurrentTargetTypeDB"/>获取</param>
        /// <param name="buildTarget">如果为NoTarget 则使用当前的平台</param>
        public static ObjectIdentifier[] SearchAssetDependenciesWithAssetGUID(string assetGUID, TypeDB typeDB = null, BuildTarget buildTarget = BuildTarget.NoTarget)
        {
            if (buildTarget == BuildTarget.NoTarget)
            {
                buildTarget = EditorUserBuildSettings.activeBuildTarget;
            }

            if (typeDB == null)
            {
                CompilePlayerScriptsUtility.GetOrCompileCurrentTargetTypeDB(false, out typeDB);
            }

            ObjectIdentifier[] assetIncludes = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(new GUID(assetGUID), buildTarget);
            return ContentBuildInterface.GetPlayerDependenciesForObjects(assetIncludes, buildTarget, typeDB);
        }

    }
}