using GF.Common.Debug;
using GFEditor.Common.Data;
using System;
using System.IO;
using UnityEngine;

namespace GFEditor.Renderer.LightmapAttach.Setting
{
    [Serializable]
    public class LightmapAttachSetting : BaseProjectSetting
    {
        public const string SETTING_NAME = "LightmapAttachSetting";

        private static LightmapAttachSetting ms_Instance;

        /// <summary>
        /// 每个Scene对应的<see cref="Exporter.BaseExporter"/>文件保存的目录
        /// </summary>
        public string ExporterFolder;
        public ExporterFileNameType ExporterFileNameType;

        public static LightmapAttachSetting GetInstance()
        {
            if (ms_Instance == null)
            {
                ms_Instance = Load<LightmapAttachSetting>(SETTING_NAME);
            }
            return ms_Instance;
        }

        protected override string GetSettingName()
        {
            return SETTING_NAME;
        }
    }
}