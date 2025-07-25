using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject.SpaceFighter;

public enum AttackType
{
    Burst,
    Cone,
    Ricochet,
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

    [Header("Bullet Prefabs")]
    [SerializeField] GameObject _riccochetBullet;
    [SerializeField] GameObject _burstBullet;
    [SerializeField] GameObject _coneShots;

    [Header("Attacking Parameters")]
    [SerializeField] float _cooldown;
    private float _lastAttack;
    [SerializeField] Transform _shootingPoint;
    [Space]
    [SerializeField] int _burstAmount;
    [SerializeField] int _coneAmount;
    [SerializeField] float _timeBetweenShots;
    private AttackType _nextAttack;

    [Header("Vulnerability Parameters")]
    [SerializeField] float _vulnerabilityTime;
    private bool _isVulnerable;


    private void Update()
    {
        if(!_isVulnerable)
        {
            if(CheckDisToPlayer())
            {
                if (_flyingAway) StopAllCoroutines();
                FlyAway();
            }

            if(!_flyingAway && CheckCooldown())
            {
                ChooseAttack();
                Attack(_nextAttack);
            }
        }
    }

    private bool CheckCooldown() => Time.time >= _lastAttack + _cooldown;

    #region VULNERABILITY MANAGEMENT
    private bool SetVulnerability(bool value) => _isVulnerable = value;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.layer == 8)
        {
            collision.TryGetComponent(out Projectile projectile);
            Debug.Log(projectile);
            if(projectile.Shooter.gameObject.CompareTag("Player"))
            {
                StartCoroutine(GetVulnerable());
            }
        }
        else if(collision.gameObject.CompareTag("Player") && _isVulnerable)
        {
            Die();
        }
    }

    private IEnumerator GetVulnerable()
    {
        SetVulnerability(true);
        Debug.Log("Is now vulnerable");
        yield return new WaitForSeconds(_vulnerabilityTime);
        SetVulnerability(false);
        Debug.Log("Is no longer vulnerable");
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
            2 => AttackType.Ricochet,
            _ => AttackType.Burst,
        };
    }

    private Vector2 GetDirToPlayer() => (_playerPos - (Vector2)_shootingPoint.position).normalized;

    private void Attack(AttackType type)
    {
        switch(type)
        {
            case AttackType.Burst:
                StartCoroutine(ShootBurst());
                break;
            case AttackType.Cone:
                StartCoroutine(ShootCone());
                break;
            case AttackType.Ricochet:
                Shoot(AttackType.Ricochet);
                break;
        };
        _lastAttack = Time.time;
    }

    private IEnumerator ShootBurst()
    {
        for (int n = 0; n < _burstAmount; n++)
        {
            Shoot(AttackType.Burst);
            yield return new WaitForSeconds(_timeBetweenShots);
        }
    }

    private IEnumerator ShootCone()
    {
        for (int n = 0; n < _coneAmount; n++)
        {
            Shoot(AttackType.Cone);
            yield return new WaitForSeconds(_timeBetweenShots);
        }
    }

    private void Shoot(AttackType type)
    {
        GameObject prefab = type switch
        {
            AttackType.Burst => _burstBullet,
            AttackType.Cone => _coneShots,
            AttackType.Ricochet => _riccochetBullet,
            _ => _burstBullet,
        };

        if(type == AttackType.Cone)
        {
            GameObject cone = Instantiate(prefab, _shootingPoint.position, _shootingPoint.parent.rotation);
            for(int n = 0; n < cone.transform.childCount;n++)
            {
                cone.transform.GetChild(n).TryGetComponent(out Projectile newProjectile);
                newProjectile.Initialize(transform);
            }
        }
        else
        {
            Instantiate(prefab, _shootingPoint.position, Quaternion.identity).TryGetComponent(out Projectile newProjectile);
            newProjectile.Initialize(GetDirToPlayer(), transform);
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
        foreach(Transform t in Waypoints)
        {
            disToActual = Vector2.Distance(t.position, _playerPos);

            if(disToFurthest == 0 || disToActual > disToFurthest)
            {
                disToFurthest = disToActual;
                furthestWP = t;
            }
        }
        if(furthestWP != null) _targetWaypoint = furthestWP;
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