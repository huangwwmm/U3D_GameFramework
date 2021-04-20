using System;
using System.Collections.Generic;
using GFEditor.Common.Data;

namespace GFEditor.Common.FolderLink
{
    [Serializable]
    public class FolderLinkSetting : BaseProjectSetting
    {
        public const string SETTING_NAME = "FolderLinkSetting";

        private static FolderLinkSetting ms_Instance;

        public List<Item> Items = new List<Item>();

        public static FolderLinkSetting GetInstance()
        {
            if (ms_Instance == null)
            {
                ms_Instance = Load<FolderLinkSetting>(SETTING_NAME);
            }
            return ms_Instance;
        }

        protected override string GetSettingName()
        {
            return SETTING_NAME;
        }
    }
}