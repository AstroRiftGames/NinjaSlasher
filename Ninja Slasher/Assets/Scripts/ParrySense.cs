using System.Collections;
using UnityEngine;

public class ParrySense : MonoBehaviour
{
    [SerializeField] private GameObject _indicator;
    [SerializeField] private CircleCollider2D _col;
    [SerializeField] private PlayerController _playerController;

    private void OnEnable()
    {
        _playerController.OnParry += (SetIndicatorActive);
        _playerController.OnHit += (SetIndicatorActive);
    }

    private void OnDisable()
    {
        _playerController.OnParry -= (SetIndicatorActive);
        _playerController.OnHit -= (SetIndicatorActive);

    }

    private void Awake()
    {
        _col.radius = _playerController.Model.ParryRange / _playerController.transform.localScale.x;
        _indicator.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Vector2 directionToProjectile = collision.transform.position - transform.position;
        if(Physics2D.Raycast(transform.position, directionToProjectile, _col.radius, LayerMask.GetMask("Obstacle", "Scenario")))
        {
            return;
        }
        if (collision.TryGetComponent(out Projectile proj))
        {
            if (proj == null)
                return;
            if(proj.IsParryable)
                SetIndicatorActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.TryGetComponent(out Projectile proj))
        {
            if (proj != null && proj.IsParryable)
            {
                SetIndicatorActive(false);
            }
        }
        SetIndicatorActive(false);
    }

    private void SetIndicatorActive(bool shouldActivate)
    {
        _indicator.SetActive(shouldActivate);
    }
}
