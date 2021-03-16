using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GFEditor.Asset.AssetBundle.Rule
{
    public enum AutoDependenciesBunleType
    {
        /// <summary>
        /// 所有资源一个Bundle
        /// </summary>
        AllAsset,
        /// <summary>
        /// 单个资源一个Bundle
        /// </summary>
        SingleAsset,
        /// <summary>
        /// 被重复Bundle依赖的资源一个Bundle
        /// </summary>
        DependencyRepeatedBundle,
    }
}