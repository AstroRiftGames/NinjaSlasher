using System.Collections;
using TMPro;
using UnityEngine;

public class MiniSwarmBot : FlyingEnemy
{
    [SerializeField] float _deploymentTime;
    private bool _isDying = false;

    public IEnumerator Initialize()
    {
        yield return new WaitForSeconds(_deploymentTime);
        _col.enabled = true;
    }

    public override void Die()
    {
        _isDying = true;
        _rb.linearVelocity = Vector2.zero;
        base.Die();
    }

    public override void CustomUpdate()
    {
        if (GameManager.Instance.PlayerHasDied) return;
        if (!_isDying)
        {
            base.CustomUpdate();
        }
    }
}
