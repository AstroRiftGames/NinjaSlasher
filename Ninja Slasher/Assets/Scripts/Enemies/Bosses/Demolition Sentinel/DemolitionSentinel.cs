using System;
using System.Collections;
using UnityEngine;

public enum SentinelAttacks
{
    None,
    Heavy,
    Double,
    Sweep,
}

public class DemolitionSentinel : BossEnemy
{
    [SerializeField] private SentinelCore _core;
    public SentinelCore Core => _core;

    public void SetIsVulnerable(bool value)
    {
        IsVulnerable = value;
        _animator.SetBool("isVulnerable", value);
        if (value)
        {
            _vulnerableEntryTime = Time.time;
            _core.enabled = true; // Agregado porque el comportamiento que estaba en SentinelVulnerableState fue portado
            StartCoroutine(SentinelAudio.VulnerableFeedbackSequence(3f));
        }
        else
        {
            _core.enabled = false;
            SentinelAudio.ExitVulnerable();
        }
    }
    private float _vulnerableEntryTime;
    [SerializeField] float _vulnerableTime;

    public void SetJustAttacked(bool value) => _justAttacked = value;
    private bool _justAttacked;

    private SentinelAttacks _nextAttack = SentinelAttacks.None;
    private bool _isAttacking;
    public void SetIsAttacking(bool value) => _isAttacking = value;

    public Vector3 TargetDir => _targetDir;
    Vector3 _targetDir;
    public bool IsRightBallTurn => _isRightBallTurn;
    private bool _isRightBallTurn = true;
    public DemolitionBall CurrentBall => _currentBall;
    DemolitionBall _currentBall;

    public float Cooldown => _cooldown;
    [SerializeField] private float _cooldown;
    [SerializeField] private DemolitionBall[] _balls;
    public DemolitionBall[] Balls => _balls;
    [SerializeField] Chain _rightChain;
    [SerializeField] Chain _leftChain;

    [Header("Double Attack")]
    public float TimeBetweenAttacks => _timeBetweenAttacks;
    [SerializeField] float _timeBetweenAttacks;
    public int AmountOfAttacks => _amountOfAttacks;
    [SerializeField] int _amountOfAttacks;
    [HideInInspector] public bool _isDoubleAttacking;

    [Header("Heavy Attack")]
    public float ChargingTime => _chargingTime;
    [SerializeField] float _chargingTime;
    [HideInInspector] public bool _isHeavyAttacking;

    [Header("SweepAttack")]
    public float SweepDuration => _sweepDuration;
    [SerializeField] float _sweepDuration;
    [HideInInspector] public bool _isSweepAttacking;

    private SentinelAudioContext _sentinelAudio;
    public SentinelAudioContext SentinelAudio => _sentinelAudio;

    private float _enterIdleTime;

    #region MAGIC METHODS
    public void Start()
    {
        _currentBall = _balls[0];
        StartCoroutine(Activate());
    }

    public IEnumerator Activate()
    {
        _sentinelAudio.PlayIntro();
        SetTargetDirection(Vector2.down);
        AimArm(_balls[0].PivotPoint);
        AimArm(_balls[1].PivotPoint);
        yield return new WaitForSeconds(_waitTime);
        ChooseAttack();
    }

    public override void CustomUpdate()
    {
        _animator.SetBool("hasRightArm", _rightChain.IsActive);
        _animator.SetBool("hasLeftArm", _leftChain.IsActive);
        
        if (IsVulnerable)
        {
            CheckVulnerableTime();
        }
        else
        {
            ManageStateBehavior();
        }
    }

    #endregion

    #region INTERNAL STATE LOGIC
    private void ManageStateBehavior()
    {
        if (_justAttacked || _isAttacking)
        {
            if (!_isAttacking)
            {
                // In Idle
                if (Time.time >= _enterIdleTime + Cooldown)
                {
                    SetJustAttacked(false);
                    SentinelAudio.StopIdle();
                }
            }
            return;
        }

        // Trigger Attack
        switch (_nextAttack)
        {
            case SentinelAttacks.Heavy:
                _isHeavyAttacking = true;
                SetIsAttacking(true);
                ChooseAttack();
                StartCoroutine(HeavyAttack());
                break;
            case SentinelAttacks.Sweep:
                _isSweepAttacking = true;
                SetIsAttacking(true);
                ChooseAttack();
                StartCoroutine(SweepAttack());
                break;
            case SentinelAttacks.Double:
                _isDoubleAttacking = true;
                SetIsAttacking(true);
                ChooseAttack();
                StartCoroutine(DoubleAttack());
                break;
            case SentinelAttacks.None:
            default:
                break;
        }
    }

    public void EnterIdle()
    {
        _enterIdleTime = Time.time;
        SentinelAudio.StartIdle();
        _isHeavyAttacking = false;
        _isDoubleAttacking = false;
        _isSweepAttacking = false;
        SetIsAttacking(false);
        SetJustAttacked(true);
    }
    #endregion

    #region ATTACK COROUTINES
    private IEnumerator HeavyAttack()
    {
        Debug.Log("Heavy Attack");
        _animator.SetTrigger("onHeavy");

        SetTargetDirection();
        AimArm(Balls[(Balls.Length >= 2) ? (IsRightBallTurn ? 0 : 1) : 0].PivotPoint);

        yield return new WaitForSeconds(.25f);

        CurrentBall.HeavyThrow();

        while (CurrentBall.IsOut)
        {
            yield return null;
        }

        SetTargetDirection(Vector2.down);
        AimArm(CurrentBall.PivotPoint);
        if (Balls.Length >= 2) ChangeBall();

        EnterIdle();
    }

