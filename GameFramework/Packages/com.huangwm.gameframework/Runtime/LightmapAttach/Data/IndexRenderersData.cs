using GF.Common.Debug;
using System.Collections.Generic;
using UnityEngine;

namespace GF.Renderer.LightmapAttach.Data
{
    [System.Serializable]
    public class IndexRenderersData : ScriptableObject
    {
        public List<Indices> RenderersIndices;
        public List<RendererData> RenderersData;

        public void Attach(GameObject root)
        {
            Transform rootTransform = root.transform;
            for (int iRenderer = 0; iRenderer < RenderersIndices.Count; iRenderer++)
            {
                int[] iterIndices = RenderersIndices[iRenderer].Value;
                RendererData iterRendererData = RenderersData[iRenderer];
                Transform iterTransform = rootTransform;
                for (int iIndex = 0; iIndex < iterIndices.Length; iIndex++)
                {
                    int index = iterIndices[iIndex];
                    if (iterTransform.childCount > index)
                    {
                        iterTransform = iterTransform.GetChild(index);
                    }
                    else
                    {
                        MDebug.LogError("Renderer"
                            , $"LightmapAttach cant find transform({iterTransform.name}) child({index})"
                            , iterTransform);
                        iterTransform = null;
                        break;
                    }
                }

                if (iterTransform)
                {
                    UnityEngine.Renderer iterRenderer = iterTransform.GetComponent<UnityEngine.Renderer>();
                    if (iterRenderer)
                    {
                        iterRendererData.Attache(iterRenderer);
                    }
                    else
                    {
                        MDebug.LogError("Renderer"
                            , $"LightmapAttach cant find renderer on transform({iterTransform.name})"
                            , iterTransform);
                    }
                }
            }
        }

        [System.Serializable]
        public struct Indices
        {
            public int[] Value;

            public Indices(Stack<int> stack)
            {
                Value = stack.ToArray();

                int lastIndex = Value.Length - 1;
                for (int index = 0; index < Value.Length / 2; index++)
                {
                    int swapIndex = lastIndex - index;
                    Value[index] = Value[index] + Value[swapIndex];
                    Value[swapIndex] = Value[index] - Value[swapIndex];
                    Value[index] = Value[index] - Value[swapIndex];
                }
            }
        }
    }
}