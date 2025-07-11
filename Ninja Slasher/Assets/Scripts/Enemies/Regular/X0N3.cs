using System.Collections;
using UnityEngine;

public class X0N3 : RangeEnemy
{
    [SerializeField] float _burstCD;
    [SerializeField] int _shotsAmount;

    public override void Attack()
    {
        SetLastAttack();
        StartCoroutine(ShootBurst());
    }

    private IEnumerator ShootBurst()
    {
        for(int n = 0; n < _shotsAmount; n++)
        {
            Shoot();
            yield return new WaitForSeconds(_burstCD);
        }
    }
}
