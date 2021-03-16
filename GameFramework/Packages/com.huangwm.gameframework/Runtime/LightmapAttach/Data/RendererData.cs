using UnityEngine;

namespace GF.Renderer.LightmapAttach.Data
{
    [System.Serializable]
    public class RendererData
    {
        public int LightmapIndex;
        public Vector4 LightmapScaleOffset;
        public int RealtimeLightmapIndex;
        public Vector4 RealtimeLightmapScaleOffset;

#if UNITY_EDITOR
        public void Export(UnityEngine.Renderer renderer)
        {
            LightmapIndex = renderer.lightmapIndex;
            LightmapScaleOffset = renderer.lightmapScaleOffset;
            RealtimeLightmapIndex = renderer.realtimeLightmapIndex;
            RealtimeLightmapScaleOffset = renderer.realtimeLightmapScaleOffset;
        }
#endif

        public void Attache(UnityEngine.Renderer renderer)
        {
            renderer.lightmapIndex = LightmapIndex;
            renderer.lightmapScaleOffset = LightmapScaleOffset;
            renderer.realtimeLightmapIndex = RealtimeLightmapIndex;
            renderer.realtimeLightmapScaleOffset = RealtimeLightmapScaleOffset;
        }
    }
}