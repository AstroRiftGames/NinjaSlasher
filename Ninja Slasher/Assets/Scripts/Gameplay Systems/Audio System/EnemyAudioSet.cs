using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/Sets/Enemy Audio Set")]
public class EnemyAudioSet : ScriptableObject
{
    public AudioEvent spawn;
    public AudioEvent idle;
    public AudioEvent shoot;
    public AudioEvent death;
    public AudioEvent hit;
    public AudioEvent explosion;
    public AudioEvent sendReport;
    public AudioEvent detection;
    public AudioEvent charge;
    public AudioEvent collision;
    public AudioEvent move;
}
