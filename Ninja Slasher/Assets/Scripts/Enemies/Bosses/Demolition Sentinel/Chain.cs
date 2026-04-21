using UnityEngine;
using System;

public class Chain : RezisableObject
{
    [SerializeField] Transform _anchor;
    
    private DemolitionBall _ball;
    public Transform BallT => _ballT;
    [SerializeField] Transform _ballT;
    [SerializeField] DemolitionSentinel _sentinel;
    [SerializeField] GameObject _chain;
    [SerializeField] private float _multiplier = 1.9f;

    public bool IsActive => _isActive;
    private bool _isActive = true;
    private bool _isMoving;
    public void SetIsMoving(bool value) => _isMoving = value;


    public override void Awake()
    {
        GetComponents();
        UpdatePositions();
    }

    private void UpdatePositions()
    {
        SetTarget(_ballT.localToWorldMatrix.GetPosition());
        SetAnchor(_anchor.localToWorldMatrix.GetPosition());
    }

    public override void GetComponents()
    {
        _chain.TryGetComponent(out SpriteRenderer r);
        _renderer = r;
        _chain.TryGetComponent(out BoxCollider2D col);
        _collider = col;
        TryGetComponent(out Animator anim);
        _animator = anim;
        _ballT.TryGetComponent(out DemolitionBall ball);
        _ball = ball;
    }

    private void Start()
    {
        _chain.transform.position = _anchor.position;
    }
    private void Update()
    {
        if(_isActive)
        {
            UpdatePositions();
            AdjustRotation();
            AdjustPosition();
            _collider.enabled = _ball.IsOut;
            AdjustSize(_multiplier);
        }
        _animator.SetBool("IsMoving", _isMoving);
    }

    private void AdjustRotation()
    {
        Vector2 dir = _ballT.position - _anchor.position;
        float angle = (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        _chain.transform.rotation = Quaternion.Euler(0, 0, angle+90);
    }

    public void DetectCollision()
    {
        BreakChain();
        ReleaseBall();
        _sentinel.SentinelAudio.PlayChainDamaged();
        _sentinel.StopAttack();
    }

    public void BreakChain()
    {
        _renderer.enabled = false;
        _collider.enabled = false;
        _isActive = false;
        _sentinel.Animator.SetTrigger("onHit");
        _ball.PivotPoint.rotation = Quaternion.Euler(0, 0, -90);
    }

    public void RepairChain()
    {
        _renderer.enabled = true;
        _collider.enabled = true;
        _isActive = true;
    }

    public void ReleaseBall()
    {
        _ballT.TryGetComponent(out DemolitionBall ball);
        ball.Release();
        _sentinel.RemoveBall(ball);
        ball.enabled = false;

        _ballT.TryGetComponent(out Rigidbody2D rb);
        rb.gravityScale = 1;
        rb.mass = 25;

        _ballT.TryGetComponent(out Collider2D col);
        col.excludeLayers = LayerMask.GetMask("Player");

        _ballT.transform.SetParent(null);
    }

    public void RecoverBall()
    {
        _ballT.TryGetComponent(out Rigidbody2D rb);
        rb.gravityScale = 0;
        rb.mass = 1;

        _ballT.TryGetComponent(out Collider2D col);
        if (col != null) col.excludeLayers = 0;

        _ballT.transform.SetParent(_anchor);

        _ballT.TryGetComponent(out DemolitionBall ball);
        ball.enabled = true;
        _sentinel.AddBall(ball, _ballT.name.Contains("Right", StringComparison.OrdinalIgnoreCase));
        ball.StartCoroutine(ball.Return());
    }
}
