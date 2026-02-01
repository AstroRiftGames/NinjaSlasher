using System.Collections;
using UnityEngine;

public enum AttackType
{
    Burst,
    Cone,
    Rebound,
}

public class MultiattackDrone : BossEnemy
{
    [Header("Flying Parameters")]
    [SerializeField] float _minDistance;
    [SerializeField] float _speed;
    [SerializeField] Transform[] Waypoints;
    private Transform _targetWaypoint;
    private Vector2 _playerPos;
    private bool _flyingAway;
    [SerializeField] BoxCollider2D _boxCol;
    [SerializeField] BoxCollider2D _boxTrigger;
    [SerializeField] CapsuleCollider2D _capsuleCol;

    [Header("Bullet Prefabs")]
    [SerializeField] Projectile _coneBullet;
    [SerializeField] Projectile _riccochetBullet;
    [SerializeField] Projectile _burstBullet;

    [Header("Attacking Parameters")]
    [SerializeField] float _cooldown;
    private float _lastAttack;
    [SerializeField] Transform _shootingPoint;
    [Space]
    [SerializeField] int _burstAmount;
    [SerializeField] int _coneAmount;
    [SerializeField] int _reboundAmount;
    [SerializeField] float _timeBetweenShots;
    [SerializeField] float _timeBetweenReboundShots;
    private AttackType _nextAttack;
    private ObjectPool<Projectile> _conePool;
    private ObjectPool<Projectile> _ricochetPool;
    private ObjectPool<Projectile> _burstPool;

    [Header("Vulnerability Parameters")]
    [SerializeField] float _vulnerabilityTime;
    private bool _isVulnerable;

    private bool _isActive = false;
    [SerializeField] private float _waitTime;

    [SerializeField] private ShootingPointContainer _shootingPointContainer;

    protected override void Awake()
    {
        base.Awake();
        _conePool = new ObjectPool<Projectile>(_coneBullet, _coneAmount * 2, transform);
        _ricochetPool = new ObjectPool<Projectile>(_riccochetBullet, _reboundAmount * 2, transform);
        _burstPool = new ObjectPool<Projectile>(_burstBullet, _burstAmount * 2, transform);
    }

    public override void Start()
    {
        base.Start();

        if (_shootingPointContainer != null && _player != null)
        {
            _shootingPointContainer.SetPlayer(_player);
        }

        StartCoroutine(Activate());
    }

    public override void CustomUpdate()
    {
        if (_isActive && !_isVulnerable)
        {
            SetLookingDirection();

            if(!_flyingAway)
            {
                if (CheckDisToPlayer())
                {
                    FlyAway();
                }

                if (CheckCooldown())
                {
                    ChooseAttack();
                    Attack(_nextAttack);
                }
            }

        }
    }

    private void SetLookingDirection()
    {
        bool playerIsOnRight = _player.transform.position.x > transform.position.x;
        Vector3 localScale = transform.localScale;
        localScale.x = playerIsOnRight ? -1 : 1;
        transform.localScale = localScale;
    }

    private bool CheckCooldown() => Time.time >= _lastAttack + _cooldown;

    #region VULNERABILITY MANAGEMENT
    private bool SetVulnerability(bool value) => _isVulnerable = value;

