using FairyGUI;
using GF.Core;
using UnityEngine;

namespace GF.UI
{
        
    public abstract class FairyGUIBaseWindow:Window
    {
        #region 数据成员
        //window信息类
        private FairyGUIWindowInfo m_fairyGUIWindowInfo;
        private bool m_hasOpen = false;
        private bool m_assetLoaded = false;
        private string m_name;
        #endregion

        #region 属性,子类界面不需要访问

        /// <summary>
        /// 界面是否打开
        /// </summary>
        public bool HasOpen
        {
            get{return m_hasOpen;}
            set{m_hasOpen = value;} 
        }

        public FairyGUIWindowInfo FairyGuiWindowInfo
        {
            get{return m_fairyGUIWindowInfo;}
            private set{m_fairyGUIWindowInfo = value;} 
        }

        /// <summary>
        /// 资源是否被加载，资源可单独卸载
        /// </summary>
        public bool AssetLoaded
        {
            get{return m_assetLoaded;}
            set{m_assetLoaded = value;} 
        }

        public string Name
        {
            get => m_name;
            set => m_name = value;
        }

        #endregion

        #region 方法，子类界面不需要访问
        /// <summary>
        /// 为windowInfo赋值
        /// </summary>
        /// <param name="oldWindow"></param>
        public void Copy(FairyGUIWindowInfo oldMenu)
        {
            FairyGuiWindowInfo = oldMenu;
            m_hasOpen = false;
        }
    
        /// <summary>
        /// 设置窗体主组件
        /// </summary>
        /// <param name="view"></param>
        public void SetWindowView(GComponent view)
        {
            this.contentPane = view;
        }
        
        #endregion


        #region 子类界面只需要重写这几个方法即可
        /// <summary>
        /// 第一次创建界面实例，用来获取需要操控的组件
        /// </summary>
        public abstract void OnInitWindow();

        /// <summary>
        /// 打开界面，主要用于逻辑注册事件，根据传入的数据初始化界面
        /// </summary>
        public abstract void OnOpen(object obj);

        /// <summary>
        /// 暂停该界面,不隐藏
        /// </summary>
        /// 
        public abstract void OnPause();

        /// <summary>
        /// 恢复隐藏的界面
        /// </summary>
        public abstract void OnResume();

        /// <summary>
        /// 界面关闭，需要销毁注册的事件
        /// </summary>
        public abstract void OnClose();

        #endregion


        
    }
}
   

