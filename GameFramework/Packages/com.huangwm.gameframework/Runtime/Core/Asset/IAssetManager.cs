using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GF.Core.Asset
{
    public interface IAssetManager
    {
        /// <summary>
        /// 异步加载一个资源
        /// 通过这个方法加载的资源，必须通过<see cref="ReleaseAsset()"/>来释放
        /// </summary>
        /// <param name="callback">(key, UnityEngine.Object)</param>
        void LoadAssetAsync(string key, Action<string, object> callback);
        /// <summary>
        /// 异步加载一组资源
        /// 通过这个方法加载的资源，必须通过<see cref="ReleaseAsset()"/>来释放
        /// </summary>
        /// <param name="callback">(key, List<UnityEngine.Object>)</param>
        void LoadAssetsByTagAsync(string key, Action<string, object> callback);
        /// <summary>
        /// 释放资源
        /// </summary>
        void ReleaseAsset(UnityEngine.Object obj);

        /// <summary>
        /// 异步实例化一个物体
        /// 通过这个方法实例化的物体，必须通过<see cref="ReleaseInstance()"/>来释放
        /// </summary>
        /// <param name="callback">(key, UnityEngine.GameObject)</param>
        void InstantiateAsync(string key
            , Vector3 position
            , Quaternion rotation
            , Transform parent
            , Action<string, object> callback);
        /// <summary>
        /// 释放物体
        /// 和<see cref="UnityEngine.Object.Destroy()"/>的效果一样
        /// </summary>
        void ReleaseInstance(GameObject instance);

        /// <summary>
        /// 加载一个场景
        /// 通过这个方法加载的场景，必须通过<see cref="UnloadSceneAsync()"/>来释放
        /// </summary>
        /// <param name="callback">(key, UnityEngine.SceneManagement.Scene)</param>
        void LoadSceneAsync(string key
            , LoadSceneMode loadMode
            , bool activateOnLoad
            , Action<string, object> callback);
        /// <summary>
        /// 释放物体
        /// 和<see cref="SceneManager.UnloadSceneAsync(Scene)"/>的效果一样
        /// </summary>
        /// <param name="callback">
        /// (key, null)
        /// Q：这里并不会回调object，为什么还要用Action<string, object>？
        /// A：不同操作的Action不同不便于资源管理器来统一管理
        /// </param>
        void UnloadSceneAsync(Scene scene, Action<string, object> callback);
    }
}