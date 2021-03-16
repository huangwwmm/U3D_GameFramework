using GF.Common;
using GF.Common.Debug;
using GFEditor.Common.Utility;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GFEditor.Renderer.LightmapAttach.Exporter
{
    public class ExportWindow : EditorWindow
    {
        private Scene m_ActiveScene;
        private MonoScript m_ExporterScript;
        private BaseExporter m_Exporter;

        [CTMenuItem("渲染/导出Lightmap")]
        private static void OpenWindow()
        {
            GetWindow<ExportWindow>("导出Lightmap");
        }

        protected void OnEnable()
        {
            m_ActiveScene = SceneManager.GetActiveScene();
            ApplyScene();
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
        }

        protected void OnDisable()
        {
            m_Exporter?.OnRelease();
            m_Exporter = null;
            m_ExporterScript = null;
            EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
        }

        protected void OnGUI()
        {
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("只能在编辑器下使用"
                    , MessageType.Warning);
                return;
            }

            if (!m_ActiveScene.IsValid())
            {
                EditorGUILayout.HelpBox("需要先打开一个场景"
                    , MessageType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(m_ActiveScene.path))
            {
                EditorGUILayout.HelpBox("只有保存到文件的场景才能使用此功能"
                    , MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("当前的场景", m_ActiveScene.path);

            EditorGUILayout.BeginHorizontal();
            if (EGLUtility.ObjectField(out m_ExporterScript, "导出设置", m_ExporterScript, false))
            {
                if (!m_ExporterScript.GetClass().IsSubclassOf(typeof(BaseExporter)))
                {
                    m_ExporterScript = null;
                }
            }
            if (m_ExporterScript != null
                && GUILayout.Button("重新创建设置", GUILayout.Width(100)))
            {
                m_Exporter?.OnRelease();
                string exporterPath = ExportUtility.GetExporterPath(m_ActiveScene);
                m_Exporter = CreateInstance(m_ExporterScript.GetClass()) as BaseExporter;
                if (m_Exporter)
                {
                    m_Exporter.OnInitialize();
                    AssetDatabase.CreateAsset(m_Exporter, exporterPath);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (m_Exporter)
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                if (GUILayout.Button("导出"))
                {
                    if (m_Exporter.CanExport(out string errorMessage))
                    {
                        try
                        {
                            m_Exporter.Export(m_ActiveScene);
                            EditorUtility.DisplayDialog("LightmapAttach", "导出完成", "Ok");
                        }
                        catch (Exception e)
                        {
                            MDebug.LogError("Renderer", "导出Lightmap失败，Exception:\n" + e.ToString());
                            EditorUtility.DisplayDialog("LightmapAttach", "导出失败:\n" + e.ToString(), "Ok");
                            MDebug.LogError("Renderer", "导出Lightmap失败，Exception:\n" + e.ToString());
                        }
                        finally
                        {
                            EditorUtility.ClearProgressBar();
                        }
                    }
                    else
                    {
                        MDebug.LogError("Renderer", "不能导出Lightmap：\n" + errorMessage);
                        EditorUtility.DisplayDialog("LightmapAttach"
                            , "不能导出：\n" + errorMessage
                            , "OK");
                    }
                }

                EditorGUI.BeginChangeCheck();
                m_Exporter.OnGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(m_Exporter);
                }
            }
        }

        private void ApplyScene()
        {
            m_ExporterScript = null;
            m_Exporter?.OnRelease();
            m_Exporter = null;

            if (!m_ActiveScene.IsValid()
                || string.IsNullOrEmpty(m_ActiveScene.path))
            {
                return;
            }

            m_Exporter = AssetDatabase.LoadMainAssetAtPath(ExportUtility.GetExporterPath(m_ActiveScene)) as BaseExporter;
            if (m_Exporter)
            {
                m_ExporterScript = MonoScript.FromScriptableObject(m_Exporter);
                m_Exporter.OnInitialize();
            }
        }

        private void OnSceneChanged(Scene previousScene, Scene activeScene)
        {
            m_ActiveScene = activeScene;
            ApplyScene();
        }
    }
}