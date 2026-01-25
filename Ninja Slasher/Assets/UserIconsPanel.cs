using UnityEngine;

public class UserIconsPanel : UIPopupBase
{
    protected override void OnShown()
    {
        Debug.Log("[UserIconsPanel] Panel mostrado");
    }

    protected override void OnHidden()
    {
        Debug.Log("[UserIconsPanel] Panel ocultado");
    }
}