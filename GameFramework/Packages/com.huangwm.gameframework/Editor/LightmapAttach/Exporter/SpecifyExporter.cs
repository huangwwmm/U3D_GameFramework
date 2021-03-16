using GFEditor.Common.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GFEditor.Renderer.LightmapAttach.Exporter
{
    public class SpecifyExporter : BaseExporter
    {
        public string TargetPath;
        public string ExportLightmapDataPath;
        public string ExportRendererDataPath;
        public bool IncludeDeactive;

        private GameObject m_Target;

        /// <param name="message">不能导出时的消息提示</param>
        /// <returns>能否导出</returns>
        public override bool CanExport(out string message)
        {
            message = null;
            if (string.IsNullOrEmpty(ExportLightmapDataPath)
                || string.IsNullOrEmpty(ExportRendererDataPath))
            {
                message = "导出目录不存在";
                return false;
            }

            FindTarget();
            if (m_Target == null)
            {
                message = "未指定导出目标";
                return false;
            }
            return true;
        }

        public override void Export(Scene scene)
        {
            ExportUtility.ExportLightmapSettingData(ExportLightmapDataPath);
            ExportUtility.ExportIndexRenderersData(ExportRendererDataPath
                , m_Target
                , IncludeDeactive);
        }

        public override void OnGUI()
        {
            ExportLightmapDataPath = (EGLUtility.File("Lightmap导出路径", ExportLightmapDataPath));
            ExportRendererDataPath = (EGLUtility.File("Renderer导出路径", ExportRendererDataPath));

            if (EGLUtility.ObjectField(out m_Target, "导出目标", m_Target, true))
            {
                TargetPath = GF.Common.Utility.ObjectUtility.CalculateTransformPath(m_Target.transform);
            }
            EditorGUILayout.LabelField("导出目标的LocalID", TargetPath);
            IncludeDeactive = EditorGUILayout.Toggle("是否包含隐藏物体", IncludeDeactive);
        }

        public override void OnInitialize()
        {
            FindTarget();
        }

        public override void OnRelease()
        {
        }

        private void FindTarget()
        {
            GF.Common.Utility.ObjectUtility.TryFindTransformByPath(out Transform targetTransform
                , SceneManager.GetActiveScene()
                , TargetPath);
            if (targetTransform)
            {
                m_Target = targetTransform.gameObject;
            }
        }
    }
}