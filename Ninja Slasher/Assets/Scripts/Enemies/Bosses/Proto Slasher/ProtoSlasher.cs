using System.Collections;
using UnityEngine;

enum AttackEnum
{
    Burst,
    Energy,
    Dash,
}
public class ProtoSlasher : BossEnemy
{
    [SerializeField] Transform _refPoint;
    [SerializeField] Transform _body;
    private Vector2 _dirToPlayer;

    [Header("Attack Management")]
    [SerializeField] float _attackCD;
    private float _lastAttack;
    private bool _isShooting;
    private bool _isDashing;

    [Header("Dash")]
    [SerializeField] [Range(0f, 1f)] float _dashChance;
    [SerializeField] float _dashForce;
    [SerializeField] float _timeBetweenDashes;
    [SerializeField] int _dashesAmount;

    [Header("Projectiles Burst")]
    [SerializeField] int _projectilesAmount;
    [SerializeField] float _timeBetweenShots;
    ObjectPool<Projectile> _burstPool;
    [SerializeField] Projectile _burstProjectilePrefab;
    [SerializeField] int _burstsToEnergy;
    private int _burstsShot;

    [Header("Energy Shot")]
    [SerializeField] Projectile _energyProjectilePrefab;
    [SerializeField] float _chargingTime;
    ObjectPool<Projectile> _energyPool;

    [Header("Parry")]
    [SerializeField] int _maxEnergyParries;
    private int _energyParries;

    [Header("Vulnerability")]
    [SerializeField] CircleCollider2D _parryZone;
    bool _isStunned;
    [SerializeField] float _stunTime;
    [SerializeField] ProtoSlasherCore _core;
    [SerializeField] float _vulnerableTime;

    protected override void Awake()
    {
        base.Awake();

        _energyPool = new ObjectPool<Projectile>(
            _energyProjectilePrefab,
            5,
            transform
        );

        _burstPool = new ObjectPool<Projectile>(
            _burstProjectilePrefab,
            _projectilesAmount * _burstsToEnergy,
            transform
        );
    }

    public override void CustomUpdate()
    {
        _dirToPlayer = GetDirToPlayer();
        if (_isShooting)
        {
            Aim();
        }
        else if(!_isDashing)
        {
            TryAttack();
        }
    }


    #region ATTACK MANAGEMENT

    private void TryAttack()
    {
        if(Time.time >= _lastAttack + _attackCD) 
        {
            switch (ChooseAttack())
            {
                case AttackEnum.Dash:
                    StartCoroutine(ManageDashes());
                    break;
                case AttackEnum.Burst:
                    StartCoroutine(ManageBurst());
                    break;
                case AttackEnum.Energy:
                    StartCoroutine(ManageEnergyShot());
                    break;
            }
            _lastAttack = Time.time;
        }
    }
    private AttackEnum ChooseAttack()
    {
        if (_burstsShot >= _burstsToEnergy) return AttackEnum.Energy;
        else
        {
            float r = Random.Range(0f, 1f);
            if (r <= _dashChance) return AttackEnum.Dash;
            else return AttackEnum.Burst;
        }
    }

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
        _burstsShot++;
        for (int n = 0; n < _projectilesAmount; n++) 
        {
            Shoot(_burstPool);
            yield return new WaitForSeconds(_timeBetweenShots);
        }
        _isShooting = false;
    }
    private IEnumerator ManageEnergyShot()
    {
        _isShooting = true;
        yield return new WaitForSeconds(_chargingTime);
        Shoot(_energyPool);
        _energyParries = 0;
        _burstsShot = 0;
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

    private void Shoot(ObjectPool<Projectile> pool)
    {
        Projectile newProjectile = pool.Get();
        newProjectile.transform.SetPositionAndRotation(_refPoint.position, Quaternion.identity);
        newProjectile.Initialize(_dirToPlayer, transform);
    }
    #endregion

    #region PARRYING

    private void TryParry(bool isEnergyProj)
    {
        if(isEnergyProj)
        {
            _energyParries++;
            if (_energyParries >= _maxEnergyParries)
            {
                StartCoroutine(SetStunned());
            }
        }
        StartParry();
    }

    private void StartParry()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2f, LayerMask.GetMask("Projectiles"));

        foreach (var hit in hits)
        {
            Projectile proj = hit.GetComponent<Projectile>();

            if (proj != null && proj.IsParryable)
            {
                proj.ReflectBackwards(transform, GetDirToPlayer());
            }
        }
    }

    #endregion

    #region STUN/VULNERABILITY MANAGEMENT
    private IEnumerator SetStunned()
    {
        _isStunned = true;
        _parryZone.enabled = false;
        yield return new WaitForSeconds(_stunTime);
        _energyParries = 0;
        _parryZone.enabled = true;
        _isStunned = false;
    }

    private IEnumerator SetVulnerable()
    {
        IsVulnerable = true;
        if (_core != null) _core.enabled = true;
        yield return new WaitForSeconds(_vulnerableTime);
        IsVulnerable = false;
        if (_core != null) _core.enabled = false;
    }
    #endregion

    #region RESOURCES

    private Vector2 GetDirToPlayer() => (_player.position - transform.position).normalized;

    #endregion

    #region COLLISIONS/TRIGGERS MANAGEMENT

    private void OnCollisionEnter2D(Collision2D collision)
    {
        string colTag = collision.gameObject.tag;
        switch(colTag)
        {
            case "Ceiling" or "Scenario" or "Floor" or "Platform":
                _rb.linearVelocity = Vector2.zero;
                break;
            case "Projectile":
                collision.gameObject.TryGetComponent(out Projectile projectile);
                if (projectile.Shooter.CompareTag("Player"))
                {
                    if (!_isStunned)
                    {
                        TryParry(collision.gameObject.name.Contains("Energy"));
                    }
                    else
                    {
                        StartCoroutine(SetVulnerable());
                        projectile.RequestDespawn();
                    }
                }
                break;
            case "Player":
                // Handled by ProtoSlasherCore
                break;
        }
    }

    #endregion
}