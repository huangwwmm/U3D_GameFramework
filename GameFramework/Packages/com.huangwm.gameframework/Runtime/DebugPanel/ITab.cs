using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GF.DebugPanel
{
    public interface ITab
    {
        void DoGUI(IGUIDrawer drawer);
    }
}