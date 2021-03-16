using System;

namespace GF.DebugPanel
{
    public abstract class BaseMonoBehaviourTab : ITab
    {
        public BaseMonoBehaviourTab()
        {
            DebugPanelMonoBehaviour.GetInstance().AddTab(this);
        }

        ~BaseMonoBehaviourTab()
        {
            DebugPanelMonoBehaviour.GetInstance().RemoveTab(this);
        }

        public abstract void DoGUI(IGUIDrawer drawer);

        public abstract void OnUpdate(float time, float deltaTime);
        public abstract void OnFixedUpdate(float time, float deltaTime);
        public abstract void OnLateUpdate(float time, float deltaTime);
        public abstract void OnEditorUpdate(float time, float deltaTime);
    }
}