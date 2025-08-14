using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class ProtoSlasher : BossEnemy
{
    [SerializeField] Transform _refPoint;
    [SerializeField] Transform _body;
    private bool _isShooting;
    private bool _isDashing;
    private Vector2 _dirToPlayer;

    [Header("Dash")]
    [SerializeField] float _dashForce;
    [SerializeField] float _timeBetweenDashes;
    [SerializeField] int _dashesAmount;

    [Header("Projectiles Burst")]
    [SerializeField] int _projectilesAmount;
    [SerializeField] float _timeBetweenShots;
    [SerializeField] GameObject _burstProjectilePrefab;
    
    [Header("Energy Shot")]
    [SerializeField] GameObject _energyProjectilePrefab;
    [SerializeField] float _chargingTime;
    [SerializeField] float _inputChargeTime;
    private float _inputTime;

    private void Update()
    {
        _dirToPlayer = GetDirToPlayer();
        if (_isShooting)
        {
            Aim();
        }
        else if(!_isDashing)
        {
            if(Input.GetKeyDown(KeyCode.Space)) StartCoroutine(ManageDashes());
            if (Input.GetKeyDown(KeyCode.G)) _inputTime = Time.time;
            if(Input.GetKeyUp(KeyCode.G))
            {
                if(Time.time >= _inputTime + _inputChargeTime)
                {
                    StartCoroutine(ManageEnergyShot());
                }
                else
                {
                    StartCoroutine(ManageBurst());
                }
            }
        }
    }


    #region ATTACK MANAGEMENT
    private IEnumerator ManageDashes()
    {
        _isDashing = true;
        for (int n = 0; n < _dashesAmount; n++)
        {
            NinjaDash();
            yield return new WaitForSeconds(_timeBetweenDashes);
        }
        _isDashing = false;
    }
    private IEnumerator ManageBurst()
    {
        _isShooting = true;
        for (int n = 0; n < _projectilesAmount; n++) 
        {
            Shoot(_burstProjectilePrefab);
            yield return new WaitForSeconds(_timeBetweenShots);
        }
        _isShooting = false;
    }
    private IEnumerator ManageEnergyShot()
    {
        _isShooting = true;
        yield return new WaitForSeconds(_chargingTime);
        Shoot(_energyProjectilePrefab);
        _isShooting = false;
    }
    #endregion

    #region DASHING
    private void NinjaDash()
    {
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(_dirToPlayer * _dashForce, ForceMode2D.Impulse);
    }
    #endregion

    #region AIMING && SHOOTING
    private void Aim()
    {
        Vector2 dir = _dirToPlayer;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _body.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Shoot(GameObject projectile)
    {
        Instantiate(projectile, _refPoint.position, Quaternion.identity).TryGetComponent(out Projectile newProjectile);
        newProjectile.Initialize(_dirToPlayer, transform);
    }
    #endregion

    #region PARRYING

    #endregion

    #region RESOURCES
    private Vector2 GetDirToPlayer() => (_player.position - transform.position).normalized;
    #endregion
    private void OnCollisionEnter2D(Collision2D collision)
    {
        string colTag = collision.gameObject.tag;
        if (colTag is "Ceiling" or "Scenario" or "Floor" or "Platform")
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }
}