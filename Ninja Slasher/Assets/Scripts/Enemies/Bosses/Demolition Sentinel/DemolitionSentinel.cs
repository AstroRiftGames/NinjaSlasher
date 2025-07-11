using System;
using System.Collections;
using UnityEngine;

public class DemolitionSentinel : BossEnemy
{
    Vector3 _targetDir;
    private float _lastAttack;
    private bool _isRightBallTurn;
    DemolitionBall _currentBall;

    [SerializeField] private float _cooldown;
    [SerializeField] private DemolitionBall[] _balls;

    [Header("Double Attack")]
    [SerializeField] float _timeBetweenAttacks;
    [SerializeField] int _amountOfAttacks;

    [Header("Area Attack")]
    [SerializeField] float _chargingTime;


    public override void Awake()
    {
        base.Awake();
        _currentBall = _balls[0];
    }

    private void Update()
    {
        if (Time.time >= _lastAttack + _cooldown)
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                _lastAttack = Time.time;
                SetTarget();
                StartCoroutine(DoubleAttack());

            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                _lastAttack = Time.time;
                SetTarget();
                StartCoroutine(HeavyAttack());
            }
        }
    }

    private void SetTarget()
    {
        Vector2 dir = _player.position - _currentBall.Anchor.position;
        _targetDir = dir.normalized;
    }

    private void ChangeBall()
    {
        _isRightBallTurn = !_isRightBallTurn;
        _currentBall = _balls[_isRightBallTurn ? 0 : 1];
    }

    private IEnumerator DoubleAttack()
    {
        for(int i = 0; i < _amountOfAttacks; i++)
        {
            _currentBall.Throw(_targetDir);
            ChangeBall();
            yield return new WaitForSeconds(_timeBetweenAttacks);
        }
    }

    private IEnumerator HeavyAttack()
    {
        yield return new WaitForSeconds(_chargingTime);
        _currentBall.HeavyThrow(_targetDir);
        ChangeBall();
    }
}