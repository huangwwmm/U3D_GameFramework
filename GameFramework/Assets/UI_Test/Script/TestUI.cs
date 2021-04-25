
using GF.Core;
using GF.UI;

public class TestUI : FairyGUIBaseWindow
{

    public override void OnBeforeOpen()
    {
        contentPane.MakeFullScreen();
        contentPane.GetChild("n0").onClick.Add(() =>
        {
            Kernel.UiManager.OpenWindow(typeof(TestUI2));
        });
        //GImage window=contentPane.GetChild("Window") as GImage;
        //window.MakeFullScreen();
    }

    public override void OnOpen()
    {
        Show();
    }

    public override void OnPause()
    {
        
    }

    public override void OnResume()
    {
        
    }

    public override void OnBeforeClose()
    {
        
    }

    public override void OnClose()
    {
        
    }
}
