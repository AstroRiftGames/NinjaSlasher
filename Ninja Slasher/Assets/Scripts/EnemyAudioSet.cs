using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/Sets/Projectile Audio Set")]
public class ProjectileAudioSet : AudioSet
{
    public AudioEvent spawn;
    public AudioEvent impact;
    public AudioEvent parried;
}
