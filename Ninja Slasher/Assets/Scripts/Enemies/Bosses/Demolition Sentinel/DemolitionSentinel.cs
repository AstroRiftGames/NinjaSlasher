using System;
using System.Linq;
using UnityEngine;


public enum SentinelStates
{
    Idle,
    DoubleAttack,
    HeavyAttack,
    SweepAttack,
    Vulenrable,
}

public enum SentinelAttacks
{
    Heavy,
    Double,
    Sweep,
}
public class DemolitionSentinel : BossEnemy
{ 
    FSM<SentinelStates> _fsm;
    ITreeNode _root;

    [SerializeField] private SentinelCore _core;
    public SentinelCore Core => _core;
    public void SetVulnerability(bool value) => _isVulnerable = value;
    private bool _isVulnerable;
    public void SetJustAttacked(bool value) => _justAttacked = value;
    private bool _justAttacked;

    private SentinelAttacks _nextAttack;
    private bool _isAttacking;
    public void SetIsAttacking(bool value) => _isAttacking = value;

    public Vector3 TargetDir => _targetDir;
    Vector3 _targetDir;
    public bool IsRightBallTurn => _isRightBallTurn;
    private bool _isRightBallTurn;
    public DemolitionBall CurrentBall => _currentBall;
    DemolitionBall _currentBall;

    public float Cooldown => _cooldown;
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

        ChooseAttack();
    }

    private void Update()
    {
        _fsm.OnUpdate();
        _root.Execute();

        Debug.Log(_fsm.CurrentState);
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
    
    public void RemoveBall(DemolitionBall ball)
    {
        DemolitionBall[] list = new DemolitionBall[_balls.Length - 1];
        foreach(DemolitionBall b in _balls)
        {   
            if(b != null && b != ball)
            {
                list.Append(b);
            }
        }
        _balls = list;
    }

    public void ChooseAttack()
    {
        float r = UnityEngine.Random.Range(0f, 1f);

        _nextAttack = r switch
        {
            >= .67f => SentinelAttacks.Double,
            >= .34f and < .67f => SentinelAttacks.Sweep,
            < .34f => SentinelAttacks.Heavy,
            _ => SentinelAttacks.Double,
        };
    }

#endregion

    #region FSM && DECISION TREE
    public void InitializeFSM()
    {
        var idle = new SentinelIdleState<SentinelStates>(this);
        var doubleAttack = new SentinelDoubleAttackState<SentinelStates>(this);
        var heavyAttack = new SentinelHeavyAttackState<SentinelStates>(this);
        var sweepAttack = new SentinelSweepAttackState<SentinelStates>(this);
        var vulnerable = new SentinelVulnerableState<SentinelStates>(this);

        _fsm = new FSM<SentinelStates>(idle);

        idle.AddTransition(SentinelStates.HeavyAttack, heavyAttack);
        idle.AddTransition(SentinelStates.SweepAttack, sweepAttack);
        idle.AddTransition(SentinelStates.DoubleAttack, doubleAttack);
        idle.AddTransition(SentinelStates.Vulenrable, vulnerable);
        heavyAttack.AddTransition(SentinelStates.Idle, idle);
        heavyAttack.AddTransition(SentinelStates.Vulenrable, vulnerable);
        sweepAttack.AddTransition(SentinelStates.Idle, idle);
        sweepAttack.AddTransition(SentinelStates.Vulenrable, vulnerable);
        doubleAttack.AddTransition(SentinelStates.Idle, idle);
        doubleAttack.AddTransition(SentinelStates.Vulenrable, vulnerable);
    }

    public void InitializeTree()
    {
        ITreeNode idle = new ActionNode(() => _fsm.Transition(SentinelStates.Idle));
        ITreeNode heavyAttack = new ActionNode(() => _fsm.Transition(SentinelStates.HeavyAttack));
        ITreeNode sweepAttack = new ActionNode(() => _fsm.Transition(SentinelStates.SweepAttack));
        ITreeNode doubleAttack = new ActionNode(() => _fsm.Transition(SentinelStates.DoubleAttack));
        ITreeNode vulnerable = new ActionNode(() => _fsm.Transition(SentinelStates.Vulenrable));

        ITreeNode qheavyAttack = new QuestionNode(QHeavyAttack, heavyAttack, idle);
        ITreeNode qSweepAttack = new QuestionNode(QSweepAttack, sweepAttack, qheavyAttack);
        ITreeNode qDoubleAttack = new QuestionNode(QDoubleAttack, doubleAttack, qSweepAttack);
        ITreeNode qVulnerable= new QuestionNode(QVulnerable, vulnerable, qDoubleAttack);

        _root = qVulnerable;
    }

    #region QUESTIONS
    bool QDoubleAttack() => !_justAttacked && (_isDoubleAttacking || (!_isAttacking && _nextAttack == SentinelAttacks.Double));
    bool QHeavyAttack() => !_justAttacked && (_isHeavyAttacking   || (!_isAttacking && _nextAttack == SentinelAttacks.Heavy));
    bool QSweepAttack() => !_justAttacked && (_isSweepAttacking   || (!_isAttacking && _nextAttack == SentinelAttacks.Sweep));
    bool QVulnerable() => _balls.Length <= 0;

    #endregion

    #endregion
}