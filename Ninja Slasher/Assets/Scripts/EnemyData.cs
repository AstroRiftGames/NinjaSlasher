using UnityEngine;

public enum EnemyType
{
    Basic,
    Range,
}

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Scriptable Object/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [SerializeField] EnemyType _type;

    public EnemyType Type => _type;
}
