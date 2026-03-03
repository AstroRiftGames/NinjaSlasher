using System;
using UnityEngine;

public class BossEnemy : Enemy
{
    [SerializeField] protected float _waitTime;
    protected bool _isWaiting = false;
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
        _animator.SetTrigger("OnHit");
        RegisterKill();
    }
}
