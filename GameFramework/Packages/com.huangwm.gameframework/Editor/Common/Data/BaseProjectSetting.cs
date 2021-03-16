using GF.Common.Debug;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GFEditor.Common.Data
{
    [System.Serializable]
    public abstract class BaseProjectSetting
    {
        public const string SETTING_PATH_FORMAT = "ProjectSettings/GF_{0}.json";

        protected static T Load<T>(string settingName)
            where T : BaseProjectSetting, new()
        {
            T setting = null;
            string settingPath = string.Format(SETTING_PATH_FORMAT, settingName);
            try
            {
                setting = JsonUtility.FromJson<T>(File.ReadAllText(settingPath));
            }
            catch (Exception e)
            {
                MDebug.LogWarning("Setting", $"Load BuildSetting({settingPath}) Exception:\n{e.ToString()}");
            }

            if (setting == null)
            {
                setting = new T();
                setting.Initialize();
            }
            return setting;
        }

        public void Save()
        {
            string settingPath = string.Format(SETTING_PATH_FORMAT, GetSettingName());
            File.WriteAllText(settingPath, JsonUtility.ToJson(this, true));
            MDebug.Log("Setting"
                , $"Saved BuildSetting({settingPath})");
        }

        protected abstract string GetSettingName();

        protected virtual void Initialize()
        { }
    }
}