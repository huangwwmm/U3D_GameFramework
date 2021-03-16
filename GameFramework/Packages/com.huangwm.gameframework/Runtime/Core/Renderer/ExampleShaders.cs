using GF.Common;
using System.Collections.Generic;
using UnityEngine;

namespace GF.Core.Renderer
{
    [CTCreateAssetMenuItem("渲染/创建示例Shaders", "ExampleShaders")]
    public class ExampleShaders : IShaders
    {
        public List<Shader> Shaders = new List<Shader>();
        public List<ComputeShader> ComputeShaders = new List<ComputeShader>();
        public List<ShaderVariantCollection> ShaderVariantCollections = new List<ShaderVariantCollection>();

        public override void AddComputeShader(ComputeShader shader)
        {
            ComputeShaders.Add(shader);
        }

        public override void AddShader(Shader shader)
        {
            Shaders.Add(shader);
        }

        public override void AddShaderVariantCollection(ShaderVariantCollection shaderVariantCollection)
        {
            ShaderVariantCollections.Add(shaderVariantCollection);
        }

        public override void ClearComputeShaders()
        {
            ComputeShaders.Clear();
        }

        public override void ClearShaders()
        {
            Shaders.Clear();
        }

        public override void ClearShaderVariantCollections()
        {
            ShaderVariantCollections.Clear();
        }

        public override bool TryFindShader(string name, out Shader shader)
        {
            for (int iShader = 0; iShader < Shaders.Count; iShader++)
            {
                Shader iterShader = Shaders[iShader];
                if (iterShader != null
                    && iterShader.name == name)
                {
                    shader = iterShader;
                    return true;
                }
            }

            shader = null;
            return false;
        }
    }
}