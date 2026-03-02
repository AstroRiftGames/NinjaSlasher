using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/Sets/Element Audio Set")]
public class ElementAudioSet : AudioSet
{
    public AudioEvent PlayerEnter;
    public AudioEvent PlayerExit;
    public AudioEvent Start;
    public AudioEvent Loop;
    public AudioEvent End;
}
