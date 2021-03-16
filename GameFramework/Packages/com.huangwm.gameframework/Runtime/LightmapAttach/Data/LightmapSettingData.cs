using UnityEngine;

namespace GF.Renderer.LightmapAttach.Data
{
    public class LightmapSettingData : ScriptableObject
    {
        public LightmapsMode LightmapsMode;

        public Texture2D[] LightmapColor;
        public Texture2D[] LightmapDir;
        public Texture2D[] ShadowMask;

        public void Attach()
        {
            LightmapSettings.lightmapsMode = LightmapsMode;

            LightmapData[] lightmapDatas = new LightmapData[LightmapColor.Length];
            for (int iLightmap = 0; iLightmap < lightmapDatas.Length; iLightmap++)
            {
                LightmapData iterLightmapData = new LightmapData();
                iterLightmapData.lightmapColor = LightmapColor[iLightmap];
                iterLightmapData.lightmapDir = LightmapDir[iLightmap];
                iterLightmapData.shadowMask = ShadowMask[iLightmap];
                lightmapDatas[iLightmap] = iterLightmapData;
            }
            LightmapSettings.lightmaps = lightmapDatas;
        }
    }
}