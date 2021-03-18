using System.Collections;
using GF.Core;
using GF.Core.Behaviour;

namespace GF.Asset.Address
{
    public class AssetManager : BaseBehaviour
    {
        public AssetManager()
             : base("AssetManager", (int)BehaviourPriority.AssetManager, BehaviourGroup.Default.ToString())
        {
            // 在初始化完成前停用Update
            SetEnable(false);
        }

        public IEnumerator InitializeAsync(KernelInitializeData initializeData)
        {
            //加载AssetMap
            //加载Bundle依赖文件
            
            yield return null;

            // 恢复Update
            SetEnable(true);
        }
    }
}