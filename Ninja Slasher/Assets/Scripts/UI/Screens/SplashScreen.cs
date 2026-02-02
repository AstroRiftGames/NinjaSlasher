using UnityEngine;

public class SplashScreen : UIScreenBase
{
    protected override void OnShown()
    {
        Debug.Log("[SplashScreen] Pantalla de splash mostrada");
    }

    protected override void OnHidden()
    {
        Debug.Log("[SplashScreen] Pantalla de splash ocultada");
    }
}