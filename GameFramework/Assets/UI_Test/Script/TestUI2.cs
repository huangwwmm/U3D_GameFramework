
using GF.Core;
using GF.UI;

public class TestUI2 : FairyGUIBaseWindow
{

    public override void OnBeforeOpen()
    {
        contentPane.MakeFullScreen();
        //GImage window=contentPane.GetChild("Window") as GImage;
        //window.MakeFullScreen();
        contentPane.GetChild("n0").onClick.Add(() =>
        {
            Kernel.UiManager.OpenWindow(typeof(TestUI),true);
        });
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
