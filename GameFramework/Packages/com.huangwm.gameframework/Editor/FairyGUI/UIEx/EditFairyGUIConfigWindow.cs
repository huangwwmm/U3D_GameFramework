using System.Collections.Generic;
using System.IO;
using System.Text;
using GF.UI;
using UnityEditor;
using UnityEngine;
using LitJson;

namespace FairyGUI.VFairyGUI.Editor
{
    public class EditFairyGUIConfigWindow : EditorWindow
    {
        #region 数据成员

        

        private static List<FairyGUIWindowInfo> _fairyGUIConfig = new List<FairyGUIWindowInfo>();

        #endregion

        #region 方法

        /// <summary>
        /// 更新枚举类文件
        /// </summary>
        /// <param name="fairyGUIResourcesPath">FairyGUI资源所在路径</param>
        [MenuItem("Window/FairyGUIEx - UpdateConfigFile")]

        public static void UpdateWindowInformation()
        {
            EditorApplication.ExecuteMenuItem("Window/FairyGUI - Refresh Packages And Panels");
            _fairyGUIConfig.Clear();
            string[] fairyGUIResourcesPaths = Directory.GetFiles(ConstData.UIDataPath ,
                "*_fui.bytes", SearchOption.AllDirectories);
            for (int i = 0; i < fairyGUIResourcesPaths.Length; i++)
            {
                string resourcesPath = fairyGUIResourcesPaths[i].Replace("\\", "/");
                string[] paths = resourcesPath.Split('/');
                string packageName = paths[paths.Length - 1].Substring(0, paths[paths.Length - 1].Length - 10);
                string windowName = packageName;             
                FairyGUIWindowInfo fairyGuiWindowInfo = new FairyGUIWindowInfo();
                int packagePathStart = resourcesPath.IndexOf("Assets/");
                fairyGuiWindowInfo.packagePath = resourcesPath.Substring(packagePathStart, resourcesPath.IndexOf(paths[paths.Length - 1]) -packagePathStart);
                fairyGuiWindowInfo.packageName = packageName;
                
                UIPackage.RemoveAllPackages();
                UIPackage.AddPackage(fairyGuiWindowInfo.packagePath + fairyGuiWindowInfo.packageName);
                List<string> list = new List<string>();
                foreach (var v in UIPackage.GetPackageItems(fairyGuiWindowInfo.packageName))
                {
                    if (v.Value.objectType == ObjectType.Component)
                    {
                        list.Add(v.Value.name);
                    }
                }
                fairyGuiWindowInfo.WindowNames = list;
                if (packageName.StartsWith(ConstData.ResPackagePrefix))
                {
                    fairyGuiWindowInfo.fairyGuiWindowType = FairyGUIWindowTypes.Resource;
                }
                else
                {
                    fairyGuiWindowInfo.fairyGuiWindowType = FairyGUIWindowTypes.Window;
                }

                _fairyGUIConfig.Add(fairyGuiWindowInfo);
            }
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            string bytePath = ConstData.UIConfigPath;
            if (File.Exists(bytePath))
                File.Delete(bytePath);
            using (FileStream fileStream = new FileStream(bytePath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                fileStream.Write(Encoding.UTF8.GetBytes(JsonMapper.ToJson(_fairyGUIConfig)),0,Encoding.UTF8.GetBytes(JsonMapper.ToJson(_fairyGUIConfig)).Length);
            }
            Debug.Log("Update VFairyGUIConfigFile Succeed！");
            UIPackage.RemoveAllPackages();
            AssetDatabase.Refresh();
        }

        void OnInspectorUpdate()
        {
            //重新绘制
            this.Repaint();
        }

        #endregion
    }
}