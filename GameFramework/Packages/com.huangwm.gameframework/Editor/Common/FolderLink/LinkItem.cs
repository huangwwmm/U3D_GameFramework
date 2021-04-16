namespace GFEditor.Common.FolderLink
{
    [System.Serializable]
    public class LinkItem
    {
        public string Link;
        public string Target;
        public bool IsDirectory;

        public LinkItem(string link, string target, bool isDirectory)
        {
            Link = link;
            Target = target;
            IsDirectory = isDirectory;
        }
    }
}