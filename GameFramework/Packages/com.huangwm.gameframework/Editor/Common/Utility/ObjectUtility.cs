using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Common.Utility
{
    public static class ObjectUtility
    {
        /// <summary>
        /// 是否包含<see cref="StaticEditorFlags"/>
        /// </summary>
        public static bool HasStaticEditorFlag(GameObject gameObject, StaticEditorFlags flag)
        {
            return (GameObjectUtility.GetStaticEditorFlags(gameObject) & flag) != 0;
        }

        /// <summary>
        /// 选中指定的Object
        /// </summary>
        public static void SelectionComponent<T>(T[] components) where T : Component
        {
            Object[] objects = new Object[components.Length];
            for (int iComponent = 0; iComponent < components.Length; iComponent++)
            {
                objects[iComponent] = components[iComponent].gameObject;
            }
            Selection.objects = objects;
        }

        /// <summary>
        /// 选中指定的Object
        /// </summary>
        public static void SelectionGameObjects(GameObject[] gameObjects)
        {
            Object[] objects = new Object[gameObjects.Length];
            for (int iGameObject = 0; iGameObject < gameObjects.Length; iGameObject++)
            {
                objects[iGameObject] = gameObjects[iGameObject];
            }
            Selection.objects = objects;
        }

        /// <summary>
        /// 获取当前选中的目录
        /// </summary>
        public static string GetSelectionFoldout(string @default = "Assets")
        {
            Object[] objects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
            for (int iObject = 0; iObject < objects.Length; iObject++)
            {
                string path = AssetDatabase.GetAssetPath(objects[iObject]);
                if (Directory.Exists(path))
                {
                    return path;
                }
                if (File.Exists(path))
                {
                    return Path.GetDirectoryName(path);
                }
            }

            return @default;
        }

        public static string[] AssetGUIDToAssetPath(string[] assetGUIDs)
        {
            HashSet<string> assetPaths = new HashSet<string>();
            for (int iAssetGUID = 0; iAssetGUID < assetGUIDs.Length; iAssetGUID++)
            {
                assetPaths.Add(AssetDatabase.GUIDToAssetPath(assetGUIDs[iAssetGUID]));
            }
            return assetPaths.ToArray();
        }
    }
}