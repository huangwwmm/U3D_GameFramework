using UnityEngine;

namespace GF.Core.Renderer
{
    /// <summary>
    /// 用于收集项目中的Shader
    /// 没用interface是因为interface不能继承ScriptableObject
    /// </summary>
    public abstract class IShaders : ScriptableObject
    {
        public abstract bool TryFindShader(string name, out Shader shader);
        public abstract void ClearShaders();
        public abstract void AddShader(Shader shader);

        public abstract void ClearComputeShaders();
        public abstract void AddComputeShader(ComputeShader shader);
        
        public abstract void AddShaderVariantCollection(ShaderVariantCollection shaderVariantCollection);
        public abstract void ClearShaderVariantCollections();
    }
}