using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GF.Asset.Build
{
    public struct AssetInfo
    {
        public string AssetPath;
        public string BundleName;

        public AssetInfo(string assetPath, string bundleName)
        {
            AssetPath = assetPath;
            BundleName = bundleName;
        }
    }
}