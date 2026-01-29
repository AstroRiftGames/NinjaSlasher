using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/Sets/Player Audio Set")]
public class PlayerAudioSet : ScriptableObject
{
    [Header("Movement")]
    public AudioEvent movementLoop;

    [Header("Landing")]
    public AudioEvent landGeneral;
    public AudioEvent landGround;
    public AudioEvent landStone;
    public AudioEvent landWood;
    public AudioEvent landSlippery;
    public AudioEvent landElastic;
    public AudioEvent landBreakable;

    [Header("Combat")]
    public AudioEvent attack;
    public AudioEvent parrySwing;
    public AudioEvent projectileParried;

    [Header("State")]
    public AudioEvent die;
    public AudioEvent[] koVariants;
}
