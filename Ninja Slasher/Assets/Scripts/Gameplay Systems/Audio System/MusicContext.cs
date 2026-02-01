using UnityEngine;

public class MusicContext : MonoBehaviour
{
    [SerializeField] private AudioEvent _splashScreenMusic;
    [SerializeField] private AudioEvent _LevelSelectionMusic;
    [SerializeField] private AudioEvent _creditsMusic;

    private void OnEnable()
    {
        MusicEvents.OnEnterSplash += PlaySplashScreen;
        MusicEvents.OnEnterLevelSelection += PlayLevelSelectionScreen;
        MusicEvents.OnEnterCredits += PlayCredits;
    }

    private void OnDisable()
    {
        MusicEvents.OnEnterSplash -= PlaySplashScreen;
        MusicEvents.OnEnterLevelSelection -= PlayLevelSelectionScreen;
        MusicEvents.OnEnterCredits -= PlayCredits;
    }

    public void PlaySplashScreen() => AudioService.Instance.PlayMusic(_splashScreenMusic);
    public void PlayLevelSelectionScreen() => AudioService.Instance.PlayMusic(_LevelSelectionMusic);
    public void PlayCredits() => AudioService.Instance.PlayMusic(_creditsMusic);
}
