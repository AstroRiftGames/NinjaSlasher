using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/Boss/Multi-Attack Drone Audio Set")]
public class MultiAttackDroneAudioSet : AudioSet
{
    [Header("Intro / Idle / Movement")]
    public AudioEvent intro;
    public AudioEvent idleLoop;
    public AudioEvent flyAwayLoop;

    [Header("Attacks")]
    public AudioEvent rebound;
    public AudioEvent burst;
    public AudioEvent cone;

    [Header("Hits")]
    public AudioEvent hitByProj;
    public AudioEvent hitByGround;
    public AudioEvent hitByPlayer;

    [Header("Defeat")]
    public AudioEvent defeated;

    public MultiAttackDroneAudioSet()
    {
    }
}
