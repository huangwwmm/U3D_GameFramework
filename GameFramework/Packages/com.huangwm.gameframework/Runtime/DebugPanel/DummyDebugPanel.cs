using System;

namespace GF.DebugPanel
{
    /// <summary>
    /// 这个类存在的意义是：
    ///		如果游戏中不需要开启DebugPanel时，<see cref="DebugPanelInstance"/>使用这个Dummy的DebugPanel
    ///		这样外面使用DebugPanel的地方就不需要判断当前是否启用DebugPanel了
    /// </summary>
    internal class DummyDebugPanel : IDebugPanel
    {
        public void RegistGUI(string tabName, Action<IGUIDrawer> doGUIAction, bool onlyRuntime)
        {
        }

        public void UnregistGUI(string tabName)
        {
        }

        public void SwitchTab(string tabName)
        {
        }

    }
}