using GF.Common.Debug;
using GF.Common.Utility;
using GF.XLuaFramework;
using System;
using System.IO;
using UnityEditor;

namespace GFEditor.XLuaFramework
{
    public class LuaScriptPostprocessor : AssetPostprocessor
    {
        public static void ReimportAllLuaScript()
        {
            string root = XLuaSetting.GetInstance().ExportedLuaRoot;
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }

            Directory.CreateDirectory(root);
            string[] emptyStrings = new string[0];
            OnPostprocessAllAssets(AssetDatabase.GetAllAssetPaths()
                , emptyStrings
                , emptyStrings
                , emptyStrings);
        }

        private static void OnPostprocessAllAssets(string[] importedAssets
            , string[] deletedAssets
            , string[] movedAssets
            , string[] movedFromAssetPaths)
        {
            if (string.IsNullOrEmpty(XLuaSetting.GetInstance().ExportedLuaRoot))
            {
                return;
            }

            bool changed = false;
            try
            {
                for (int iAsset = 0; iAsset < importedAssets.Length; iAsset++)
                {
                    string iterAsset = importedAssets[iAsset];
                    if (!IsLuaScript(iterAsset))
                    {
                        continue;
                    }

                    CreateLua(iterAsset);
                    changed = true;
                }

                for (int iAsset = 0; iAsset < deletedAssets.Length; iAsset++)
                {
                    string iterAsset = deletedAssets[iAsset];
                    if (!IsLuaScript(iterAsset))
                    {
                        continue;
                    }

                    changed |= DeleteLua(iterAsset);
                }

                for (int iAsset = 0; iAsset < movedAssets.Length; iAsset++)
                {
                    string iterAsset = movedFromAssetPaths[iAsset];
                    if (IsLuaScript(iterAsset))
                    {
                        changed |= DeleteLua(iterAsset);
                    }

                    iterAsset = movedAssets[iAsset];
                    if (IsLuaScript(iterAsset))
                    {
                        CreateLua(iterAsset);
                        changed = true;
                    }
                }
            }
            catch (Exception e)
            {
                MDebug.LogError("XLua", "Postprocess lua script Exception:\n" + e.ToString());
            }
            finally
            {
                if (changed)
                {
                    AssetDatabase.Refresh(ImportAssetOptions.Default);
                }
            }
        }

        private static void CreateLua(string sourceLuaPath)
        {
            string exportedLuaPath = ConvertLuaPathSourceToExported(sourceLuaPath);
            string exportedFoloder = Path.GetDirectoryName(exportedLuaPath);
            if (!Directory.Exists(exportedFoloder))
            {
                Directory.CreateDirectory(exportedFoloder);
            }
            AssetDatabase.CopyAsset(sourceLuaPath, exportedLuaPath);
            MDebug.Log("XLua", "Reimport lua script: " + exportedLuaPath);
        }

        private static bool DeleteLua(string sourceLuaPath)
        {
            string exportedLuaPath = ConvertLuaPathSourceToExported(sourceLuaPath);
            if (File.Exists(exportedLuaPath))
            {
                File.Delete(exportedLuaPath);
                MDebug.Log("XLua", "Delete lua script: " + exportedLuaPath);
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsLuaScript(string assetPath)
        {
            return assetPath.Contains(XLuaSetting.LUA_SCRIPT_SIGN)
                && assetPath.EndsWith(XLuaSetting.LUA_SOURCE_EXTENSION);
        }

        private static string ConvertLuaPathSourceToExported(string sourceLuaPath)
        {
            string exportedLuaPath = string.Format("{0}/{1}{2}"
                , XLuaSetting.GetInstance().ExportedLuaRoot
                , sourceLuaPath.Substring(sourceLuaPath.LastIndexOf(XLuaSetting.LUA_SCRIPT_SIGN) + XLuaSetting.LUA_SCRIPT_SIGN.Length)
                , XLuaSetting.LUA_EXPORTED_EXTENSION);
            return exportedLuaPath;
        }
    }
}