    private IEnumerator SweepAttack()
    {
        _animator.SetTrigger("onSweep");
        SentinelAudio.PlaySweep();
        SetTargetDirection(Vector2.down);

        yield return new WaitForSeconds(1.5f);

        EnterIdle();
    }

    private IEnumerator DoubleAttack()
    {
        Debug.Log("Double Attack");
        _animator.SetTrigger("onDouble");
        for (int i = 0; i < AmountOfAttacks; i++)
        {
            SetTargetDirection();
            AimArm(Balls[IsRightBallTurn ? 0 : 1].PivotPoint);

            yield return new WaitForSeconds(.25f);

            SentinelAudio.PlayDoubleAttack(IsRightBallTurn);
            CurrentBall.Throw();

            while (CurrentBall.IsOut)
            {
                yield return null;
            }

            SetTargetDirection(Vector2.down);
            AimArm(CurrentBall.PivotPoint);
            if (Balls.Length >= 2) ChangeBall();

            yield return new WaitForSeconds(1f);
        }
        
        EnterIdle();
    }
    #endregion

    #region RESOURCES

    protected override void InitializeAudioContext()
    {
        _sentinelAudio = GetComponent<SentinelAudioContext>();
        _sentinelAudio?.Initialize(_data.AudioSet);
    }

    void CheckVulnerableTime()
    {
        if (Time.time >= _vulnerableEntryTime + _vulnerableTime)
        {
            StartCoroutine(Recover());
        }
    }

    private IEnumerator Recover()
    {
        _animator.SetTrigger("onRecover");
        SetIsVulnerable(false);
        _balls = new DemolitionBall[2];

        yield return new WaitForSeconds(1f);

        _rightChain.RepairChain();
        _rightChain.RecoverBall();
        
        yield return new WaitForSeconds(1f);

        _leftChain.RepairChain();
        _leftChain.RecoverBall();

        yield return new WaitForSeconds(1f);

        SetJustAttacked(false);
        SetIsAttacking(false);

        yield return new WaitForSeconds(1f);

        SetTargetDirection(Vector2.down);
        AimArm(_balls[0].PivotPoint);
        AimArm(_balls[1].PivotPoint);

        yield return new WaitForSeconds(.5f);
        ChooseAttack();
        _currentBall = _balls[0];
        _isRightBallTurn = true;
    }

    public void SetTargetDirection()
    {
        Vector2 dir = _player.position - _currentBall.PivotPoint.position;
        _targetDir = dir.normalized;
    }

    public void SetTargetDirection(Vector2 dir)
    {
        _targetDir = dir.normalized;
        AimArm(CurrentBall.PivotPoint);
    }

    public void AimArm(Transform arm)
    {
        float angle = (Mathf.Atan2(_targetDir.y, _targetDir.x) * Mathf.Rad2Deg);
        Debug.Log($"Aiming {arm.name} at {angle}°");
        arm.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void ChangeBall()
    {
        _isRightBallTurn = !_isRightBallTurn;
        _currentBall = _balls[_isRightBallTurn ? 0 : 1];
        _animator.SetBool("isRightBallTurn", _isRightBallTurn);
    }

    public void ReturnOneBall(DemolitionBall ball)
    {
        ball.StartCoroutine(ball.Return(_timeBetweenAttacks));
    }

    public void RemoveBall(DemolitionBall ball)
    {
        DemolitionBall[] list = new DemolitionBall[_balls.Length - 1];
        foreach(DemolitionBall b in _balls)
        {   
            if (b != null && b != ball)
            {
                list[0] = b;
            }
        }
        _balls = list;  
    }

    public void AddBall(DemolitionBall ball, bool isRightBall)
    {
        if (isRightBall)
        {
            _balls[0] = ball;
        }
        else
        {
            _balls[1] = ball;
        }
    }

    public void ChooseAttack()
    {
        float r = UnityEngine.Random.Range(0f, 1f);
        switch (r)
        {
            case >= .67f:
                if (_balls.Length >= 2)
                {
                    _nextAttack = SentinelAttacks.Double;
                }
                else
                {
                    _nextAttack = SentinelAttacks.Heavy;
                }
                break;
            case >= .34f and < .67f:
                _nextAttack = SentinelAttacks.Sweep;
                break;
            case < .34f:
                _nextAttack = SentinelAttacks.Heavy;
                break;
            default:
                _nextAttack = SentinelAttacks.Double;
                break;
        }
    }

    public void StopAttack()
    {
        StopAllCoroutines();

        _isDoubleAttacking = false;
        _isHeavyAttacking = false;
        _isSweepAttacking = false;
        SetIsAttacking(false);
        SetJustAttacked(false);
        SentinelAudio.StopIdle();

        if (_balls.Length >= 1)
        {
            _currentBall = _balls[0];
            _isRightBallTurn = !_isRightBallTurn;
            SetTargetDirection(Vector2.down);
            AimArm(_currentBall.PivotPoint);
        }
        else
        {
            _nextAttack = SentinelAttacks.None;
            SetIsVulnerable(true);
        }
        ChooseAttack();
    }

    #endregion
}
