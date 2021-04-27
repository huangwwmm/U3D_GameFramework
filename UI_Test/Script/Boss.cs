using System.Collections;
using System.Collections.Generic;
using FairyGUI;
using GF.Core;
using GF.UI;
using UnityEngine;

namespace GF.UI
{
    public class Boss : FairyGUIBaseWindow
    {
        private Transition t4;
        public override void OnInitWindow()
        {
            t4 = contentPane.GetTransition("t4");
        }

        public override void OnOpen(object obj)
        {
            Play_Transition();
        }

        public override void OnPause()
        {
        
        }

        public override void OnResume()
        {
            Play_Transition();
        }

        public override void OnClose()
        {
           
        }

        private void Play_Transition()
        {
            t4.Play(() =>
            {
                Kernel.UiManager.HideWindow(typeof(Boss));
                Kernel.UiManager.OpenWindow(typeof(Main),null);
            });
        }
    }

}
