using UnityEngine;

namespace GF.DebugPanel
{
    public interface IGUIDrawer
    {
        bool IsEditor();
        float GetPanelWidth();

        void BeginToolbarHorizontal();
        void EndHorizontal();

        bool ToolbarToggle(bool value, string label);
        bool ToolbarButton(bool value, string label);

        void ImportantLabel(string label);

        void Space();

        void CalcMinMaxWidth_Button(GUIContent content, out float minWidth, out float maxWidth);
    }
}