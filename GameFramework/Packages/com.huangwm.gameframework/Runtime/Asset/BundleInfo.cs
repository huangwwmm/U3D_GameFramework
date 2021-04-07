using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GF.Asset
{
    public struct BundleInfo
    {
        public string BundleName;
        public int[] DependencyBundleIndexs;
        public int[] DirectyReferenceAssets;
    }
}