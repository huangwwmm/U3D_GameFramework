using GF.Common.Debug;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace GF.Common.Utility
{
    public static class UnityEngineReflectionUtility
    {
        private static object[] ms_Parameters1 = new object[1];
        private static object[] ms_Parameters2 = new object[2];

        public static class GUILayoutUtility
        {
            private static FieldInfo CURRENT_FIELDINFO;

            public static object Current()
            {
                if (CURRENT_FIELDINFO == null)
                {
                    CURRENT_FIELDINFO = typeof(UnityEngine.GUILayoutUtility).GetField("current", BindingFlags.Static | BindingFlags.NonPublic);
                }

                return CURRENT_FIELDINFO.GetValue(null);
            }

            public static object TopLevel()
            {
                return LayoutCache.TopLevel(Current());
            }

            public static class LayoutCache
            {
                private static Type LAYOUT_CAHCE_TYPE;
                private static FieldInfo TOPLEVEL_FIELDINFO;

                static LayoutCache()
                {
                    LAYOUT_CAHCE_TYPE = typeof(UnityEngine.GUILayoutUtility).Assembly.GetType("UnityEngine.GUILayoutUtility+LayoutCache");
                }

                public static object TopLevel(object layoutCache)
                {
                    if (TOPLEVEL_FIELDINFO == null)
                    {
                        TOPLEVEL_FIELDINFO = LAYOUT_CAHCE_TYPE.GetField("topLevel", BindingFlags.Instance | BindingFlags.NonPublic);
                    }

                    return TOPLEVEL_FIELDINFO.GetValue(layoutCache);
                }
            }
        }

        public static class GUILayoutGroup
        {
            private static Type GUI_LAYOUT_GROUP_TYPE;
            private static MethodInfo GET_LAST_METHODINFO;

            static GUILayoutGroup()
            {
                GUI_LAYOUT_GROUP_TYPE = typeof(UnityEngine.GUILayoutUtility).Assembly.GetType("UnityEngine.GUILayoutGroup");
            }

            public static Rect GetLast(object guiLayoutGroup)
            {
                if (GET_LAST_METHODINFO == null)
                {
                    GET_LAST_METHODINFO = GUI_LAYOUT_GROUP_TYPE.GetMethod("GetLast", BindingFlags.Instance | BindingFlags.Public);
                }

                return (Rect)GET_LAST_METHODINFO.Invoke(guiLayoutGroup, null);
            }
        }

        public static class ShaderKeyword
        {
            /// <summary>
            /// Keep in sync with kMaxShaderKeywords in ShaderKeywordSet.h
            /// vaild index [0, MAX_SHADER_KEYWORDS)
            /// </summary>
            public const int MAX_SHADER_KEYWORDS =
#if UNITY_2019_1_OR_NEWER
                320
#else
                256
#endif
                ;

            private static Type SHADERKEYWORD_TYPE;
            private static MethodInfo GET_SHADER_KEYWORD_NAME_METHODINFO;
            private static MethodInfo GET_SHADER_KEYWORD_TYPE_METHODINFO;
            private static FieldInfo KEYWORD_INDEX_FIELDINFO;
            private static ConstructorInfo CONSTRUCTORINFO;

            static ShaderKeyword()
            {
                SHADERKEYWORD_TYPE = typeof(UnityEngine.Rendering.ShaderKeyword);
                GET_SHADER_KEYWORD_NAME_METHODINFO = SHADERKEYWORD_TYPE.GetMethod("GetShaderKeywordName", BindingFlags.Static | BindingFlags.NonPublic);
                GET_SHADER_KEYWORD_TYPE_METHODINFO = SHADERKEYWORD_TYPE.GetMethod("GetShaderKeywordType", BindingFlags.Static | BindingFlags.NonPublic);
                KEYWORD_INDEX_FIELDINFO = SHADERKEYWORD_TYPE.GetField("m_KeywordIndex", BindingFlags.Instance | BindingFlags.NonPublic);
                CONSTRUCTORINFO = SHADERKEYWORD_TYPE.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance
                    , null
                    , new Type[] { typeof(int) }
                    , null);
            }

            public static string GetShaderKeywordName(int keywordIndex)
            {
                object result = GET_SHADER_KEYWORD_NAME_METHODINFO.Invoke(null, new object[] { keywordIndex });
                return result == null
                    ? null
                    : (string)result;
            }

            public static ShaderKeywordType GetShaderKeywordType(int keywordIndex)
            {
                object result = GET_SHADER_KEYWORD_TYPE_METHODINFO.Invoke(null, new object[] { keywordIndex });
                return result == null
                    ? ShaderKeywordType.None
                    : (ShaderKeywordType)result;
            }

            public static int GetShaderKeywordIndex(UnityEngine.Rendering.ShaderKeyword shaderKeyword)
            {
                return (int)KEYWORD_INDEX_FIELDINFO.GetValue(shaderKeyword);
            }

            public static void SetShaderKeywordIndex(UnityEngine.Rendering.ShaderKeyword shaderKeyword, int keywordIndex)
            {
                KEYWORD_INDEX_FIELDINFO.SetValue(shaderKeyword, keywordIndex);
            }

            public static UnityEngine.Rendering.ShaderKeyword NewShaderKeyword(int index)
            {
                ms_Parameters1[0] = index;
                return (UnityEngine.Rendering.ShaderKeyword)CONSTRUCTORINFO.Invoke(ms_Parameters1);
            }
        }

        public static class Material
        {
            private static PropertyInfo RAW_RENDER_QUEUE_PROPERTYINFO;

            static Material()
            {
                RAW_RENDER_QUEUE_PROPERTYINFO = typeof(UnityEngine.Material).GetProperty("rawRenderQueue", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            public static int RawRenderQueue(UnityEngine.Material material)
            {
                return (int)RAW_RENDER_QUEUE_PROPERTYINFO.GetValue(material);
            }
        }
    }
}