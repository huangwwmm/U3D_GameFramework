using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Common.Utility
{
    public static class UnityEditorReflectionUtility
    {
        /// <summary>
        /// <see cref="EditorWindow.m_Parent"/>
        /// </summary>
        private static FieldInfo EDITOR_WINDOW_PARENT_FIELDINFO;
        /// <summary>
        /// 用于计算FPS
        /// </summary>
        private static FieldInfo MAX_FRAME_TIME_FIELDINFO;
        /// <summary>
        /// 当前帧CPU时间
        /// </summary>
        private static FieldInfo CLIENT_FRAME_TIME_FIELDINFO;
        /// <summary>
        /// 当前帧渲染时间
        /// </summary>
        private static FieldInfo RENDER_FRAME_TIME_FIELDINFO;
        /// <summary>
        /// 当前渲染的三角形数量
        /// </summary>
        private static MethodInfo TRIANGLE_COUNT_METHODINFO;
        /// <summary>
        /// 当前渲染的顶点数量
        /// </summary>
        private static MethodInfo VERTICE_COUNT_METHODINFO;
        /// <summary>
        /// Batched数量
        /// </summary>
        private static MethodInfo BATCHED_COUNT_METHODINFO;
        /// <summary>
        /// 动态Batched数量
        /// </summary>
        private static MethodInfo DYNAMIC_BATCHED_COUNT_METHODINFO;
        /// <summary>
        /// 静态Batched数量
        /// </summary>
        private static MethodInfo STATIC_BATCHED_COUNT_METHODINFO;
        /// <summary>
        /// Instanced Batched数量
        /// </summary>
        private static MethodInfo INSTANCED_BATCHED_COUNT_METHODINFO;
        /// <summary>
        /// 动态DrawCall数量
        /// </summary>
        private static MethodInfo DYNAMIC_BATCHED_DRAWCALL_COUNT_METHODINFO;
        /// <summary>
        /// 静态DrawCall数量
        /// </summary>
        private static MethodInfo STATIC_BATCHED_DRAWCALL_COUNT_METHODINFO;
        /// <summary>
        /// Instanced DrawCall数量
        /// </summary>
        private static MethodInfo INSTANCED_BATCHED_DRAWCALL_COUNT_METHODINFO;

        private static object[] ms_Parameters1 = new object[1];
        private static object[] ms_Parameters2 = new object[2];

        public static EditorWindow GetGameWindow()
        {
            return EditorWindow.GetWindow(GetGameWindowType());
        }

        public static Type GetGameWindowType()
        {
            return typeof(UnityEditor.EditorGUILayout).Assembly.GetType("UnityEditor.GameView");
        }

        public static Type GetSceneWindowType()
        {
            return typeof(UnityEditor.EditorGUILayout).Assembly.GetType("UnityEditor.SceneView");
        }

        public static object GetEditorWindowParent(EditorWindow editorWindow)
        {
            if (EDITOR_WINDOW_PARENT_FIELDINFO == null)
            {
                EDITOR_WINDOW_PARENT_FIELDINFO = typeof(UnityEditor.EditorWindow).GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            return EDITOR_WINDOW_PARENT_FIELDINFO.GetValue(editorWindow);
        }

        public static float GetMaxFrameTime()
        {
            if (MAX_FRAME_TIME_FIELDINFO == null)
            {
                MAX_FRAME_TIME_FIELDINFO = typeof(UnityEditor.EditorGUILayout).Assembly
                    .GetType("UnityEditor.GameViewGUI")
                    .GetField("m_MaxFrameTime", BindingFlags.Static | BindingFlags.NonPublic);
            }

            return (float)MAX_FRAME_TIME_FIELDINFO.GetValue(null);
        }

        public static float GetClientFrameTime()
        {
            if (CLIENT_FRAME_TIME_FIELDINFO == null)
            {
                CLIENT_FRAME_TIME_FIELDINFO = typeof(UnityEditor.EditorGUILayout).Assembly
                    .GetType("UnityEditor.GameViewGUI")
                    .GetField("m_ClientFrameTime", BindingFlags.Static | BindingFlags.NonPublic);
            }

            return (float)CLIENT_FRAME_TIME_FIELDINFO.GetValue(null);
        }

        public static float GetRenderFrameTime()
        {
            if (RENDER_FRAME_TIME_FIELDINFO == null)
            {
                RENDER_FRAME_TIME_FIELDINFO = typeof(UnityEditor.EditorGUILayout).Assembly
                    .GetType("UnityEditor.GameViewGUI")
                    .GetField("m_RenderFrameTime", BindingFlags.Static | BindingFlags.NonPublic);
            }

            return (float)RENDER_FRAME_TIME_FIELDINFO.GetValue(null);
        }

        public static int GetTriangleCount()
        {
            if (TRIANGLE_COUNT_METHODINFO == null)
            {
                TRIANGLE_COUNT_METHODINFO = typeof(UnityEditor.EditorGUILayout).Assembly
                    .GetType("UnityEditor.UnityStats")
                    .GetMethod("get_triangles", BindingFlags.Static | BindingFlags.Public);
            }

            return (int)TRIANGLE_COUNT_METHODINFO.Invoke(null, null);
        }

        public static int GetVerticeCount()
        {
            if (VERTICE_COUNT_METHODINFO == null)
            {
                VERTICE_COUNT_METHODINFO = typeof(UnityEditor.EditorGUILayout).Assembly
                    .GetType("UnityEditor.UnityStats")
                    .GetMethod("get_vertices", BindingFlags.Static | BindingFlags.Public);
            }

            return (int)VERTICE_COUNT_METHODINFO.Invoke(null, null);
        }

        public static int GetBatcheCount()
        {
            if (BATCHED_COUNT_METHODINFO == null)
            {
                BATCHED_COUNT_METHODINFO = typeof(UnityEditor.EditorGUILayout).Assembly
                    .GetType("UnityEditor.UnityStats")
                    .GetMethod("get_batches", BindingFlags.Static | BindingFlags.Public);
            }

            return (int)BATCHED_COUNT_METHODINFO.Invoke(null, null);
        }

        public static int GetDynamicBatcheCount()
        {
            if (DYNAMIC_BATCHED_COUNT_METHODINFO == null)
            {
                DYNAMIC_BATCHED_COUNT_METHODINFO = typeof(UnityEditor.EditorGUILayout).Assembly
                    .GetType("UnityEditor.UnityStats")
                    .GetMethod("get_dynamicBatches", BindingFlags.Static | BindingFlags.Public);
            }

            return (int)DYNAMIC_BATCHED_COUNT_METHODINFO.Invoke(null, null);
        }

        public static int GetStaticBatcheCount()
        {
            if (STATIC_BATCHED_COUNT_METHODINFO == null)
            {
                STATIC_BATCHED_COUNT_METHODINFO = typeof(UnityEditor.EditorGUILayout).Assembly
                    .GetType("UnityEditor.UnityStats")
                    .GetMethod("get_staticBatches", BindingFlags.Static | BindingFlags.Public);
            }

            return (int)STATIC_BATCHED_COUNT_METHODINFO.Invoke(null, null);
        }

        public static int GetInstancedBatcheCount()
        {
            if (INSTANCED_BATCHED_COUNT_METHODINFO == null)
            {
                INSTANCED_BATCHED_COUNT_METHODINFO = typeof(UnityEditor.EditorGUILayout).Assembly
                    .GetType("UnityEditor.UnityStats")
                    .GetMethod("get_instancedBatches", BindingFlags.Static | BindingFlags.Public);
            }

            return (int)INSTANCED_BATCHED_COUNT_METHODINFO.Invoke(null, null);
        }

        public static int GetDynamicBatchedDrawCallCount()
        {
            if (DYNAMIC_BATCHED_DRAWCALL_COUNT_METHODINFO == null)
            {
                DYNAMIC_BATCHED_DRAWCALL_COUNT_METHODINFO = typeof(UnityEditor.EditorGUILayout).Assembly
                    .GetType("UnityEditor.UnityStats")
                    .GetMethod("get_dynamicBatchedDrawCalls", BindingFlags.Static | BindingFlags.Public);
            }

            return (int)DYNAMIC_BATCHED_DRAWCALL_COUNT_METHODINFO.Invoke(null, null);
        }

        public static int GetStaticBatchedDrawCallCount()
        {
            if (STATIC_BATCHED_DRAWCALL_COUNT_METHODINFO == null)
            {
                STATIC_BATCHED_DRAWCALL_COUNT_METHODINFO = typeof(UnityEditor.EditorGUILayout).Assembly
                    .GetType("UnityEditor.UnityStats")
                    .GetMethod("get_staticBatchedDrawCalls", BindingFlags.Static | BindingFlags.Public);
            }

            return (int)STATIC_BATCHED_DRAWCALL_COUNT_METHODINFO.Invoke(null, null);
        }

        public static int GetInstancedBatchedDrawCallCount()
        {
            if (INSTANCED_BATCHED_DRAWCALL_COUNT_METHODINFO == null)
            {
                INSTANCED_BATCHED_DRAWCALL_COUNT_METHODINFO = typeof(UnityEditor.EditorGUILayout).Assembly
                    .GetType("UnityEditor.UnityStats")
                    .GetMethod("get_instancedBatchedDrawCalls", BindingFlags.Static | BindingFlags.Public);
            }

            return (int)INSTANCED_BATCHED_DRAWCALL_COUNT_METHODINFO.Invoke(null, null);
        }

        public static class SplitterGUILayout
        {
            private static Type SPLITTER_GUI_LAYOUT_TYPE;
            private static Type SPLITTER_STATE_TYPE;

            private static MethodInfo BEGIN_VERTICAL_SPLIT_METHODINFO;
            private static MethodInfo END_VERTICAL_SPLIT_METHODINFO;

            static SplitterGUILayout()
            {
                SPLITTER_GUI_LAYOUT_TYPE = typeof(UnityEditor.EditorGUILayout).Assembly.GetType("UnityEditor.SplitterGUILayout");
                SPLITTER_STATE_TYPE = typeof(UnityEditor.EditorGUILayout).Assembly.GetType("UnityEditor.SplitterState");
            }

            public static object CreateSplitterState(float[] relativeSizes, int[] minSizes = null, int[] maxSizes = null, int splitSize = 0)
            {
                return Activator.CreateInstance(SPLITTER_STATE_TYPE, relativeSizes, minSizes, maxSizes, splitSize);
            }

            public static void BeginVerticalSplit(object splitterState, params GUILayoutOption[] options)
            {
                if (BEGIN_VERTICAL_SPLIT_METHODINFO == null)
                {
                    BEGIN_VERTICAL_SPLIT_METHODINFO = SPLITTER_GUI_LAYOUT_TYPE.GetMethod("BeginVerticalSplit", new Type[] { SPLITTER_STATE_TYPE, options.GetType() });
                }

                BEGIN_VERTICAL_SPLIT_METHODINFO.Invoke(null, new object[] { splitterState, options });
            }

            public static void EndVerticalSplit()
            {
                if (END_VERTICAL_SPLIT_METHODINFO == null)
                {
                    END_VERTICAL_SPLIT_METHODINFO = SPLITTER_GUI_LAYOUT_TYPE.GetMethod("EndVerticalSplit", BindingFlags.Static | BindingFlags.Public);
                }

                END_VERTICAL_SPLIT_METHODINFO.Invoke(null, null);
            }
        }

        public static class InternalEditorUtility
        {
            private static Type INTERNAL_EDITOR_UTILITY_TYPE;

            private static MethodInfo HAS_PRO_METHODINFO;

            static InternalEditorUtility()
            {
                INTERNAL_EDITOR_UTILITY_TYPE = typeof(UnityEditor.EditorGUILayout).Assembly.GetType("UnityEditorInternal.InternalEditorUtility");
            }

            public static bool HasPro()
            {
                if (HAS_PRO_METHODINFO == null)
                {
                    HAS_PRO_METHODINFO = INTERNAL_EDITOR_UTILITY_TYPE.GetMethod("HasPro", BindingFlags.Static | BindingFlags.Public);
                }

                return (bool)HAS_PRO_METHODINFO.Invoke(null, null);
            }
        }

        public static class ShaderGUIUtility
        {
            private static MethodInfo CREATE_SHADER_GUI_METHODINFO;

            static ShaderGUIUtility()
            {
                Type shaderGUIUtilityType = typeof(UnityEditor.EditorGUILayout).Assembly.GetType("UnityEditor.ShaderGUIUtility");
                CREATE_SHADER_GUI_METHODINFO = shaderGUIUtilityType.GetMethod("CreateShaderGUI"
                    , BindingFlags.Static | BindingFlags.NonPublic);
            }

            public static ShaderGUI CreateShaderGUI(string customEditorName)
            {
                return CREATE_SHADER_GUI_METHODINFO.Invoke(null, new object[] { customEditorName }) as ShaderGUI;
            }
        }

        public static class ShaderUtil
        {
            private static MethodInfo GET_VARIANT_COUNT_METHODINFO;
            private static MethodInfo SAVE_CURRENT_SHADER_VARIANT_COLLECTION_METHODINFO;
            private static MethodInfo CLEAR_CURRENT_SHADER_VARIANT_COLLECTION_METHODINFO;
            private static MethodInfo GET_CURRENT_SHADER_VARIANT_COLLECTION_SHADER_COUNT_METHODINFO;
            private static MethodInfo GET_CURRENT_SHADER_VARIANT_COLLECTION_VARIANT_COUNT_METHODINFO;

            private static MethodInfo GET_SHADER_PROPERTY_ATTRIBUTES_METHODINFO;

            private static MethodInfo GET_ALL_GLOBAL_KEYWORDS_METHODINFO;
            private static MethodInfo GET_SHADER_GLOBAL_KEYWORDS_METHODINFO;
            private static MethodInfo GET_SHADER_LOCAL_KEYWORDS_METHODINFO;

            static ShaderUtil()
            {
                Type shaderUtilType = typeof(UnityEditor.ShaderUtil);

                GET_VARIANT_COUNT_METHODINFO = shaderUtilType.GetMethod("GetVariantCount"
                    , BindingFlags.Static | BindingFlags.NonPublic);
                SAVE_CURRENT_SHADER_VARIANT_COLLECTION_METHODINFO = shaderUtilType.GetMethod("SaveCurrentShaderVariantCollection"
                    , BindingFlags.Static | BindingFlags.NonPublic);
                CLEAR_CURRENT_SHADER_VARIANT_COLLECTION_METHODINFO = shaderUtilType.GetMethod("ClearCurrentShaderVariantCollection"
                    , BindingFlags.Static | BindingFlags.NonPublic);
                GET_CURRENT_SHADER_VARIANT_COLLECTION_SHADER_COUNT_METHODINFO = shaderUtilType.GetMethod("GetCurrentShaderVariantCollectionShaderCount"
                    , BindingFlags.Static | BindingFlags.NonPublic);
                GET_CURRENT_SHADER_VARIANT_COLLECTION_VARIANT_COUNT_METHODINFO = shaderUtilType.GetMethod("GetCurrentShaderVariantCollectionVariantCount"
                    , BindingFlags.Static | BindingFlags.NonPublic);

                GET_SHADER_PROPERTY_ATTRIBUTES_METHODINFO = shaderUtilType.GetMethod("GetShaderPropertyAttributes"
                    , BindingFlags.Static | BindingFlags.NonPublic);

                GET_ALL_GLOBAL_KEYWORDS_METHODINFO = shaderUtilType.GetMethod("GetAllGlobalKeywords"
                    , BindingFlags.Static | BindingFlags.NonPublic);
                GET_SHADER_GLOBAL_KEYWORDS_METHODINFO = shaderUtilType.GetMethod("GetShaderGlobalKeywords"
                    , BindingFlags.Static | BindingFlags.NonPublic);
                GET_SHADER_LOCAL_KEYWORDS_METHODINFO = shaderUtilType.GetMethod("GetShaderLocalKeywords"
                    , BindingFlags.Static | BindingFlags.NonPublic);
            }

            public static int GetCurrentShaderVariantCollectionShaderCount()
            {
                return (int)GET_CURRENT_SHADER_VARIANT_COLLECTION_SHADER_COUNT_METHODINFO.Invoke(null, null);
            }

            public static int GetCurrentShaderVariantCollectionVariantCount()
            {
                return (int)GET_CURRENT_SHADER_VARIANT_COLLECTION_VARIANT_COUNT_METHODINFO.Invoke(null, null);
            }

            public static ulong GetVariantCount(Shader s, bool usedBySceneOnly)
            {
                ms_Parameters2[0] = s;
                ms_Parameters2[1] = usedBySceneOnly;
                ulong result = (ulong)GET_VARIANT_COUNT_METHODINFO.Invoke(null, ms_Parameters2);
                ms_Parameters2[0] = null;
                return result;
            }

            public static void SaveCurrentShaderVariantCollection(string assetPath)
            {
                SAVE_CURRENT_SHADER_VARIANT_COLLECTION_METHODINFO.Invoke(null, new object[] { assetPath });
            }

            public static void ClearCurrentShaderVariantCollection()
            {
                CLEAR_CURRENT_SHADER_VARIANT_COLLECTION_METHODINFO.Invoke(null, null);
            }

            public static string[] GetShaderPropertyAttributes(Shader s, string name)
            {
                ms_Parameters2[0] = s;
                ms_Parameters2[1] = name;
                string[] result = (string[])GET_SHADER_PROPERTY_ATTRIBUTES_METHODINFO.Invoke(null, ms_Parameters2);
                ms_Parameters2[0] = null;
                return result;
            }

            public static string[] GetAllGlobalKeywords()
            {
                return (string[])GET_ALL_GLOBAL_KEYWORDS_METHODINFO.Invoke(null, null);
            }

            public static string[] GetShaderGlobalKeywords(Shader shader)
            {
                ms_Parameters1[0] = shader;
                string[] result = (string[])GET_SHADER_GLOBAL_KEYWORDS_METHODINFO.Invoke(null, ms_Parameters1);
                ms_Parameters1[0] = null;
                return result;
            }

            public static string[] GetShaderLocalKeywords(Shader shader)
            {
                ms_Parameters1[0] = shader;
                string[] result = (string[])GET_SHADER_LOCAL_KEYWORDS_METHODINFO.Invoke(null, ms_Parameters1);
                ms_Parameters1[0] = null;
                return result;
            }
        }

        public static class ShaderInspectorPlatformsPopup
        {
            private static Type SHADER_INSPECTOR_PLATFORMS_POPUP_TYPE;

            static ShaderInspectorPlatformsPopup()
            {
                SHADER_INSPECTOR_PLATFORMS_POPUP_TYPE = typeof(UnityEditor.EditorGUILayout).Assembly.GetType("UnityEditor.ShaderInspectorPlatformsPopup");
            }

            public static object Constructor(Shader shader)
            {
                return Activator.CreateInstance(SHADER_INSPECTOR_PLATFORMS_POPUP_TYPE, new object[] { shader }, null);
            }
        }

        public static class GUIView
        {
            private static MethodInfo CAPTURE_SCENE_METHOD_INFO;

            static GUIView()
            {
                Type guiViewType = typeof(UnityEditor.EditorGUILayout).Assembly.GetType("UnityEditor.GUIView");
                CAPTURE_SCENE_METHOD_INFO = guiViewType.GetMethod("CaptureRenderDocScene", BindingFlags.Instance | BindingFlags.Public);
            }

            public static void CaptureScene(EditorWindow editorWindow)
            {
                CAPTURE_SCENE_METHOD_INFO.Invoke(GetEditorWindowParent(editorWindow), null);
            }
        }

        public static class RenderDoc
        {
            private static MethodInfo IS_INSTALLED_METHOD_INFO;
            private static MethodInfo IS_LOADED_METHOD_INFO;
            private static MethodInfo IS_SUPPORTED_METHOD_INFO;
            private static MethodInfo LOAD_METHOD_INFO;

            static RenderDoc()
            {
                Type renderDocType = typeof(UnityEditor.EditorGUILayout).Assembly.GetType("UnityEditorInternal.RenderDoc");
                IS_INSTALLED_METHOD_INFO = renderDocType.GetMethod("IsInstalled", BindingFlags.Static | BindingFlags.Public);
                IS_LOADED_METHOD_INFO = renderDocType.GetMethod("IsLoaded", BindingFlags.Static | BindingFlags.Public);
                IS_SUPPORTED_METHOD_INFO = renderDocType.GetMethod("IsSupported", BindingFlags.Static | BindingFlags.Public);
                LOAD_METHOD_INFO = renderDocType.GetMethod("Load", BindingFlags.Static | BindingFlags.Public);
            }

            public static bool IsInstalled()
            {
                return (bool)IS_INSTALLED_METHOD_INFO.Invoke(null, null);
            }

            public static bool IsLoaded()
            {
                return (bool)IS_LOADED_METHOD_INFO.Invoke(null, null);
            }

            public static bool IsSupported()
            {
                return (bool)IS_SUPPORTED_METHOD_INFO.Invoke(null, null);
            }

            public static void Load()
            {
                LOAD_METHOD_INFO.Invoke(null, null);
            }
        }

        public static class EditorGUI
        {
            private static EventInfo HYPERLINK_LICKED_EVENTINFO;
            private static PropertyInfo HYPERLINKINFOS_PROPERTYINFO;
            public static EventInfo HyperLinkClicked()
            {
                if (HYPERLINK_LICKED_EVENTINFO == null)
                {
                    HYPERLINK_LICKED_EVENTINFO = typeof(UnityEditor.EditorGUILayout).Assembly
                        .GetType("UnityEditor.EditorGUI")
                        .GetEvent("hyperLinkClicked", BindingFlags.Static | BindingFlags.NonPublic);
                }

                return HYPERLINK_LICKED_EVENTINFO;
            }

            public static void Add_HyperLinkClicked(EventHandler handler)
            {
                ms_Parameters1[0] = handler;
                HyperLinkClicked().AddMethod.Invoke(null, ms_Parameters1);
            }

            public static void Remove_HyperLinkClicked(EventHandler handler)
            {
                ms_Parameters1[0] = handler;
                HyperLinkClicked().RemoveMethod.Invoke(null, ms_Parameters1);
            }

            public static Dictionary<string, string> Get_HyperlinkInfos(EventArgs e)
            {
                if (HYPERLINKINFOS_PROPERTYINFO == null)
                {
                    HYPERLINKINFOS_PROPERTYINFO = e.GetType().GetProperty("hyperlinkInfos");
                }
                return (Dictionary<string, string>)HYPERLINKINFOS_PROPERTYINFO.GetValue(e);
            }
        }

        public static class CodeEditor
        {
            //public static Dictionary<string, string> GetFoundScriptEditorPaths()
            //{

            //}
        }
    }
}