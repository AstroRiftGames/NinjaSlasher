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
    [SerializeField] GameObject _coneBullet;
    [SerializeField] GameObject _riccochetBullet;
    [SerializeField] GameObject _burstBullet;

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
    private GenericPool<Projectile> _conePool;
    private GenericPool<Projectile> _ricochetPool;
    private GenericPool<Projectile> _burstPool;

    [Header("Vulnerability Parameters")]
    [SerializeField] float _vulnerabilityTime;
    private bool _isVulnerable;

    private bool _isActive = false;
    [SerializeField] private float _waitTime;

    public override void Awake()
    {
        base.Awake();
        _conePool = new GenericPool<Projectile>(_coneBullet, _coneAmount * 2, transform);
        _ricochetPool = new GenericPool<Projectile>(_riccochetBullet, _reboundAmount * 2, transform);
        _burstPool = new GenericPool<Projectile>(_burstBullet, _burstAmount * 2, transform);
    }

    public override void Start()
    {
        base.Start();
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
        GameObject prefab = type switch
        {
            AttackType.Burst => _burstBullet,
            AttackType.Cone => _coneBullet,
            AttackType.Rebound => _riccochetBullet,
            _ => _burstBullet,
        };

        Transform t = null;

        if (type == AttackType.Cone)
        {
            Projectile cone = _conePool.Get();
            t = cone.transform;

            Vector2 dir = GetDirToPlayer();
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            cone.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
            cone.transform.parent = null;

            for (int n = 0; n < cone.transform.childCount; n++)
            {
                cone.transform.GetChild(n).TryGetComponent(out Projectile newProjectile);
                newProjectile.Initialize(transform, _conePool);
            }
        }
        else
        {
            GenericPool<Projectile> pool = type == AttackType.Rebound ? _ricochetPool : _burstPool;
            Projectile projectile = pool.Get();
            t = projectile.transform;
            projectile.transform.SetPositionAndRotation(_shootingPoint.position, Quaternion.identity);
            projectile.Initialize(GetDirToPlayer(), transform, pool);
            projectile.transform.parent = null;
        }
        AudioManager.Instance.PlaySFXAtPosition(type == AttackType.Burst ? SFXClip.B_Drone_BurstAttack: type == AttackType.Cone ? SFXClip.B_Drone_ConeAttack: SFXClip.B_Drone_ReboundAttack, t.position);
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