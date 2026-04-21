using System;
using UnityEngine;

public class BossEnemy : Enemy
{
    [SerializeField] protected float _waitTime;
    protected bool _isWaiting = false;

    public virtual bool IsVulnerable { get; protected set; }
    public override void OnEnable()
    {
        base.OnEnable();
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }

    public override void Die()
    {
        if (_isDead) return;
        _isDead = true;

        _animator.SetTrigger("OnHit");
        RegisterKill();
    }
}
