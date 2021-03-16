using System;

namespace GF.DebugPanel
{
    /// <summary>
    /// 这个接口存在的意义就是为了实现<see cref="DummyDebugPanel"/>
    /// </summary>
    public interface IDebugPanel
    {
        /// <summary>
        /// 注册一个GUI事件
        /// </summary>
        void RegistGUI(string tabName, Action<IGUIDrawer> doGUIAction, bool onlyRuntime);
        /// <summary>
        /// 注销一个GUI事件
        /// </summary>
        void UnregistGUI(string tabName);
        void SwitchTab(string tabName);
    }
}