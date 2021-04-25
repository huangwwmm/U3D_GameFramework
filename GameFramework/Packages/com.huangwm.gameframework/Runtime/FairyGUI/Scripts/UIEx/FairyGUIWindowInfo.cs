
using System.Collections.Generic;

namespace GF.UI
{
    [System.Serializable]
    public class FairyGUIWindowInfo
    {
        #region 数据成员

        private string m_packagePath;
        //包名
        private string m_packageName;
        //所有窗体名称
        private List<string> m_windowNames;
        //menuType
        private FairyGUIWindowTypes _fairyGUIWindowType;

        #endregion

        #region 属性
        /// <summary>
        /// 编辑器下的路径,唯一性------------
        /// </summary>
        public string packagePath
        {
            get{return m_packagePath;}
            set{m_packagePath = value;} 
        }

        /// <summary>
        /// 包名字，唯一性------------
        /// </summary>
        public string packageName
        {
            get{return m_packageName;}
            set{m_packageName = value;} 
            
        }

        /// <summary>
        /// 组件名字，可以有多个组件，这个为默认组件，不唯一
        /// </summary>
        public List<string> WindowNames
        {
            get{return m_windowNames;}
            set{m_windowNames = value;} 
        }

        /// <summary>
        /// 包类型，如果为Resource，说明需要提前加载
        /// </summary>
        public FairyGUIWindowTypes fairyGuiWindowType
        {
            get{return _fairyGUIWindowType;}
            set{_fairyGUIWindowType = value;} 
        }

        #endregion
        
        
        #region //方法

        public bool ConstainWindow(string windowName)
        {
            return m_windowNames.Contains(windowName);
        }
        
        #endregion
    } 
}
