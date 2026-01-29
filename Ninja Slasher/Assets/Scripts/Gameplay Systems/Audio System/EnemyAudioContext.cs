using UnityEngine;

public class EnemyAudioContext : MonoBehaviour
{
    [SerializeField] private EnemyData _enemyData;

    public EnemyAudioSet Audio => _enemyData != null ? _enemyData.AudioSet : null;
}
