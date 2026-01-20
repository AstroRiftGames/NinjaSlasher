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
    SeekerUnit,
    X0N3,
    BL4ZT,
    OM3GA,
    KRUSH9,
    DemolitionSentinel,
    MultiTaskDrone,
    Arachnomadre,
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
    [SerializeField] Projectile _projectile;
    [SerializeField] private float _range;
    [SerializeField] VulnerabilityValues _isVulnerable;

    public EnemyType Type => _type;
    public Projectile Projectile => _projectile;
    public float Range => _range;
    public VulnerabilityValues IsVulnerable => _isVulnerable;
}
