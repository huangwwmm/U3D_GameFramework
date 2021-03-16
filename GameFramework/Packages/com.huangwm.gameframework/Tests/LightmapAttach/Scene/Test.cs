using GF.Renderer.LightmapAttach.Data;
using UnityEngine;

namespace GFTests.Renderer.LightmapAttach
{
    public class Test : MonoBehaviour
    {
        public GameObject Prefab;
        public LightmapSettingData LightmapSettingData;
        public IndexRenderersData IndexRenderersData;
        public bool StaticBatching;

        protected void Start()
        {
            GameObject go = Instantiate(Prefab);

            LightmapSettingData.Attach();
            IndexRenderersData.Attach(go);

            if (StaticBatching)
            {
                StaticBatchingUtility.Combine(go);
            }
        }
    }
}