using System.Collections;
using TMPro;
using UnityEngine;

public class MiniSwarmBot : FlyingEnemy
{
    [SerializeField] float _deploymentTime;
    private bool _isDying = false;
    private bool _isDeploying = true;

    public IEnumerator Initialize()
    {
        yield return new WaitForSeconds(_deploymentTime);
        foreach(Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            col.enabled = true;
        }
        _isDeploying = false;
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
        if (_isDying || _isDead) return;
        if (_isDeploying) return;
        base.CustomUpdate();
    }
}
