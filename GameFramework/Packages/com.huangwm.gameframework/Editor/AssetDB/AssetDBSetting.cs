using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GFEditor.AssetDB
{
    public class AssetDBSetting
    {
        private static AssetDBSetting ms_Instance;

        private bool m_HandleAssetPostprocessor;

        public static AssetDBSetting GetInstance()
        {
            if (ms_Instance == null)
            {
                ms_Instance = new AssetDBSetting();
            }
            return ms_Instance;
        }

        private AssetDBSetting()
        {
            m_HandleAssetPostprocessor = EditorPrefs.GetBool("AssetDB_m_HandleAssetPostprocessor", false);
        }

        public bool GetHandleAssetPostprocessor()
        {
            return m_HandleAssetPostprocessor;
        }

        public void SetHandleAssetPostprocessor(bool handleAssetPostprocessor)
        {
            if (handleAssetPostprocessor == m_HandleAssetPostprocessor)
            {
                return;
            }

            m_HandleAssetPostprocessor = handleAssetPostprocessor;
            EditorPrefs.SetBool("AssetDB_m_HandleAssetPostprocessor", m_HandleAssetPostprocessor);
        }
    }

    public class AssetDBSettingProvider : SettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new AssetDBSettingProvider("GF/Asset DB"
                , SettingsScope.User
                , new HashSet<string>(new[] { "asset", "asset db", "db" }));
        }

        public AssetDBSettingProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            AssetDBSetting setting = AssetDBSetting.GetInstance();

            EditorGUILayout.BeginHorizontal();
            setting.SetHandleAssetPostprocessor(EditorGUILayout.Toggle("自动计算资源引用关系", setting.GetHandleAssetPostprocessor()));
            if (setting.GetHandleAssetPostprocessor()
                && GUILayout.Button("重新计算所有资源的引用关系"))
            {
                DB.GetInstance().RecaculateDBWithDialog();
            }
            EditorGUILayout.EndHorizontal();
            if (setting.GetHandleAssetPostprocessor())
            {
                EditorGUILayout.HelpBox("开启\"自动计算资源引用关系\"功能后，最好执行一次\"重新计算所有资源的引用关系\"", MessageType.Info);
            }
        }
    }
}