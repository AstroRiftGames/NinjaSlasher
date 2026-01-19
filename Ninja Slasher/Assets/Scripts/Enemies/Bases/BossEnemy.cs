using AstroRift.Core.Update;
using System;
using UnityEngine;

public class BossEnemy : Enemy
{
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
