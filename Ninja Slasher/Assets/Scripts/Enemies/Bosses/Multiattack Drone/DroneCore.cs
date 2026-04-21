using UnityEngine;

public class DroneCore : BossCore
{
    protected override void OnVulnerableHit(Collider2D collision)
    {
        if (_boss != null)
        {
            _boss.StopAllCoroutines();
            base.OnVulnerableHit(collision);
        }
    }
}
