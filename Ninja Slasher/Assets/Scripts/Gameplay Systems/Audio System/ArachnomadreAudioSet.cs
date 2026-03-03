using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/Boss/Arachnomadre Audio Set")]
public class ArachnomadreAudioSet : AudioSet
{
    [Header("Intro / Idle / Movement")]
    public AudioEvent intro;
    public AudioEvent idleLoop;
    public AudioEvent movementLoop;

    [Header("Attacks")]
    public AudioEvent eggSpawn;
    public AudioEvent submerge;
    public AudioEvent emerge;

    [Header("Vulnerable")]
    public AudioEvent vulnerableEnter;
    public AudioEvent vulnerableLoop;
    public AudioEvent recovered;

    [Header("Defeat")]
    public AudioEvent defeated;
    public AudioEvent defeatedLoop;
}
