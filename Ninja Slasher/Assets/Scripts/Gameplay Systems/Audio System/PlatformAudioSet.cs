using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/Sets/Platform Audio Set")]
public class PlatformAudioSet : AudioSet
{
    public AudioEvent PlayerEnter;
    public AudioEvent PlayerExit;
    public AudioEvent Interaction;
    public AudioEvent DestroyPlatform;
    public AudioEvent Idle;
    public AudioEvent Idle2;
}
