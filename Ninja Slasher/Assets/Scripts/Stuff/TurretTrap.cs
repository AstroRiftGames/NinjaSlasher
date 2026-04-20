using UnityEngine;
using System.Collections;

public class TurretTrap : MonoBehaviour
{
    [SerializeField] private float appearTime;
    [SerializeField] private float laserDuration;
    [SerializeField] private float hideTime;
    [SerializeField] private float timeToShoot;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float laserLength;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LayerMask playerLayer;

    private Transform player;

    private void Awake()
    {
        player = FindObjectOfType<Controller>().transform;
        gameObject.SetActive(false);
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        StartCoroutine(TurretRoutine());
    }

    private IEnumerator TurretRoutine()
    {
        yield return new WaitForSeconds(appearTime);

        if (player != null)
        {
            Vector3 originalFirePosition = firePoint.position;
            Vector2 dir = (player.position - firePoint.position).normalized;

            yield return new WaitForSeconds(timeToShoot);

            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;

                Vector3 endPoint = originalFirePosition + (Vector3)dir * laserLength;

                lineRenderer.SetPosition(0, originalFirePosition);
                lineRenderer.SetPosition(1, endPoint);

                RaycastHit2D hit = Physics2D.Raycast(originalFirePosition, dir, laserLength, playerLayer);

                if (hit.collider != null)
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        PlayerController playerController = hit.collider.GetComponent<PlayerController>();
                        if (playerController != null)
                        {
                            playerController.Die();
                        }
                    }
                }
            }
        }

        yield return new WaitForSeconds(laserDuration);

        if (lineRenderer != null)
            lineRenderer.enabled = false;

        yield return new WaitForSeconds(hideTime);
        gameObject.SetActive(false);
    }
}