using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/Sets/UI Audio Set")]
public class UIAudioSet : ScriptableObject
{
    [Header("Basic UI")]
    public AudioEvent tapSplash;
    public AudioEvent select;
    public AudioEvent transition;
    public AudioEvent transitionSlash;
    public AudioEvent showConfig;

    [Header("PowerUps / Actions")]
    public AudioEvent powerUp;
    public AudioEvent claim;

    [Header("Results")]
    public AudioEvent victory;
    public AudioEvent defeat;

    [Header("Diegetic UI")]
    public AudioEvent wheelSpin;
    public AudioEvent leverPull;

    [Header("Rewards")]
    public AudioEvent rewardCoins;
    public AudioEvent rewardPrizeCoins;
    public AudioEvent rewardPrize;
}
