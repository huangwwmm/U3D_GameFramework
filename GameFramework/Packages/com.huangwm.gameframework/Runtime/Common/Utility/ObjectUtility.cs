using GF.Common.Debug;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GF.Common.Utility
{
    public static class ObjectUtility
    {
        private static MethodInfo METHODINFO_FIND_OBJECT_FROM_INSTANCE_ID;

        /// <summary>
        /// 获取一个transform相对rootTransform的缩放
        /// 如果rootTransform为null，则获得transform相对世界空间缩放
        /// </summary>
        public static Vector3 CalculateLossyScale(Transform transform, Transform rootTransform = null)
        {
            if (rootTransform == null)
            {
                return transform.lossyScale;
            }
            else
            {
#if GF_DEBUG
                const int MAX_DEEP = 1000;
                int deep = 0;
#endif

                Transform iterTransform = transform;
                Vector3 scale = Vector3.one;
                while (iterTransform != rootTransform
                        && iterTransform != null)
                {
                    scale = MathUtility.EachMulti(scale, iterTransform.localScale);
                    iterTransform = iterTransform.parent;
#if GF_DEBUG
                    if (deep++ >= MAX_DEEP)
                    {
                        MDebug.Assert(false, "CalculateLossyScale Deep超出限制，是不是代码逻辑有问题");
                        break;
                    }
#endif
                }
                return scale;
            }
        }

        /// <summary>
        /// 计算一个Transform的name Path
        /// 例，一个Transform结构：
        ///		A
        ///		|-B
        ///		  |-C
        ///		    |-D
        ///		  |-E
        ///	D的Path = /A/B/C/D
        ///	E的Path = /A/E
        /// </summary>
        public static string CalculateTransformPath(Transform transform, Transform rootTransform = null)
        {
#if GF_DEBUG
            const int MAX_DEEP = 1000;
            int deep = 0;
#endif

            StringBuilder stringBuilder = StringUtility.AllocStringBuilder();
            Transform iterTransform = transform;
            while (iterTransform != rootTransform
                    && iterTransform != null)
            {
                stringBuilder.Insert(0, "/" + iterTransform.name);
                iterTransform = iterTransform.parent;

#if GF_DEBUG
                if (deep++ >= MAX_DEEP)
                {
                    MDebug.Assert(false, "CalculateTransformPath Deep超出限制，是不是代码逻辑有问题");
                    break;
                }
#endif
            }
            return StringUtility.ReleaseStringBuilder(stringBuilder);
        }

        /// <summary>
        /// 计算一个Transform的Index Path，并转换为string
        /// 结果是倒序的
        /// 例，一个Transform结构：
        ///		A
        ///		|-B
        ///		|-C
        ///		  |-D
        ///		    |-E
        ///		  |-F
        ///	E的Path = 0/0/1/
        ///	F的Path = 1/1/
        /// </summary>
        public static string CalculateTransformIndexPathStringReverseOrder(Transform transform, Transform rootTransform = null)
        {
#if UNITY_EDITOR
            const int MAX_DEEP = 1000;
            int deep = 0;
#endif
            if (transform == null)
            {
                return "";
            }
            StringBuilder stringBuilder = StringUtility.AllocStringBuilder();
            Transform iterTransform = transform;
            Transform iterTransformParent = iterTransform.parent;
            while (iterTransformParent != rootTransform
                    && iterTransformParent != null)
            {
                int index = int.MinValue;
                for (int iChild = 0; iChild < iterTransformParent.childCount; iChild++)
                {
                    if (iterTransformParent.GetChild(iChild) == iterTransform)
                    {
                        index = iChild;
                        break;
                    }
                }
                stringBuilder.Append(index).Append("/");

                iterTransform = iterTransformParent;
                iterTransformParent = iterTransformParent.parent;

#if UNITY_EDITOR
                if (deep++ >= MAX_DEEP)
                {
                    MDebug.Assert(false, "CalculateTransformIndexPathStringReverseOrder Deep超出限制，是不是代码逻辑有问题");
                    break;
                }
#endif
            }
            return StringUtility.ReleaseStringBuilder(stringBuilder);
        }

        /// <summary>
        /// <see cref="CalculateTransformPath"/>
        /// </summary>
        public static bool TryFindTransformByPath(out Transform result, Scene scene, string path)
        {
            result = null;

            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (!scene.IsValid())
            {
                return false;
            }

            string[] nodes = path.Split(new char[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (nodes.Length == 0)
            {
                return false;
            }

            GameObject[] rootGameObjects = scene.GetRootGameObjects();
            for (int iGameObject = 0; iGameObject < rootGameObjects.Length; iGameObject++)
            {
                GameObject iterGameObject = rootGameObjects[iGameObject];
                if (iterGameObject.name == nodes[0])
                {
                    result = iterGameObject.transform;
                    break;
                }
            }

            if (result == null)
            {
                return false;
            }

            for (int iNode = 1; iNode < nodes.Length; iNode++)
            {
                string node = nodes[iNode];
                result = result.Find(node);
                if (result == null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 移除节点下的所有子节点
        /// </summary>
        public static void DestroyAllChildern(Transform transform)
        {
            for (int iChild = transform.childCount - 1; iChild >= 0; iChild--)
            {
                Object.DestroyImmediate(transform.GetChild(iChild).gameObject);
            }
        }

        /// <summary>
        /// 寻找obj上的transfrom
        /// </summary>
        public static Transform FindTransform(Object obj)
        {
            Transform transform;

            if (obj is Transform)
            {
                transform = obj as Transform;
            }
            else if (obj is Component)
            {
                transform = (obj as Component).transform;
            }
            else if (obj is GameObject)
            {
                transform = (obj as GameObject).transform;
            }
            else
            {
                transform = null;
            }
            return transform;
        }

        /// <summary>
        /// 获取某个节点的根节点
        /// </summary>
        public static Transform GetRootParent(Transform transform)
        {
            Transform iterParent = transform;
            while (iterParent.parent != null)
            {
                iterParent = iterParent.parent;
            }
            return iterParent;
        }

        /// <summary>
        /// 收集节点的所有子节点
        /// </summary>
        /// <param name="result">搜集的结果</param>
        /// <param name="rootTransform">搜集的根节点</param>
        /// <param name="includeDeactivate">是否包括deactive的节点</param>
        public static void CollectAllChildren(List<GameObject> result, Transform rootTransform, bool includeDeactivate = true)
        {
            int childCount = rootTransform.childCount;
            for (int iChild = 0; iChild < childCount; iChild++)
            {
                Transform iterChild = rootTransform.GetChild(iChild);
                GameObject iterGameObject = iterChild.gameObject;
                if (includeDeactivate
                    || iterGameObject.activeSelf)
                {
                    result.Add(iterGameObject);
                    CollectAllChildren(result, iterChild, includeDeactivate);
                }
            }
        }

        /// <summary>
        /// 收集<see cref="Object.DontDestroyOnLoad(Object)"/>以外的所有GameObject
        /// </summary>
        public static void CollectAllGameObjectWithoutDontDestroyOnLoad(List<GameObject> result, string[] excludeSceneNames = null)
        {
            for (int iScene = 0; iScene < SceneManager.sceneCount; iScene++)
            {
                Scene iterScene = SceneManager.GetSceneAt(iScene);
                if (excludeSceneNames != null)
                {
                    string iterSceneName = iterScene.name;
                    bool isExcludeScene = false;
                    for (int iExcludeScene = 0; iExcludeScene < excludeSceneNames.Length; iExcludeScene++)
                    {
                        if (iterSceneName == excludeSceneNames[iExcludeScene])
                        {
                            isExcludeScene = true;
                            break;
                        }
                    }
                    if (isExcludeScene)
                    {
                        continue;
                    }
                }

                GameObject[] rootGameObjects = iterScene.GetRootGameObjects();
                for (int iRootGameObject = 0; iRootGameObject < rootGameObjects.Length; iRootGameObject++)
                {
                    GameObject iterRootGameObject = rootGameObjects[iRootGameObject];
                    result.Add(iterRootGameObject);
                    CollectAllChildren(result, iterRootGameObject.transform, true);
                }
            }
        }

        /// <summary>
        /// 收集<see cref="Object.DontDestroyOnLoad(Object)"/>中所有的GameObject
        /// </summary>
        public static void CollectAllGameObjectFromDontDestroyOnLoad(List<GameObject> gameObjects)
        {
            List<GameObject> rootGameObjectsExcludeDontDestroyOnLoad = new List<GameObject>();

            for (int iScene = 0; iScene < SceneManager.sceneCount; iScene++)
            {
                Scene iterScene = SceneManager.GetSceneAt(iScene);
                GameObject[] rootGameObjects = iterScene.GetRootGameObjects();
                rootGameObjectsExcludeDontDestroyOnLoad.AddRange(rootGameObjects);
            }

            List<GameObject> rootGameObjectsFromDontDestroyOnLoad = new List<GameObject>();
            GameObject[] allGameObject = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int iGameObject = 0; iGameObject < allGameObject.Length; iGameObject++)
            {
                GameObject iterGameObject = allGameObject[iGameObject].transform.root.gameObject;
                if (iterGameObject.hideFlags == HideFlags.None
                    && !rootGameObjectsFromDontDestroyOnLoad.Contains(iterGameObject)
                    && !rootGameObjectsExcludeDontDestroyOnLoad.Contains(iterGameObject))
                {
                    rootGameObjectsFromDontDestroyOnLoad.Add(iterGameObject);
                }
            }

            for (int iRootGameObject = 0; iRootGameObject < rootGameObjectsFromDontDestroyOnLoad.Count; iRootGameObject++)
            {
                GameObject iterRootGameObject = rootGameObjectsFromDontDestroyOnLoad[iRootGameObject];
                gameObjects.Add(iterRootGameObject);
                CollectAllChildren(gameObjects, iterRootGameObject.transform, true);
            }
        }

        /// <summary>
        /// 查找<see cref="Object.GetInstanceID"/>对应的Object
        /// </summary>
        public static Object FindObjectFromInstanceID(int instanceID)
        {
            if (METHODINFO_FIND_OBJECT_FROM_INSTANCE_ID == null)
            {
                METHODINFO_FIND_OBJECT_FROM_INSTANCE_ID = typeof(Object)
                    .GetMethod("FindObjectFromInstanceID", BindingFlags.NonPublic | BindingFlags.Static);
            }
            return (Object)METHODINFO_FIND_OBJECT_FROM_INSTANCE_ID.Invoke(null, new object[] { instanceID });
        }
    }
}