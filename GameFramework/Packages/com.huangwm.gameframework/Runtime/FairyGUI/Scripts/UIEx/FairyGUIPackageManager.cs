using System;
using System.Collections.Generic;
using FairyGUI;
using UnityEngine;

namespace GF.UI
{
    public class FairyGUIPackageManager
    {
        #region 数据成员

        //记录已经添加的包的字典
        private Dictionary<string, bool> _packageAddDict = new Dictionary<string, bool>();

        #endregion

        #region 方法

        /// <summary>
        /// 加载FairyGUI中的包
        /// </summary>
        /// <param name="packagePath">包路径</param>
        /// <param name="packageName">包名</param>
        public void AddPackage(string packagePath,string packageName)
        {
            if (CheckPackageHaveAdd(packagePath,packageName) == false)
            {
                try
                {
                    UIPackage.AddPackage(packagePath+packageName);
                    _packageAddDict.Add(packagePath+packageName, true);
                }
                catch (Exception e)
                {
                    Debug.LogError("无法加载"+packageName+"包，请检查在"+packagePath+"目录下是否存在"+packageName+":"+e);
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #endif
                    return;
                }
            }
        }
        
        /// <summary>
        /// 加载FairyGUI中的包
        /// </summary>
        /// <param name="bundle">bundle</param>
        /// <param name="packageName">包名</param>
        public void AddPackage(AssetBundle bundle,string packageName)
        {
            if (CheckPackageHaveAdd(packageName) == false)
            {
                try
                {
                    UIPackage.AddPackage(bundle);
                    _packageAddDict.Add(packageName, true);
                }
                catch (Exception e)
                {
                    Debug.LogError("无法加载"+packageName+"包，请检查在"+bundle.name+":"+e);
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                    return;
                }
            }
        }

        /// <summary>
        /// 检查FairyGUI中的包是否已经加载
        /// </summary>
        /// <param name="packagePath">包路径</param>
        /// <param name="packageName">包名</param>
        public bool CheckPackageHaveAdd(string packagePath,string packageName)
        {
            return _packageAddDict.ContainsKey(packagePath+packageName);
        }
        
        /// <summary>
        /// 检查FairyGUI中的包是否已经加载
        /// </summary>
        /// <param name="packageName">包名</param>
        public bool CheckPackageHaveAdd(string packageName)
        {
            return _packageAddDict.ContainsKey(packageName);
        }

        /// <summary>
        /// 卸载已经加载的包
        /// </summary>
        /// <param name="packageName">包名</param>
        public void RemovePackage(string packageName)
        {
            try
            {
                UIPackage.RemovePackage(packageName);
                _packageAddDict.Remove(packageName);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #endif
            }
        }

        public void RemoveAllPackage()
        {
            UIPackage.RemoveAllPackages();
        }

        /// <summary>
        /// 从内存中卸载包的资源，并没有移除包。
        /// </summary>
        /// <param name="packageName">包名</param>
        public void UnloadAssets(string packageName)
        {
            try
            {
                UIPackage package = UIPackage.GetByName(packageName);
                if (package != null)
                {
                    package.UnloadAssets();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #endif
            }
            
        }

        /// <summary>
        /// 重新加载包的资源进入内存。
        /// </summary>
        /// <param name="packageName">包名</param>
        public void ReloadAssets(string packageName)
        {
            try
            {
                UIPackage package = UIPackage.GetByName(packageName);
                package.ReloadAssets();
            }
            catch (Exception e)
            {
                Debug.LogError("资源加载错误，请检查是否在调用了DestroyAllWindow方法后又调用了OpenWindow方法:"+e);
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #endif
            }
           
        }

        #endregion

        
    }
}