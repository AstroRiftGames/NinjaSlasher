using UnityEngine;

[CreateAssetMenu(menuName = "/Game/Audio/Sets/Enemy Audio Set")]
public class EnemyAudioSet : ScriptableObject
{
    [Header("Core")]
    public AudioEvent spawn;
    public AudioEvent idle;
    public AudioEvent attack;
    public AudioEvent hit;
    public AudioEvent death;
}
