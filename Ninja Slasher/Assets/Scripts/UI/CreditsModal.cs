using UnityEngine;

public class CreditsModal : UIModalBase
{
    protected override void OnShown()
    {
        Debug.Log("[CreditsModal] Modal de créditos mostrado");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(MusicClip.Credits, true);
        }
    }

    protected override void OnHidden()
    {
        Debug.Log("[CreditsModal] Modal de créditos ocultado");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(MusicClip.MainMenu, true);
        }
    }
}