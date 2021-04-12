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
	}

	public struct InstantiateBasicData
	{
		
		public bool IsWorldSpace;
		public Transform Parent;
		public Vector3 Position;
	}
}