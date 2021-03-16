using System;
using UnityEngine;
using UnityEditor;

namespace GFEditor.Common.Utility
{
    public static class EGLUtility
    {
        public static bool DelayedIntField(out int newValue, string label, int value, params GUILayoutOption[] options)
        {
            newValue = EditorGUILayout.DelayedIntField(label, value, options);
            return value != newValue;
        }

        public static bool Toggle(string label, bool value, params GUILayoutOption[] options)
        {
            Toggle(out bool newValue, label, value, options);
            return newValue;
        }

        public static bool Toggle(out bool newValue, string label, bool value, params GUILayoutOption[] options)
        {
            newValue = EditorGUILayout.Toggle(label, value, options);
            return value != newValue;
        }

        public static bool ToggleLeft(string label, bool value, params GUILayoutOption[] options)
        {
            ToggleLeft(out bool newValue, label, value, options);
            return newValue;
        }

        public static bool ToggleLeft(out bool newValue, string label, bool value, params GUILayoutOption[] options)
        {
            newValue = EditorGUILayout.ToggleLeft(label, value, options);
            return value != newValue;
        }

        public static bool DelayedFloatField(out float newValue, string label, float value, params GUILayoutOption[] options)
        {
            newValue = EditorGUILayout.DelayedFloatField(label, value, options);
            return value != newValue;
        }

        public static bool Vector3Field(out Vector3 newValue, string label, Vector3 value, params GUILayoutOption[] options)
        {
            newValue = EditorGUILayout.Vector3Field(label, value, options);
            return value != newValue;
        }

        public static bool EnumPopup<T>(out T newValue, string label, T value, params GUILayoutOption[] options)
            where T : Enum
        {
            newValue = (T)EditorGUILayout.EnumPopup(label, value, options);
            return !value.Equals(newValue);
        }

        public static string Folder(string label, string value, params GUILayoutOption[] options)
        {
            Folder(out string newValue, label, value, options);
            return newValue;
        }

        public static bool Folder(out string newValue, string label, string value, params GUILayoutOption[] options)
        {
            newValue = null;
            EditorGUILayout.BeginHorizontal();
            newValue = EditorGUILayout.TextField(label, value, options);
            UnityEngine.Object selectedObject = EditorGUILayout.ObjectField(null
                , typeof(UnityEngine.Object)
                , true
                , GUILayout.Width(32));
            if (selectedObject != null)
            {
                string path = AssetDatabase.GetAssetPath(selectedObject);
                if (!string.IsNullOrEmpty(path))
                {
                    newValue = System.IO.Directory.Exists(path)
                        ? path
                        : System.IO.Path.GetDirectoryName(path);
                }
            }
            if (GUILayout.Button("...", GUILayout.Width(20)))
            {
                string folder = EditorUtility.OpenFolderPanel(label
                    , value
                    , string.IsNullOrEmpty(value) ? Application.dataPath : value);
                if (!string.IsNullOrEmpty(folder))
                {
                    newValue = folder.Replace(Application.dataPath, "Assets");
                }
            }
            EditorGUILayout.EndHorizontal();

            return newValue != value;
        }

        public static string File(string label, string value, params GUILayoutOption[] options)
        {
            File(out string newValue, label, value, options);
            return newValue;
        }

        public static bool File(out string newValue, string label, string value, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal();
            newValue = EditorGUILayout.TextField(label, value, options);

            UnityEngine.Object selectedObject = EditorGUILayout.ObjectField(null
                , typeof(UnityEngine.Object)
                , true
                , GUILayout.Width(32));
            if (selectedObject != null)
            {
                string path = AssetDatabase.GetAssetPath(selectedObject);
                if (!string.IsNullOrEmpty(path))
                {
                    newValue = path;
                }
            }

            if (GUILayout.Button("...", GUILayout.Width(20)))
            {
                string file = EditorUtility.OpenFilePanel(label, value ?? Application.dataPath, string.Empty);
                if (!string.IsNullOrEmpty(file))
                {
                    newValue = file;
                }
            }
            EditorGUILayout.EndHorizontal();
            return value != newValue;
        }

        public static string AssetPath<T>(string label, string value, params GUILayoutOption[] options)
           where T : UnityEngine.Object
        {
            AssetPath<T>(out string newValue, label, value, options);
            return newValue;
        }

        public static bool AssetPath<T>(out string newValue, string label, string value, params GUILayoutOption[] options)
            where T : UnityEngine.Object
        {
            EditorGUILayout.BeginHorizontal();
            newValue = EditorGUILayout.TextField(label, value, options);
            T obj = null;
            obj = EditorGUILayout.ObjectField(obj, typeof(T), false, GUILayout.Width(36)) as T;
            if (obj != null)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path))
                {
                    newValue = path;
                }
            }
            if (GUILayout.Button("...", GUILayout.Width(20)))
            {
                string file = EditorUtility.OpenFilePanel(label, value ?? Application.dataPath, string.Empty);
                if (!string.IsNullOrEmpty(file))
                {
                    newValue = file.Replace(Application.dataPath, "Assets");
                }
            }
            EditorGUILayout.EndHorizontal();
            return value != newValue;
        }

        public static bool ObjectField<T>(out T newValue, string label, T value, bool allowSceneObjects, params GUILayoutOption[] options)
            where T : UnityEngine.Object
        {
            newValue = EditorGUILayout.ObjectField(label, value, typeof(T), allowSceneObjects) as T;
            if (newValue == null
                && value == null)
            {
                return false;
            }
            if (newValue == null
                || value == null)
            {
                return true;
            }
            return !newValue.Equals(value);
        }
    }
}