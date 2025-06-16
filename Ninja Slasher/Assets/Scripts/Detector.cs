using UnityEngine;

public class Detector : MonoBehaviour
{
    [SerializeField] private float delayBeforeActivation = 0.5f;
    [SerializeField] private TurretTrap[] turrets;

    private bool activated = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!activated && collision.CompareTag("Player"))
        {
            activated = true;
            StartCoroutine(ActivateTrapRoutine());
        }
    }

    private System.Collections.IEnumerator ActivateTrapRoutine()
    {
        yield return new WaitForSeconds(delayBeforeActivation);
        foreach (var turret in turrets)
        {
            turret.Activate();
        }
    }
}