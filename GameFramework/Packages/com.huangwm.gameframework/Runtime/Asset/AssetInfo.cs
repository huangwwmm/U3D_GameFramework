using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GF.Asset
{
    public struct AssetInfo
    {
        public string AssetPath;
        public int BundleIndex;

        public AssetInfo(string assetPath, int bundleIndex)
        {
            AssetPath = assetPath;
            BundleIndex = bundleIndex;
        }
    }
}