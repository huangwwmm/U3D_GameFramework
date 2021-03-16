using GF.Common;
using GF.Common.Debug;
using GF.Common.Utility;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GFEditor.AssetDB
{
    [CTCreateAssetMenuItem("工具/AssetDB/创建资源处理/示例", "AssetProcess_Example")]
    public class AssetProcessExample : BaseAssetProcess
    {
        public override void ProcessAll(Object[] assets)
        {
            StringBuilder stringBuilder = StringUtility.AllocStringBuilder()
                .AppendLine("处理以下资源:");
            for (int iAsset = 0; iAsset < assets.Length; iAsset++)
            {
                string iterAssetPath = AssetDatabase.GetAssetPath(assets[iAsset]);
                if (string.IsNullOrEmpty(iterAssetPath))
                {
                    continue;
                }
                stringBuilder.AppendLine(iterAssetPath);
            }
            MDebug.Log("Asset", StringUtility.ReleaseStringBuilder(stringBuilder));
        }
    }
}