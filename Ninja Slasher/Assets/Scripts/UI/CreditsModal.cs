using UnityEngine;

public class CreditsModal : UIModalBase
{
    protected override void OnShown()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(MusicClip.Credits, true);
        }
    }

    protected override void OnHidden()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(MusicClip.MainMenu, true);
        }
    }
}