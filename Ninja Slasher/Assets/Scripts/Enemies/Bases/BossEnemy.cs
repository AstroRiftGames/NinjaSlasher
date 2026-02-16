using System;
using UnityEngine;

public class BossEnemy : Enemy
{
    [SerializeField] protected float _waitTime;
    protected bool _isWaiting = false;
    public override void OnEnable()
    {
        base.OnEnable();
        CustomUpdateManager.Instance.SubscribeToUpdate(CustomUpdate);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        CustomUpdateManager.Instance.UnsubscribeFromUpdate(CustomUpdate);
    }

    public override void Die()
    {
        _animator.SetTrigger("OnHit");
        RegisterKill();
    }
}
