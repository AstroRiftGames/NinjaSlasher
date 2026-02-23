using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/Sets/Platform Audio Set")]
public class PlatformAudioSet : ScriptableObject
{
    public AudioEvent PlayerEnter;
    public AudioEvent PlayerExit;
    public AudioEvent Interaction;
    public AudioEvent DestroyPlatform;
}
