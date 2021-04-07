using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFEditor.Asset.Build
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
