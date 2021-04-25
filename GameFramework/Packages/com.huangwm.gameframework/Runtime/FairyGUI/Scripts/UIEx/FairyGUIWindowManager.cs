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
		public override void OpenWindow(Type fairyGUIWindowType,bool hidePrevious = false,bool unLoadPreviousAsset = false)
    	{
	     //    FairyGUIBaseWindow bw;
    		// if (_showWindowStack.Count > 0)
    		// {
    		// 	bw = _showWindowStack.Peek();
    		// 	bw.OnPause();
      //           if (hidePrevious)
      //           {
	     //            bw.Hide();
      //           }
      //           else
      //           {
	     //            unLoadPreviousAsset = false;
      //           }
      //           if (unLoadPreviousAsset)
      //           {
	     //            _fairyGUIPackageManager.UnloadAssets(bw.FairyGuiWindowInfo.packageName);
	     //            bw.AssetLoaded = false;
      //           }
      //       }
		    // bw = GetFairyGUIBaseWindow(fairyGUIWindowType,windowName);
		    // if (bw == null)
		    // {
			   //  FairyGUIWindowInfo fairyGuiWindowInfo = GetFairyGUIWindowInfo(fairyGUIWindowType);
			   //  if (fairyGuiWindowInfo == null)
			   //  {
				  //   Debug.LogError("要开打的不是窗体，请检查OpenWindow函数的参数是否为FairyGUIBaseWindow的子类并且已在注册表注册！");
				  //   return;
			   //  }
			   //  if (fairyGuiWindowInfo.fairyGuiWindowType == FairyGUIWindowTypes.Window)
			   //  {
				  //  
				  //   //TODO 使用bundle初始化
				  //   _fairyGUIPackageManager.AddPackage(fairyGuiWindowInfo.packagePath,fairyGuiWindowInfo.packageName);
				  //   Assembly assembly=Assembly.LoadFrom(fairyGUIWindowType.Assembly.Location);
				  //   Type type = assembly.GetType(fairyGuiWindowInfo.windowName);
				  //   if (type == null)
				  //   {
					 //    Debug.LogError("导入的"+fairyGuiWindowInfo.packageName+"包，并没有为其创建对应脚本文件，请创建"+fairyGuiWindowInfo.windowName+"脚本并继承自FairyGUIBaseWindow。");
					 //    return;
				  //   }
				  //   object obj = type.Assembly.CreateInstance(type.Name);
				  //   bw = obj as FairyGUIBaseWindow;
				  //   bw.Copy(fairyGuiWindowInfo);
				  //   bw.AssetLoaded = true;
				  //   GComponent view = UIPackage.CreateObject(bw.FairyGuiWindowInfo.packageName, bw.FairyGuiWindowInfo.windowName).asCom;
				  //   bw.SetWindowView(view);
				  //   _windowList.Add(bw);
			   //  }
			   //  else
			   //  {
				  //   Debug.LogError("尝试创建资源包中的组件！资源包中的任何组件都无法被创建。");
				  //   return;
			   //  }
		    // }
		    // else
		    // {
			   //  AfterOpenWindow(bw);
		    // }
        }

	    private void AfterOpenWindow(FairyGUIBaseWindow bw)
	    {
		    if (!bw.AssetLoaded)
		    {
			    _fairyGUIPackageManager.ReloadAssets(bw.FairyGuiWindowInfo.packageName);
		    }
		    
		    if (!bw.HasOpen)
		    {
			    _showWindowStack.Push(bw);
			    bw.HasOpen = true;
			    bw.OnBeforeOpen();
			    bw.OnOpen();
			    bw.Show();
		    }
		    else
		    {
			    bw.OnResume(); 
			    bw.Show();
		    }
	    }

    }
}

