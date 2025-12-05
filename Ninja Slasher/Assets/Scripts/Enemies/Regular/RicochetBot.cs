using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class RicochetBot : RangeEnemy
{
    [SerializeField] private Transform _cannon;
    private bool _isShooting = false;
    public override void CustomUpdate()
    {
        TryAttack();
    }
    private void SetRandomDirection()
    {
        Vector2 newVector = (Vector2) transform.up + new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        SetDirToTarget(newVector.normalized);
    }

    public override void TryAttack()
    {
        if (!_isShooting)
        {
            ChargeAttack();
        }
    }

    private void ChargeAttack()
    {
        _isShooting = true;
        SetRandomDirection();
        StartCoroutine(Aim());
    }

    public override void Attack()
    {
        SetLastAttack();
        _animator.SetTrigger("OnAttack");
    }

    public void PlayShootSFX()
    {
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.E_Ricochet_Shoot, transform.position);
    }

    public void PlayChargeSFX()
    {
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.E_Ricochet_Charge, transform.position);
    }

    private IEnumerator Aim()
    {
        float angle = Mathf.Atan2(_dirToTarget.y, _dirToTarget.x) * Mathf.Rad2Deg - 90f;
        while(Quaternion.Angle(_cannon.rotation, Quaternion.Euler(new Vector3(0, 0, angle))) > 0.5f)
        {
            _cannon.rotation = Quaternion.RotateTowards(_cannon.rotation, Quaternion.Euler(new Vector3(0, 0, angle)), 200 * Time.deltaTime);
            yield return null;
        }
        Attack();
        while(CheckCooldown() == false)
        {
            yield return null;
        }
        _isShooting = false;
    }

    public override void Die()
    {
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.E_Ricochet_Death, transform.position);
        base.Die();
    }
}
