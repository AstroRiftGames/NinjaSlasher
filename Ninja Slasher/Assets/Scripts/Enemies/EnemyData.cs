using System;
using UnityEngine;

public enum EnemyType
{
    ScoutBot,
    GuardBot,
    NanoSwarm,
    BlazeUnit,
    RicochetBot,
    MiniSwarmBot,
}
[Serializable]
public class VulnerabilityValues
{
    public bool fromUp;
    public bool fromDown;
    public bool fromBehind;
    public bool fromFront;
}

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Scriptable Object/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [SerializeField] EnemyType _type;
    [SerializeField] GameObject _projectile;
    [SerializeField] float _range;
    [SerializeField] VulnerabilityValues _isVulnerable;

    public EnemyType Type => _type;
    public GameObject Projectile => _projectile;
    public float Range => _range;
    public VulnerabilityValues IsVulnerable => _isVulnerable;
}
