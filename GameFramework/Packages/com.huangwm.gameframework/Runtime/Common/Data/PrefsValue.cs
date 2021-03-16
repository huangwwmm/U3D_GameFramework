using GF.Common.Debug;
using System;
using UnityEditor;
using UnityEngine;

namespace GF.Common.Data
{
    public struct PrefsValue<T>
    {
        private bool m_IsInitialized;
        private string m_Key;
        private object m_Value;
        private object m_DefaultValue;
        /// <summary>
        /// 自动保存
        /// </summary>
        private bool m_AutoSave;
        /// <summary>
        /// 优先使用的Prefs类型
        ///	打包后只能使用<see cref="PrefsType.Player"/>
        /// </summary>
        private PrefsType m_PrefsType;
        private ValueType m_ValueType;

        public static implicit operator T(PrefsValue<T> value)
        {
            return value.Get();
        }

        public PrefsValue(string key)
            : this(key, default, true, PrefsType.Editor)
        {
        }

        public PrefsValue(string key, T defaultValue)
            : this(key, defaultValue, true, PrefsType.Editor)
        {
        }

        public PrefsValue(string key, T defaultValue, bool autoSave, PrefsType prefsType)
        {
            m_IsInitialized = true;

            m_Key = key;
            m_Value = defaultValue;
            m_DefaultValue = defaultValue;
            m_AutoSave = autoSave;
            m_PrefsType = prefsType;

            Type type = typeof(T);
            if (type == typeof(string))
            {
                m_ValueType = ValueType.String;
            }
            else if (type == typeof(float))
            {
                m_ValueType = ValueType.Float;
            }
            else if (type == typeof(bool))
            {
                m_ValueType = ValueType.Bool;
            }
            else if (type == typeof(int))
            {
                m_ValueType = ValueType.Int;
            }
            else if (type == typeof(Color))
            {
                m_ValueType = ValueType.Color;
            }
            else
            {
                m_ValueType = ValueType.Unknown;
                MDebug.LogError("Prefs", $"ValueItem not support value type({type})");
            }

            Load();
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        public T Get()
        {
            return (T)m_Value;
        }

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <returns>IsChanged</returns>
        public bool Set(T value)
        {
            bool changed = !value.Equals(m_Value);
            m_Value = value;
            if (m_AutoSave
                && changed)
            {
                Save();
            }
            return changed;
        }

        /// <summary>
        /// 保存到Prefs
        /// </summary>
        public void Save()
        {
#if UNITY_EDITOR
            switch (m_PrefsType)
            {
                case PrefsType.Editor:
                    switch (m_ValueType)
                    {
                        case ValueType.String:
                            EditorPrefs.SetString(m_Key, (string)m_Value);
                            break;
                        case ValueType.Float:
                            EditorPrefs.SetFloat(m_Key, (float)m_Value);
                            break;
                        case ValueType.Bool:
                            EditorPrefs.SetBool(m_Key, (bool)m_Value);
                            break;
                        case ValueType.Int:
                            EditorPrefs.SetInt(m_Key, (int)m_Value);
                            break;
                        case ValueType.Color:
                            EditorPrefs.SetInt(m_Key, Utility.ColorUtility.ConvertColorToInt32((Color)m_Value));
                            break;
                        default:
                            UnityEngine.Debug.LogError(string.Format("Unresolved Save ValueType({0})", m_ValueType));
                            break;
                    }
                    break;
                case PrefsType.Player:
#endif
                    switch (m_ValueType)
                    {
                        case ValueType.String:
                            PlayerPrefs.SetString(m_Key, (string)m_Value);
                            break;
                        case ValueType.Float:
                            PlayerPrefs.SetFloat(m_Key, (float)m_Value);
                            break;
                        case ValueType.Bool:
                            PlayerPrefs.SetInt(m_Key, ((bool)m_Value) ? 1 : 0);
                            break;
                        case ValueType.Int:
                            PlayerPrefs.SetInt(m_Key, (int)m_Value);
                            break;
                        case ValueType.Color:
                            PlayerPrefs.SetInt(m_Key, Utility.ColorUtility.ConvertColorToInt32((Color)m_Value));
                            break;
                        default:
                            UnityEngine.Debug.LogError(string.Format("Unresolved Save ValueType({0})", m_ValueType));
                            break;
                    }
#if UNITY_EDITOR
                    break;
            }
#endif
        }

        /// <summary>
        /// 从Prefs读取
        /// </summary>
        public void Load()
        {
#if UNITY_EDITOR
            switch (m_PrefsType)
            {
                case PrefsType.Editor:
                    switch (m_ValueType)
                    {
                        case ValueType.String:
                            m_Value = EditorPrefs.GetString(m_Key, (string)m_DefaultValue);
                            break;
                        case ValueType.Float:
                            m_Value = EditorPrefs.GetFloat(m_Key, (float)m_DefaultValue);
                            break;
                        case ValueType.Bool:
                            m_Value = EditorPrefs.GetBool(m_Key, (bool)m_DefaultValue);
                            break;
                        case ValueType.Int:
                            m_Value = EditorPrefs.GetInt(m_Key, (int)m_DefaultValue);
                            break;
                        case ValueType.Color:
                            m_Value = Utility.ColorUtility.ConvertInt32ToColor(EditorPrefs.GetInt(m_Key, Utility.ColorUtility.ConvertColorToInt32((Color)m_DefaultValue)));
                            break;
                        case ValueType.Unknown:
                        default:
                            UnityEngine.Debug.LogError(string.Format("Unresolved Load ValueType({0})", m_ValueType));
                            break;
                    }
                    break;
                case PrefsType.Player:
#endif
                    switch (m_ValueType)
                    {
                        case ValueType.String:
                            m_Value = PlayerPrefs.GetString(m_Key, (string)m_DefaultValue);
                            break;
                        case ValueType.Float:
                            m_Value = PlayerPrefs.GetFloat(m_Key, (float)m_DefaultValue);
                            break;
                        case ValueType.Bool:
                            m_Value = PlayerPrefs.GetInt(m_Key, ((bool)m_DefaultValue) ? 1 : 0) == 1 ? true : false;
                            break;
                        case ValueType.Int:
                            m_Value = PlayerPrefs.GetInt(m_Key, (int)m_DefaultValue);
                            break;
                        case ValueType.Color:
                            m_Value = Utility.ColorUtility.ConvertInt32ToColor(PlayerPrefs.GetInt(m_Key, Utility.ColorUtility.ConvertColorToInt32((Color)m_DefaultValue)));
                            break;
                        case ValueType.Unknown:
                        default:
                            UnityEngine.Debug.LogError(string.Format("Unresolved Load ValueType({0})", m_ValueType));
                            break;
                    }
#if UNITY_EDITOR
                    break;
            }
#endif
        }

        public bool IsInitialized()
        {
            return m_IsInitialized;
        }

        public override bool Equals(object obj)
        {
            return obj is PrefsValue<T>
                ? m_Value.Equals(((PrefsValue<T>)obj).Get())
                : m_Value.Equals(obj);
        }

        public override int GetHashCode()
        {
            return m_Key.GetHashCode() + m_Value.GetHashCode();
        }

        public override string ToString()
        {
            return m_Value.ToString();
        }

        public enum PrefsType
        {
            /// <summary>
            /// <see cref="PlayerPrefs"/>
            /// </summary>
            Player,
            /// <summary>
            /// <see cref="EditorPrefs"/>
            /// </summary>
            Editor,
        }

        /// <summary>
        /// <see cref="m_Value"/>的类型
        /// </summary>
        private enum ValueType : byte
        {
            /// <summary>
            /// 未支持的类型，可以使用<see cref="Get"/><see cref="Set"/>，但是无法<see cref="Save"/><see cref="Load"/>
            /// </summary>
            Unknown = 0,
            String,
            Float,
            Bool,
            Int,
            Color,
        }
    }
}