using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/Boss/Sentinel Audio Set")]
public class SentinelAudioSet : AudioSet
{
    [Header("Intro / Idle")]
    public AudioEvent intro;
    public AudioEvent idleLoop;

    [Header("Attacks")]
    public AudioEvent doubleAttackLeft;
    public AudioEvent doubleAttackRight;
    public AudioEvent heavyAttack;
    public AudioEvent sweepAttack;
    public AudioEvent woosh;
    public AudioEvent impact;

    [Header("Vulnerable")]
    public AudioEvent vulnerableEnter;
    public AudioEvent vulnerableLoop;
    public AudioEvent recovered;

    [Header("Chain")]
    public AudioEvent chainDamaged;

    [Header("Defeat")]
    public AudioEvent defeated;
    public AudioEvent defeatedLoop;
}
