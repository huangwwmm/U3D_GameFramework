using GFEditor.Common.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Renderer.LightmapAttach.Setting
{
    public class LightmapAttachSettingProvider : SettingsProvider
    {
        private bool m_ExporterFileNameTypeChanged;

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new LightmapAttachSettingProvider("GF/Lightmap Attach"
                , SettingsScope.Project
                , new HashSet<string>(new[] { "Lightmap Attach" }));
        }

        public LightmapAttachSettingProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
            m_ExporterFileNameTypeChanged = false;
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            LightmapAttachSetting setting = LightmapAttachSetting.GetInstance();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("保存"))
            {
                LightmapAttachSetting.GetInstance().Save();
                m_ExporterFileNameTypeChanged = false;
            }
            EditorGUILayout.EndHorizontal();

            setting.ExporterFolder = (EGLUtility.Folder("导出设置保存目录", setting.ExporterFolder));

            if (m_ExporterFileNameTypeChanged)
            {
                EditorGUILayout.HelpBox("修改这项设置，会导致之前的导出设置丢失场景的索引。"
                    , MessageType.Warning);
            }
            if (EGLUtility.EnumPopup(out setting.ExporterFileNameType
                     , "导出设置文件名"
                     , setting.ExporterFileNameType))
            {
                m_ExporterFileNameTypeChanged = true;
            }
        }
    }
}