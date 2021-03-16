namespace GFEditor.Asset.AssetBundle.Rule
{
    public enum BundleNameType
    {
        /// <summary>
        /// 指定的名字
        /// </summary>
        Specify = 1,
        /// <summary>
        /// 所在目录名的格式化
        /// </summary>
        FormatParentFolderName = 2,
        /// <summary>
        /// 所在目录名的格式化
        /// </summary>
        FormatAssetName = 3,
        /// <summary>
        /// 相对路径的格式化
        /// </summary>
        FormatRelativePath = 4,
    }
}
