using System;

namespace GF.Common
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CTCreateAssetMenuItemAttribute : Attribute
    {
        public string MenuName;
        public int Priority;
        public string DefaultAssetName;

        public CTCreateAssetMenuItemAttribute(string menu)
        {
            MenuName = menu;
            Priority = CTMenuItemAttribute._s_TempProiority++;
        }

        public CTCreateAssetMenuItemAttribute(string menu, string defaultAssetName)
        {
            MenuName = menu;
            Priority = CTMenuItemAttribute._s_TempProiority++;
            DefaultAssetName = defaultAssetName;
        }

        public CTCreateAssetMenuItemAttribute(string menu, int priority)
        {
            MenuName = menu;
            Priority = priority;
        }

        public CTCreateAssetMenuItemAttribute(string menu, int priority, string defaultAssetName)
        {
            MenuName = menu;
            Priority = priority;
            DefaultAssetName = defaultAssetName;
        }
    }
}