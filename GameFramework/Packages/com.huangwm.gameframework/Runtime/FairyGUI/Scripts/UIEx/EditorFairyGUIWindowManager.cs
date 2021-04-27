using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using FairyGUI;
using LitJson;

namespace GF.UI
{
	public class EditorFairyGUIWindowManager : IFairyGUIWindowManager
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
			    //查看ui信息是否存在，信息存在，也就是包会存在，后续可以加载
			    FairyGUIWindowInfo fairyGuiWindowInfo = GetFairyGUIWindowInfo(fairyGUIWindowType);
			    if (fairyGuiWindowInfo == null)
			    {
				    Debug.LogError("要开打的不是窗体，请检查OpenWindow函数的参数是否为FairyGUIBaseWindow的子类并且已在注册表注册！");
				    return;
			    }
			    
			    //加载包
			    if (fairyGuiWindowInfo.fairyGuiWindowType == FairyGUIWindowTypes.Window)
			    {
				    _fairyGUIPackageManager.AddPackage(fairyGuiWindowInfo.packagePath,fairyGuiWindowInfo.packageName);
			    }
			    else
			    {
				    Debug.LogError("尝试创建资源包中的组件！资源包中的任何组件都无法被创建。");
				    return;
			    }
			    
			    //加载脚本
			    Assembly assembly=Assembly.LoadFrom(fairyGUIWindowType.Assembly.Location);
			    string windowName = fairyGUIWindowType.Name;
			    string typeName = fairyGUIWindowType.FullName;
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
			    bw.OnInitWindow();
			    _windowList.Add(bw);
		    }

		    AfterOpenWindow(bw,sourceData);

        }

    }
}

