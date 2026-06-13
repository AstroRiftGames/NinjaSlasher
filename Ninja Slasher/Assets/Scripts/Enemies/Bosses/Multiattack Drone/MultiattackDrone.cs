using System;
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
    [SerializeField][Range(0, 100)] float _coneParryableChance;
    [SerializeField] int _reboundAmount;
    [SerializeField][Range(0, 100)] float _reboundParryableChance;
    private AttackType _nextAttack;
    private ObjectPool<Projectile> _conePool;
    private ObjectPool<Projectile> _ricochetPool;
    private ObjectPool<Projectile> _burstPool;

    [Header("Vulnerability Parameters")]
    [SerializeField] float _vulnerabilityTime;
    [SerializeField] DroneCore _core;

    private bool _isActive = false;

    [SerializeField] private ShootingPointContainer _shootingPointContainer;
    [SerializeField] private MultiAttackDroneAudioContext _droneAudioContext;
    [SerializeField] MultiAttackDroneAudioSet _audioSet;

    protected override void Awake()
    {
        base.Awake();
        _conePool = new ObjectPool<Projectile>(_coneBullet, _coneAmount * 2, transform);
        _ricochetPool = new ObjectPool<Projectile>(_riccochetBullet, _reboundAmount * 2, transform);
        _burstPool = new ObjectPool<Projectile>(_burstBullet, _burstAmount * 2, transform);
    }

    public void Start()
    {
        if (_shootingPointContainer != null && _player != null)
        {
            _shootingPointContainer.SetPlayer(_player);
        }

        StartCoroutine(Activate());
        InitializeAudioContext();
    }

    public override void CustomUpdate()
    {
        if (_isActive && !IsVulnerable)
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
        localScale.x = playerIsOnRight ? -.5f : .5f;
        transform.localScale = localScale;
    }

    protected override void InitializeAudioContext()
    {
        if (_droneAudioContext != null)
            _droneAudioContext.Initialize(_audioSet);
    }

    private bool CheckCooldown() => Time.time >= _lastAttack + _cooldown;

    #region VULNERABILITY MANAGEMENT
    private void SetVulnerability(bool value)
    {
        IsVulnerable = value;
        if (_core != null) _core.enabled = value;
    }

    private IEnumerator Activate()
    {
        yield return new WaitForSeconds(_waitTime);
        _isActive = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Projectile") && !IsVulnerable)
        {
            collision.TryGetComponent(out Projectile projectile);
            if (projectile != null && projectile.Shooter != null && projectile.Shooter.gameObject.CompareTag("Player"))
            {
                StopAllCoroutines();
                StartCoroutine(GetVulnerable());
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsVulnerable) return;
        if(collision.gameObject.CompareTag("Scenario"))
        {
            AudioService.Instance.PlaySFXAtPosition(_droneAudioContext.Audio.hitByGround, transform.position);
        }
    }

    private IEnumerator GetVulnerable()
    {
        _animator.SetTrigger("OnHit");
        SetVulnerability(true);
        _rb.gravityScale = 1;
        _boxCol.enabled = true;
        yield return new WaitForSeconds(_vulnerabilityTime);
        _animator.SetTrigger("OnRecover");
        _boxCol.enabled = false;
        _rb.gravityScale = 0;
        yield return new WaitForSeconds(1.75f);

        SetVulnerability(false);
        FlyAway();
    }

    public override void Die()
    {
        base.Die();
        _boxCol.enabled = false;
        _capsuleCol.enabled = false;
        _rb.bodyType = RigidbodyType2D.Static;
    }

    #endregion

    #region ATTACK METHODS

    private void ChooseAttack()
    {
        int r = UnityEngine.Random.Range(0, 3);
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

    private int SelectParriableProjectile(int amount)
    {
        return UnityEngine.Random.Range(0, amount);
    }

    private IEnumerator ShootBurst()
    {
        _animator.SetTrigger("OnLinearBurst");
        yield return new WaitForSeconds(.916f);
        for (int n = 0; n < _burstAmount; n++)
        {
            Shoot(AttackType.Burst, GetDirToPlayer());
            yield return new WaitForSeconds(.165f);
        }
    }

    private IEnumerator ShootCone()
    {
        _animator.SetTrigger("OnConeShot");
        yield return new WaitForSeconds(.916f);
        Vector2 direction = GetDirToPlayer();
        for (int m = 0; m < _coneAmount; m++)
        {
            for (int n = 0; n < 3; n++)
            {
                float offsetAngle = n < 1 ? -15 : n == 1 ? 0 : 15;
                direction = Quaternion.Euler(0, 0, offsetAngle) * direction;

                Shoot(AttackType.Cone, direction);
            }
            yield return new WaitForSeconds(.5f);
        }
    }

    private IEnumerator ShootRebound()
    {
        _animator.SetTrigger("OnReboundShot");
        yield return new WaitForSeconds(.916f);
        int parriableProj = SelectParriableProjectile(_reboundAmount);
        
        for (int n = 0; n < _reboundAmount; n++)
        {
            Shoot(AttackType.Rebound, GetDirToPlayer());
            yield return new WaitForSeconds(.5f);
        }
    }
    
    private void Shoot(AttackType type, Vector2 direction)
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

        bool shouldBeParryable = type == AttackType.Cone || type == AttackType.Rebound;
        float parryableChance = shouldBeParryable ? _reboundParryableChance : 0;
        bool isParryable = shouldBeParryable && UnityEngine.Random.Range(0, 100) < parryableChance;

        projectile.SetParryOverrideProvider(PowerUpManager.Instance);
        projectile.Initialize(direction, transform, isParryable);
    }

    private void HandleProjectileDespawn(Projectile projectile)
    {
        projectile.OnRequestDespawn -= HandleProjectileDespawn;


        if (projectile.name.Contains("Cone"))
        {
            if (_conePool != null)
                _conePool.Release(projectile);
        }
        else if (projectile.name.Contains("Rebound"))
        {
            if (_ricochetPool != null)
                _ricochetPool.Release(projectile);
        }
        else if (projectile.name.Contains("Burst"))
        {
            if (_burstPool != null)
                _burstPool.Release(projectile);
        }
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
    }

    #endregion
}
