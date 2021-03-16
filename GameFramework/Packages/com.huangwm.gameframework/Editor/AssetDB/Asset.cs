using GF.Common.Collection;

namespace GFEditor.AssetDB
{
    [System.Serializable]
    public class Asset
    {
        public BetterList<string> Dependencies = new BetterList<string>();
        public BetterList<string> BeDependencies = new BetterList<string>();
    }
}