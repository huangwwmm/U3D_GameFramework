using System;
using System.Collections.Generic;
using GF.Common.Debug;
using GF.Common.Utility;
using GF.Renderer.LightmapAttach.Data;
using GFEditor.Renderer.LightmapAttach.Setting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GFEditor.Renderer.LightmapAttach.Exporter
{
    public static class ExportUtility
    {
        public static string GetExporterPath(Scene scene)
        {
            string exporterFileName;
            switch (LightmapAttachSetting.GetInstance().ExporterFileNameType)
            {
                case ExporterFileNameType.GUID:
                    exporterFileName = AssetDatabase.AssetPathToGUID(scene.path) + ".asset";
                    break;
                case ExporterFileNameType.Path:
                    exporterFileName = StringUtility.FormatToFileName(scene.path) + ".asset";
                    break;
                default:
                    MDebug.LogError("Renderer", "Not handle ExporterFileNameType: " + LightmapAttachSetting.GetInstance().ExporterFileNameType);
                    return null;
            }

            string exporterPath = $"{LightmapAttachSetting.GetInstance().ExporterFolder}/{exporterFileName}";
            return exporterPath;
        }

        public static void ExportLightmapSettingData(string exportPath)
        {
            LightmapSettingData lightmapSettingData = ScriptableObject.CreateInstance(typeof(LightmapSettingData)) as LightmapSettingData;
            lightmapSettingData.LightmapsMode = LightmapSettings.lightmapsMode;
            LightmapData[] lightmapDatas = LightmapSettings.lightmaps;
            lightmapSettingData.LightmapColor = new Texture2D[lightmapDatas.Length];
            lightmapSettingData.LightmapDir = new Texture2D[lightmapDatas.Length];
            lightmapSettingData.ShadowMask = new Texture2D[lightmapDatas.Length];
            for (int iLightmap = 0; iLightmap < lightmapDatas.Length; iLightmap++)
            {
                LightmapData iterLightmapData = lightmapDatas[iLightmap];
                lightmapSettingData.LightmapColor[iLightmap] = iterLightmapData.lightmapColor;
                lightmapSettingData.LightmapDir[iLightmap] = iterLightmapData.lightmapDir;
                lightmapSettingData.ShadowMask[iLightmap] = iterLightmapData.shadowMask;
            }

            AssetDatabase.CreateAsset(lightmapSettingData, exportPath);
        }

        public static void ExportIndexRenderersData(string exportPath, GameObject root, bool includeDeactive)
        {
            IndexRenderersData indexRenderersData = ScriptableObject.CreateInstance(typeof(IndexRenderersData)) as IndexRenderersData;

            indexRenderersData.RenderersIndices = new List<IndexRenderersData.Indices>();
            indexRenderersData.RenderersData = new List<RendererData>();

            Transform iterTransform = root.transform;
            Stack<int> indices = new Stack<int>();
            InternalExport(indexRenderersData, indices, iterTransform, includeDeactive);

            AssetDatabase.CreateAsset(indexRenderersData, exportPath);
        }

        private static void InternalExport(IndexRenderersData indexRenderersData, Stack<int> indices, Transform rootTransform, bool includeDeactive)
        {
            int childCount = rootTransform.childCount;
            for (int iChild = 0; iChild < childCount; iChild++)
            {
                Transform iterChild = rootTransform.GetChild(iChild);
                GameObject iterGameObject = iterChild.gameObject;
                if (!includeDeactive
                    && !iterGameObject.activeSelf)
                {
                    continue;
                }

                indices.Push(iChild);
                if (Common.Utility.ObjectUtility.HasStaticEditorFlag(iterGameObject
#if UNITY_2019_1_OR_NEWER
                    , StaticEditorFlags.ContributeGI
#else
                    , UnityEditor.StaticEditorFlags.LightmapStatic
#endif
                    ))
                {
                    UnityEngine.Renderer iterRenderer = iterGameObject.GetComponent<UnityEngine.Renderer>();
                    if (iterRenderer)
                    {
                        indexRenderersData.RenderersIndices.Add(new IndexRenderersData.Indices(indices));

                        RendererData rendererData = new RendererData();
                        rendererData.Export(iterRenderer);
                        indexRenderersData.RenderersData.Add(rendererData);
                    }
                }

                InternalExport(indexRenderersData, indices, iterChild, includeDeactive);
                indices.Pop();
            }
        }
    }
}