using UnityEditor;
using UnityEngine;

namespace GFEditor.AssetDB
{
    public class MyAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!AssetDBSetting.GetInstance().GetHandleAssetPostprocessor())
            {
                return;
            }

            DB.GetInstance().OnPostprocessAllAssets(importedAssets
                , deletedAssets
                , movedAssets
                , movedFromAssetPaths);
        }
    }
}