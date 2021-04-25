using System;
using UnityEngine;

namespace GF.Asset
{
	public interface IAssetManager
	{
		void InstantiateGameObjectAsync(AssetKey assetKey, Action<AssetKey, UnityEngine.Object> callback, InstantiateBasicData instantiateBasicData = default);
		void ReleaseGameObjectAsync(GameObject asset);

		void LoadAssetAsync(AssetKey assetKey, Action<AssetKey, UnityEngine.Object> callback);
		void UnloadAssetAsync(UnityEngine.Object asset);

		void LoadAssetBundleForFairyGUIAsync(string assetBundleName, Action<AssetBundle> callback);
	}

	public struct InstantiateBasicData
	{
		public bool IsWorldSpace;
		/// <summary>
		/// 如果为Null则把GameObject实例化到Scene的根节点下
		/// </summary>
		public Transform Parent;
		public Vector3 Position;
	}
}