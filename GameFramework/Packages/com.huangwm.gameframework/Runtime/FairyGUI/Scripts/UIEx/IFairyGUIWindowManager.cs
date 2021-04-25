

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
	     
    	}
    
        /// <summary>
        /// 暂时隐藏窗体
        /// </summary>
        /// <param name="FairyGUIWindowName">窗体类型枚举</param>
    	public void HideWindow(Type FairyGUIWindowType,bool unLoadAssets = false)
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
			    if (unLoadAssets)
			    {
				    _fairyGUIPackageManager.UnloadAssets(bw.FairyGuiWindowInfo.packageName);
				    bw.AssetLoaded = false;
			    }
			    
			    if (bw.Name == FairyGUIWindowType.Name)
				    break;
		    }
		    if (_showWindowStack.Count >= 1)
		    {
			    bw = _showWindowStack.Peek();
			    if (!bw.AssetLoaded)
			    {
				    _fairyGUIPackageManager.ReloadAssets(bw.FairyGuiWindowInfo.packageName);
				    bw.AssetLoaded = true;
			    }
			    
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
