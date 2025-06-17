using UnityEngine;
using System.Collections;

public class TurretTrap : MonoBehaviour
{
    [SerializeField] private float appearTime = 0.5f;
    [SerializeField] private float laserDuration = 1f;
    [SerializeField] private float hideTime = 0.5f;
    [SerializeField] private float timeToShoot = 0.5f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float laserLength = 20f;
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
            Vector2 dir = (player.position - firePoint.position).normalized;

            yield return new WaitForSeconds(timeToShoot);

            firePoint.right = dir;

            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, firePoint.position);

                RaycastHit2D hit = Physics2D.Raycast(firePoint.position, dir, laserLength, playerLayer);
                Vector3 endPoint = firePoint.position + (Vector3)dir * laserLength;

                if (hit.collider != null)
                {
                    endPoint = hit.point;
                    if (hit.collider.CompareTag("Player"))
                    {
                        Controller playerController = hit.collider.GetComponent<Controller>();
                        if (playerController != null)
                        {
                            playerController.Die();
                        }
                    }
                }

                lineRenderer.SetPosition(1, endPoint);
            }
        }

        yield return new WaitForSeconds(laserDuration);

        if (lineRenderer != null)
            lineRenderer.enabled = false;

        yield return new WaitForSeconds(hideTime);

        gameObject.SetActive(false);
    }
}