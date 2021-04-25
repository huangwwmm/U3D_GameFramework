using FairyGUI;

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

        #region 属性

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

        #region 方法

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

        /// <summary>
        /// 创建前期 主要用于寻找view上的组件
        /// </summary>
        public abstract void OnBeforeOpen();

        /// <summary>
        /// 创建成功 主要用于逻辑注册,最后调用Show()方法
        /// </summary>
        public abstract void OnOpen();

        /// <summary>
        /// 当在该界面上再打开界面时,暂停该界面
        /// </summary>
        /// 
        public abstract void OnPause();

        /// <summary>
        /// 当在关闭该界面上打开的界面时,恢复该界面
        /// </summary>
        public abstract void OnResume();

        public abstract void OnBeforeClose();

        /// <summary>
        /// 界面关闭,最后调用Hide()方法并将this.contentPane销毁
        /// </summary>
        public abstract void OnClose();

        #endregion


        
    }
}
   

