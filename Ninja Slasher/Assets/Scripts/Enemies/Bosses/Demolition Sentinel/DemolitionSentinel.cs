using System;
using System.Collections;
using UnityEngine;


public enum SentinelStates
{
    Idle,
    DoubleAttack,
    HeavyAttack,
    SweepAttack,
}

public class DemolitionSentinel : BossEnemy
{ 
    FSM<SentinelStates> _fsm;
    ITreeNode _root;

    public Vector3 TargetDir => _targetDir;
    Vector3 _targetDir;
    public bool IsRightBallTurn => _isRightBallTurn;
    private bool _isRightBallTurn;
    public DemolitionBall CurrentBall => _currentBall;
    DemolitionBall _currentBall;

    [SerializeField] private float _cooldown;
    [SerializeField] private DemolitionBall[] _balls;

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

    #region MAGIC METHODS
    public override void Start()
    {
        base.Start();
        InitializeFSM();
        InitializeTree();
        _currentBall = _balls[0];
    }

    private void Update()
    {
        _fsm.OnUpdate();
        _root.Execute();
    }

    #endregion

    #region RESOURCES

    public void SetTargetDirection()
    {
        Vector2 dir = _player.position - _currentBall.Anchor.position;
        _targetDir = dir.normalized;
    }

    public void ChangeBall()
    {
        _isRightBallTurn = !_isRightBallTurn;
        _currentBall = _balls[_isRightBallTurn ? 0 : 1];
    }

#endregion

    #region FSM
    public void InitializeFSM()
    {
        var idle = new SentinelIdleState<SentinelStates>(this);
        var doubleAttack = new SentinelDoubleAttackState<SentinelStates>(this);
        var heavyAttack = new SentinelHeavyAttackState<SentinelStates>(this);
        var sweepAttack = new SentinelSweepAttackState<SentinelStates>(this);

        _fsm = new FSM<SentinelStates>(idle);

        idle.AddTransition(SentinelStates.HeavyAttack, heavyAttack);
        idle.AddTransition(SentinelStates.SweepAttack, sweepAttack);
        idle.AddTransition(SentinelStates.DoubleAttack, doubleAttack);
        heavyAttack.AddTransition(SentinelStates.Idle, idle);
        sweepAttack.AddTransition(SentinelStates.Idle, idle);
        doubleAttack.AddTransition(SentinelStates.Idle, idle);
    }

    public void InitializeTree()
    {
        ITreeNode idle = new ActionNode(() => _fsm.Transition(SentinelStates.Idle));
        ITreeNode heavyAttack = new ActionNode(() => _fsm.Transition(SentinelStates.HeavyAttack));
        ITreeNode sweepAttack = new ActionNode(() => _fsm.Transition(SentinelStates.SweepAttack));
        ITreeNode doubleAttack = new ActionNode(() => _fsm.Transition(SentinelStates.DoubleAttack));

        ITreeNode qheavyAttack = new QuestionNode(QHeavyAttack, heavyAttack, idle);
        ITreeNode qSweepAttack = new QuestionNode(QSweepAttack, sweepAttack, qheavyAttack);
        ITreeNode qDoubleAttack = new QuestionNode(QDoubleAttack, doubleAttack, qSweepAttack);

        _root = qDoubleAttack;
    }

    #region QUESTIONS
    bool QDoubleAttack() => _isDoubleAttacking || (!(_isSweepAttacking && _isHeavyAttacking) && Input.GetKeyDown(KeyCode.G));
    bool QHeavyAttack() => _isHeavyAttacking   || (!(_isSweepAttacking && _isDoubleAttacking) && Input.GetKeyDown(KeyCode.H));
    bool QSweepAttack() => _isSweepAttacking   || (!(_isHeavyAttacking && _isDoubleAttacking) && Input.GetKeyDown(KeyCode.J));

    #endregion

    #endregion
}