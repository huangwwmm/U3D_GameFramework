using GF.Common.Data;
using GF.Common.Debug;
using GF.Common.Utility;
using GFEditor.Common.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Common.FolderLink
{
    public class FolderLinkSettingProvider : SettingsProvider
    {
        private PrefsValue<bool> m_SettingFoldout;
        private PrefsValue<bool> m_LinkFoldout;

        private ItemsForGUI m_ItemsForGUI;
        private SerializedObject m_SO_Items;
        private SerializedProperty m_SP_Items;

        private List<Item> m_Items;

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new FolderLinkSettingProvider("GF/Folder Link"
                , SettingsScope.Project
                , new HashSet<string>(new[] { "GF" }));
        }

        public FolderLinkSettingProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
            m_SettingFoldout = new PrefsValue<bool>("FolderLinkSettingProvider m_SettingFoldout");
            m_LinkFoldout = new PrefsValue<bool>("FolderLinkSettingProvider m_LinkFoldout");

            m_ItemsForGUI = ScriptableObject.CreateInstance<ItemsForGUI>();
            m_ItemsForGUI.Items = FolderLinkSetting.GetInstance().Items.ToList();

            m_SO_Items = new SerializedObject(m_ItemsForGUI);
            m_SP_Items = m_SO_Items.FindProperty("Items");

            m_Items = new List<Item>();
            RegenerateItems();
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            FolderLinkSetting setting = FolderLinkSetting.GetInstance();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("保存"))
            {
                setting.Items = m_ItemsForGUI.Items.ToList();
                setting.Save();
                RegenerateItems();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            m_SettingFoldout.Set(EditorGUILayout.Foldout(m_SettingFoldout, "设置"));
            EditorGUILayout.EndHorizontal();
            if (m_SettingFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_SP_Items
                    , EditorGUIUtility.TrTextContent("Links")
                    , true);
                if (EditorGUI.EndChangeCheck())
                {
                    m_SO_Items.ApplyModifiedProperties();
                    EditorUtility.SetDirty(m_ItemsForGUI);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            m_LinkFoldout.Set(EditorGUILayout.Foldout(m_LinkFoldout, "Link"));
            EditorGUILayout.EndHorizontal();
            if (m_LinkFoldout)
            {
                for (int iItem = 0; iItem < m_Items.Count; iItem++)
                {
                    Item iterItem = m_Items[iItem];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(iterItem.Name);
                    if (GUILayout.Button("创建Link", GUILayout.Width(72)))
                    {
                        CreateLink(iterItem);
                    }
                    if (GUILayout.Button("移除Link", GUILayout.Width(72)))
                    {
                        DeleteLink(iterItem);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private void RegenerateItems()
        {
            m_Items.Clear();

            m_Items.AddRange(FolderLinkSetting.GetInstance().Items);
        }

        private void CreateLink(Item item)
        {
            DeleteLink(item);

            for (int iLink = 0; iLink < item.Links.Count; iLink++)
            {
                LinkItem iterLink = item.Links[iLink];
                ExecuteProcessUtility.Mklink(iterLink.Link, iterLink.Target, false, iterLink.IsDirectory, true);
            }
        }

        private void DeleteLink(Item item)
        {
            for (int iLink = 0; iLink < item.Links.Count; iLink++)
            {
                LinkItem iterLink = item.Links[iLink];
                if (iterLink.IsDirectory)
                {
                    if (Directory.Exists(iterLink.Link))
                    {
                        ExecuteProcessUtility.Rmdir(iterLink.Link);
                    }
                }
                else
                {
                    if (File.Exists(iterLink.Link))
                    {
                        File.Delete(iterLink.Link);
                    }
                }
            }
        }

        private class ItemsForGUI : ScriptableObject
        {
            public List<Item> Items;
        }
    }
}