    private IEnumerator Activate()
    {
        yield return new WaitForSeconds(_waitTime);
        _isActive = true;
        AudioManager.Instance.PlayLoopedSFXAtPosition(SFXClip.B_Drone_Idle, transform.position);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Projectile") && !_isVulnerable)
        {
            collision.TryGetComponent(out Projectile projectile);
            if (projectile.Shooter.gameObject.CompareTag("Player"))
            {
                StopAllCoroutines();
                AudioManager.Instance.PlaySFXAtPosition(SFXClip.B_Drone_ProjectileHit, transform.position);
                StartCoroutine(GetVulnerable());
            }
        }
        else if (collision.gameObject.CompareTag("Player") && _isVulnerable)
        {
            StopAllCoroutines();
            AudioManager.Instance.PlaySFXAtPosition(SFXClip.B_Drone_PlayerHit, transform.position);
            Die();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Scenario") && _isVulnerable)
        {
            AudioManager.Instance.PlaySFXAtPosition(SFXClip.B_Drone_FloorHit, transform.position);
        }
    }

    private IEnumerator GetVulnerable()
    {
        _animator.SetTrigger("OnHit");
        SetVulnerability(true);
        _rb.gravityScale = 1;
        _boxCol.enabled = true;
        _boxTrigger.enabled = true;
        yield return new WaitForSeconds(_vulnerabilityTime);
        _animator.SetTrigger("OnRecover");
        _boxCol.enabled = false;
        _boxTrigger.enabled = false;
        _rb.gravityScale = 0;
        yield return new WaitForSeconds(1.75f);

        SetVulnerability(false);
        FlyAway();
    }

    public override void Die()
    {
        base.Die();
        _boxCol.enabled = false;
        _boxTrigger.enabled = false;
        _capsuleCol.enabled = false;
        _rb.bodyType = RigidbodyType2D.Static;
    }

    #endregion

    #region ATTACK METHODS

    private void ChooseAttack()
    {
        int r = Random.Range(0, 3);
        _nextAttack = r switch
        {
            0 => AttackType.Burst,
            1 => AttackType.Cone,
            2 => AttackType.Rebound,
            _ => AttackType.Burst,
        };
    }

    private Vector2 GetDirToPlayer() => (_playerPos - (Vector2)_shootingPoint.position).normalized;

    private void Attack(AttackType type)
    {
        switch (type)
        {
            case AttackType.Burst:
                StartCoroutine(ShootBurst());
                break;
            case AttackType.Cone:
                StartCoroutine(ShootCone());
                break;
            case AttackType.Rebound:
                StartCoroutine(ShootRebound());
                break;
        }
        ;
        _lastAttack = Time.time;
    }

    private IEnumerator ShootBurst()
    {
        _animator.SetTrigger("OnLinearBurst");
        yield return new WaitForSeconds(.91f);
        for (int n = 0; n < _burstAmount; n++)
        {
            Shoot(AttackType.Burst);
            yield return new WaitForSeconds(_timeBetweenShots);
        }
    }

    private IEnumerator ShootCone()
    {
        _animator.SetTrigger("OnConeShot");
        yield return new WaitForSeconds(.91f);
        for (int n = 0; n < _coneAmount; n++)
        {
            Shoot(AttackType.Cone);
            yield return new WaitForSeconds(_timeBetweenShots);
        }
    }

    private IEnumerator ShootRebound()
    {
        _animator.SetTrigger("OnReboundShot");
        yield return new WaitForSeconds(.91f);
        for (int n = 0; n < _reboundAmount; n++)
        {
            Shoot(AttackType.Rebound);
            yield return new WaitForSeconds(_timeBetweenReboundShots);
        }
    }

    private void Shoot(AttackType type)
    {
        ObjectPool<Projectile> pool = type switch
        {
            AttackType.Burst => _burstPool,
            AttackType.Cone => _conePool,
            AttackType.Rebound => _ricochetPool,
            _ => _burstPool
        };

        Projectile projectile = pool.Get();

        projectile.transform.SetPositionAndRotation(
            _shootingPoint.position,
            Quaternion.identity
        );

        projectile.OnRequestDespawn -= HandleProjectileDespawn;
        projectile.OnRequestDespawn += HandleProjectileDespawn;

        projectile.Initialize(GetDirToPlayer(), transform);
    }

    private void HandleProjectileDespawn(Projectile projectile)
    {
        projectile.OnRequestDespawn -= HandleProjectileDespawn;

        if (_conePool != null && _conePool.AvailableCount >= 0)
            _conePool.Release(projectile);
        else if (_ricochetPool != null)
            _ricochetPool.Release(projectile);
        else
            _burstPool.Release(projectile);
    }

    #endregion

    #region EVASION && FLYING METHODS
    private bool CheckDisToPlayer()
    {
        _playerPos = _player.transform.position;

        float disToPlayer = Vector2.Distance(_playerPos, transform.position);
        return disToPlayer < _minDistance;
    }

    private void FlyAway()
    {
        _flyingAway = true;
        FindFurthestPoint();
        StartCoroutine(FlyTowards(_targetWaypoint.position));
    }

    private void FindFurthestPoint()
    {
        float disToActual = 0;
        float disToFurthest = 0;
        Transform furthestWP = Waypoints[0];
        foreach (Transform t in Waypoints)
        {
            disToActual = Vector2.Distance(t.position, _playerPos);

            if (disToFurthest == 0 || disToActual > disToFurthest)
            {
                disToFurthest = disToActual;
                furthestWP = t;
            }
        }
        if (furthestWP != null) _targetWaypoint = furthestWP;
    }

    private IEnumerator FlyTowards(Vector2 targetPos)
    {
        AudioManager.Instance.StopSFX(SFXClip.B_Drone_Idle);
        AudioManager.Instance.PlayLoopedSFXAtPosition(SFXClip.B_Drone_FlyAway, transform.position);
        while (_flyingAway)
        {
            Vector2 dirToFly = (targetPos - (Vector2)transform.position).normalized;
            _rb.linearVelocity = dirToFly * _speed;

            float disToTarget = Vector2.Distance(transform.position, targetPos);
            if (disToTarget <= .5f)
            {
                _rb.linearVelocity = Vector2.zero;
                _flyingAway = false;
            }
            yield return null;
        }
        AudioManager.Instance.StopSFX(SFXClip.B_Drone_FlyAway);
        AudioManager.Instance.PlayLoopedSFXAtPosition(SFXClip.B_Drone_Idle, transform.position);
    }

    #endregion
}