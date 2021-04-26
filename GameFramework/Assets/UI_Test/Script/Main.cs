using System.Collections;
using System.Collections.Generic;
using FairyGUI;
using GF.Core;
using GF.UI;
using UnityEngine;

namespace GF.UI
{
    public class Main : FairyGUIBaseWindow
    {
        private GGroup m_group;
        private GButton m_button;
        public override void Init()
        {
            m_group = contentPane.GetChild("button_group").asGroup;
            m_button = contentPane.GetChild("n0").asButton;
            m_button.onClick.Add(() =>
            {
 
                Kernel.UiManager.HideWindow(GetType(),false);
                Kernel.UiManager.OpenWindow(typeof(Boss));
            });
        }

        public override void OnOpen()
        {
        
        }

        public override void OnPause()
        {
            m_group.visible = false;
        }

        public override void OnResume()
        {
            m_group.visible = true;
        }
    

        public override void OnClose()
        {
            Close();
        }
    }

}
