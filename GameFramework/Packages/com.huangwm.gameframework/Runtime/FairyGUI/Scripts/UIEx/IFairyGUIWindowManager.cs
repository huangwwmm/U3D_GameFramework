

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using FairyGUI;
using GF.Core.Behaviour;
using LitJson;
using UnityEngine;

namespace GF.UI
{
	/// <summary>
	/// 此处用抽象类，是因为有一些公共方法
	/// </summary>
	public abstract class IFairyGUIWindowManager : BaseBehaviour
{
    	#region 数据成员
        //包管理器
        protected FairyGUIPackageManager _fairyGUIPackageManager=new FairyGUIPackageManager();
	    //显示窗体栈
	    protected Stack<FairyGUIBaseWindow> _showWindowStack = new Stack<FairyGUIBaseWindow>();
	    //窗体列表
	    protected List<FairyGUIBaseWindow> _windowList=new List<FairyGUIBaseWindow>();

        private List<FairyGUIWindowInfo> _windowInfos;
        private static IFairyGUIWindowManager _instance;
    	#endregion

        #region 属性

        #endregion

        #region 方法

        public IFairyGUIWindowManager()
        {
	        Init();
        }
        /// <summary>
        /// 加载配置文件，创建窗体类。
        /// </summary>
        protected void Init()
    	{
	        string bytePath = ConstData.UIConfigPath;;
	        if (File.Exists(bytePath))
	        {
		        using (FileStream fileStream=new FileStream(bytePath,FileMode.Open,FileAccess.Read,FileShare.Read))
		        {
			        using (StreamReader streamReader=new StreamReader(fileStream,Encoding.UTF8))
			        {
				        _windowInfos = JsonMapper.ToObject<List<FairyGUIWindowInfo>>(streamReader.ReadToEnd());
			        }
		        }
	        }
	        else
	        {
		        Debug.LogError("读取配置文件失败，请重新生成配置文件！");
		        return;
	        }
    		foreach (var item in _windowInfos)
    		{
			    if (item.fairyGuiWindowType == FairyGUIWindowTypes.Resource)
			    {
				    _fairyGUIPackageManager.AddPackage(item.packagePath,item.packageName);
			    }    
    		}
    	}
    
        /// <summary>
        /// 通过窗体类型来得到对应窗体信息
        /// </summary>
        /// <param name="FairyGUIWindowName">窗体类型</param>
        /// <returns>窗体实例</returns>
        protected FairyGUIBaseWindow GetFairyGUIBaseWindow(Type FairyGUIWindowType)
        {
	        string WindowName = FairyGUIWindowType.Name;
    		foreach (var item in _windowList)
    		{
    			if (item.Name == WindowName)
    				return item;
    		}
    		 return null;
    	}

        /// <summary>
        /// 根据窗体类型获取相应的包的数据（包）
        /// </summary>
        /// <param name="FairyGUIWindowType"></param>
        /// <returns></returns>
        protected FairyGUIWindowInfo GetFairyGUIWindowInfo(Type FairyGUIWindowType)
        {
	        string WindowName = FairyGUIWindowType.Name;
	        foreach (var item in _windowInfos)
	        {
		        if (item.ConstainWindow(WindowName))
			        return item;
	        }
	        return null;
        }
    	
        /// 显示窗体
        /// </summary>
        /// <param name="FairyGUIWindowName">窗体类型枚举</param>
        /// <param name="hidePrevious">隐藏上一个？</param>
        /// <param name="unLoadPreviousAsset">卸载上一个界面的资源，hidePrevious = false，unLoadPreviousAsset = false，尽量设置为flase，重新加载会消耗资源</param>
        /// <param name="windowName">加载的窗体，也就是组件的名字，为null的时候，为json中的默认组件,开放此参数是因为一个包里可以有多个组件</param>
        public virtual void OpenWindow(Type fairyGUIWindowType,bool hidePrevious = false,bool unLoadPreviousAsset = false)
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
		    // bw = GetFairyGUIBaseWindow(fairyGUIWindowType);
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
				  //   _fairyGUIPackageManager.AddPackage(fairyGuiWindowInfo.packagePath,fairyGuiWindowInfo.packageName);
				  // //  _fairyGUIPackageManager.UnloadAssets(fairyGuiWindowInfo.packageName);
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
      //
		    // if (!bw.AssetLoaded)
		    // {
			   //  _fairyGUIPackageManager.ReloadAssets(bw.FairyGuiWindowInfo.packageName);
		    // }
		    //
		    // if (!bw.HasOpen)
		    // {
			   //  _showWindowStack.Push(bw);
			   //  bw.HasOpen = true;
			   //  bw.OnBeforeOpen();
			   //  bw.OnOpen();
			   //  bw.Show();
		    // }
		    // else
		    // {
			   //  bw.OnResume(); 
			   //  bw.Show();
		    // }
    	}
    
        /// <summary>
        /// 暂时隐藏窗体
        /// </summary>
        /// <param name="FairyGUIWindowName">窗体类型枚举</param>
    	public void HideWindow(Type FairyGUIWindowType,string WindowName = null)
    	{
    		
	        FairyGUIBaseWindow bw = GetFairyGUIBaseWindow(FairyGUIWindowType);
	        if (bw == null)
	        {
		        Debug.LogError("要关闭的不是窗体，请检查HideWindow函数的参数是否为FairyGUIBaseWindow的子类并且已在注册表注册！");
		        return;
	        }
		    while(_showWindowStack.Count>0)
		    {
			    bw = _showWindowStack.Pop();
			    bw.OnPause();
			    bw.Hide();
			    _fairyGUIPackageManager.UnloadAssets(bw.FairyGuiWindowInfo.packageName);
			    if (bw.Name == FairyGUIWindowType.Name)
				    break;
		    }
		    if (_showWindowStack.Count >= 1)
		    {
			    bw = _showWindowStack.Peek();
			    _fairyGUIPackageManager.ReloadAssets(bw.FairyGuiWindowInfo.packageName);
			    bw.OnResume();
			    bw.Show();
		    }
    	}
        
        /// <summary>
        /// 销毁所有窗体
        /// </summary>
	    public void DestroyAllWindow()
	    {
		    while (_showWindowStack.Count > 0)
		    {
			    FairyGUIBaseWindow bw = _showWindowStack.Pop();
			    bw.OnBeforeClose();
			    bw.OnClose();
			    bw.Hide();
			   // _fairyGUIPackageManager.UnloadAssets(bw.FairyGuiWindowInfo.packageName);
		    }

		    _fairyGUIPackageManager.RemoveAllPackage();
		    
	    }

        public override void OnDisable()
        {
	        base.OnDisable();
	        DestroyAllWindow();
        }

        #endregion
}

}
