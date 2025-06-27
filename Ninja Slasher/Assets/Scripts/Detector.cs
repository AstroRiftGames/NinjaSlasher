using UnityEngine;

public class Detector : MonoBehaviour
{
    [SerializeField] private float delayBeforeActivation;
    [SerializeField] private TurretTrap[] turrets;
    [SerializeField] private float cooldownTime;

    private bool isOnCooldown = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isOnCooldown && collision.CompareTag("Player"))
        {
            isOnCooldown = true;
            StartCoroutine(ActivateTrapRoutine());
        }
    }

    private System.Collections.IEnumerator ActivateTrapRoutine()
    {
        yield return new WaitForSeconds(delayBeforeActivation);

        foreach (var turret in turrets)
        {
            if (turret != null)
            {
                turret.Activate();
            }
        }

        yield return new WaitForSeconds(cooldownTime);
        isOnCooldown = false;
    }
}