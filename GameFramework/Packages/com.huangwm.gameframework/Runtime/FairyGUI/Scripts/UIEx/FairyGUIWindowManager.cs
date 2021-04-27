using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using FairyGUI;
using GF.Asset;
using GF.Core;
using LitJson;

namespace GF.UI
{
	public class FairyGUIWindowManager : IFairyGUIWindowManager
    {
		/// <summary>
		/// 注释见父类
		/// </summary>
		public override void OpenWindow(Type fairyGUIWindowType,object sourceData)
    	{
	        FairyGUIBaseWindow bw;
	        bw = GetFairyGUIBaseWindow(fairyGUIWindowType);
		    if (bw == null)
		    {
			    FairyGUIWindowInfo fairyGuiWindowInfo = GetFairyGUIWindowInfo(fairyGUIWindowType);
			    if (fairyGuiWindowInfo == null)
			    {
				    Debug.LogError("要开打的不是窗体，请检查OpenWindow函数的参数是否为FairyGUIBaseWindow的子类并且已在注册表注册！");
				    return;
			    }
			    if (fairyGuiWindowInfo.fairyGuiWindowType == FairyGUIWindowTypes.Window)
			    {
				    if (!_fairyGUIPackageManager.CheckPackageHaveAdd(fairyGuiWindowInfo.packageName))
				    {
					    Kernel.AssetManager.LoadAssetBundleForFairyGUIAsync(fairyGuiWindowInfo.packageName, (ab) =>
					    {
						    _fairyGUIPackageManager.AddPackage(ab,fairyGuiWindowInfo.packageName);
						    AfterLoadAssetBundle(fairyGUIWindowType,bw,fairyGuiWindowInfo,sourceData);
					    });
				    }
				    else
				    {
					    AfterLoadAssetBundle(fairyGUIWindowType,bw,fairyGuiWindowInfo,sourceData);
				    }
			    }
			    else
			    {
				    Debug.LogError("尝试创建资源包中的组件！资源包中的任何组件都无法被创建。");
				    return;
			    }
		    }
		    else
		    {
			    AfterOpenWindow(bw,sourceData);
		    }
        }

		private void AfterLoadAssetBundle(Type fairyGUIWindowType,FairyGUIBaseWindow bw,FairyGUIWindowInfo fairyGuiWindowInfo,object sourceData)
		{
			string windowName = fairyGUIWindowType.Name;
			string typeName = fairyGUIWindowType.FullName;
			Assembly assembly=Assembly.LoadFrom(fairyGUIWindowType.Assembly.Location);
			Type type = assembly.GetType(typeName);
			if (type == null)
			{
				Debug.LogError("导入的"+fairyGuiWindowInfo.packageName+"包，并没有为其创建对应脚本文件，请创建"+windowName+"脚本并继承自FairyGUIBaseWindow。");
				return;
			}
			object obj = type.Assembly.CreateInstance(typeName);
			bw = obj as FairyGUIBaseWindow;
			bw.Copy(fairyGuiWindowInfo);
			bw.AssetLoaded = true;
			bw.Name = windowName;
			GComponent view = UIPackage.CreateObject(bw.FairyGuiWindowInfo.packageName, windowName).asCom;
			bw.SetWindowView(view);
			_windowList.Add(bw);
			bw.OnInitWindow();
			AfterOpenWindow(bw,sourceData);
		}

    }
}

