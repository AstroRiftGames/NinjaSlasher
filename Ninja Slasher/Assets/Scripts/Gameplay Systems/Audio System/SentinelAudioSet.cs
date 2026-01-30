using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/Boss/Sentinel Audio Set")]
public class SentinelAudioSet : ScriptableObject
{
    [Header("Intro / Idle")]
    public AudioEvent intro;
    public AudioEvent idleLoop;

    [Header("Attacks")]
    public AudioEvent doubleAttackLeft;
    public AudioEvent doubleAttackRight;
    public AudioEvent heavyAttack;
    public AudioEvent sweepAttack;

    [Header("Vulnerable")]
    public AudioEvent vulnerableEnter;
    public AudioEvent vulnerableLoop;
    public AudioEvent recovered;

    [Header("Defeat")]
    public AudioEvent defeated;
    public AudioEvent defeatedLoop;
}
