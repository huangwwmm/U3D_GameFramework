using System.Collections.Generic;

namespace GFEditor.Common.FolderLink
{
    [System.Serializable]
    public class Item
    {
        public string Name;
        public List<LinkItem> Links;

        public Item(string name)
        {
            Name = name;
            Links = new List<LinkItem>();
        }

        public Item AddLink(LinkItem link)
        {
            Links.Add(link);
            return this;
        }
    }
}