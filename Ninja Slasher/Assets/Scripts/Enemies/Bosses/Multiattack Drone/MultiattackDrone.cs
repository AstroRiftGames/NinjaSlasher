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

    [Header("Bullet prefabs")]
    [SerializeField] GameObject _riccochetBullet;
    [SerializeField] GameObject _burstBullet;
    [SerializeField] GameObject _coneShots;

    [Header("Attacking Parameters")]
    [SerializeField] float _cooldown;
    [SerializeField] Transform _shootingPoint;
    [Space]
    [SerializeField] int _burstAmount;
    [SerializeField] int _coneAmount;
    [SerializeField] float _timeBetweenShots;


    private bool _flyingAway;

    private void Update()
    {
        if(CheckDisToPlayer())
        {
            if (_flyingAway) StopAllCoroutines();
            FlyAway();
        }
    }

    #region ATTACK METHODS

    private void Attack(AttackType type)
    {
        Vector2 dirToPlayer = (_playerPos - (Vector2)_shootingPoint.position).normalized;
        switch(type)
        {
            case AttackType.Burst:
                StartCoroutine(ShootBurst(dirToPlayer));
                break;
            case AttackType.Cone:
                StartCoroutine(ShootCone());
                break;
            case AttackType.Ricochet:
                Shoot(AttackType.Ricochet, dirToPlayer);
                Debug.Log("Ricochet Attack");
                break;
        };
    }

    private IEnumerator ShootBurst(Vector2 dir)
    {
        for (int n = 0; n < _burstAmount; n++)
        {
            Shoot(AttackType.Burst, dir);
            yield return new WaitForSeconds(_timeBetweenShots);
        }
    }

    private IEnumerator ShootCone()
    {
        for (int n = 0; n < _coneAmount; n++)
        {
            Shoot(AttackType.Cone, Vector2.zero);
            yield return new WaitForSeconds(_timeBetweenShots);
        }
    }

    private void Shoot(AttackType type, Vector2 dirToShoot)
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
            GameObject cone = Instantiate(prefab, _shootingPoint.position, Quaternion.identity);
            for(int n = 0; n < cone.transform.childCount;n++)
            {
                cone.transform.GetChild(n).TryGetComponent(out Projectile newProjectile);
                newProjectile.Initialize(transform);
            }
        }
        else
        {
            Instantiate(prefab, _shootingPoint.position, Quaternion.identity).TryGetComponent(out Projectile newProjectile);
            newProjectile.Initialize(dirToShoot, transform);
            Debug.Log("Bullet Shot");
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
            Debug.Log($"Distance to {t} is {disToActual}");

            if(disToFurthest == 0 || disToActual > disToFurthest)
            {
                disToFurthest = disToActual;
                furthestWP = t;

                Debug.Log($"TargetWP updateded ({furthestWP})");
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

            Debug.Log("Flying away");
            yield return null;
        }
    }

    #endregion
}