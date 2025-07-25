using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject.SpaceFighter;

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
    [SerializeField] GameObject _regularBullet;

    [Header("Attacking Parameters")]
    [SerializeField] float _cooldown;


    private bool _flyingAway;

    private void Update()
    {
        if(CheckDisToPlayer())
        {
            if (_flyingAway) StopAllCoroutines();
            FlyAway();
        }
    }

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
}
