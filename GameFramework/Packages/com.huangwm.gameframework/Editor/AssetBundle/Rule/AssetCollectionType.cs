namespace GFEditor.Asset.AssetBundle.Rule
{
    public enum AssetCollectionType
    {
        /// <summary>
        /// 所有资源包括子资源
        /// </summary>
        All = 1,
        /// <summary>
        /// 子节点每个目录一个集合
        /// </summary>
        ChildFolders = 2,
        /// <summary>
        /// 每个子文件是一个集合
        /// </summary>
        Childern = 3,
    }
}