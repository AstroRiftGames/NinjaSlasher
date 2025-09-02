using UnityEngine;
using System;

public class Chain : MonoBehaviour
{
    [SerializeField] Transform _anchor;
    [SerializeField] Transform _ball;
    public Transform Ball => _ball;
    [SerializeField] DemolitionSentinel _sentinel;

    public bool IsActive => _isActive;
    private bool _isActive = true;

    SpriteRenderer _renderer;
    BoxCollider2D _collider;

    private void Awake()
    {
        TryGetComponent(out SpriteRenderer r);
        _renderer = r;
        TryGetComponent(out BoxCollider2D col);
        _collider = col;
    }
    private void Start()
    {
        transform.position = _anchor.position;
    }
    private void Update()
    {
        if(_isActive)
        {
            AdjustRotation();
            AdjustPosition();
            AdjustSize();
        }
    }


    private Vector2 GetSize(bool _isRenderer)
    {
        float scale = _renderer.flipX ? -1 : 1;
        return  new Vector2(CalculateLength() * (_isRenderer ? scale : 1), .553f);
    }

    private void AdjustSize()
    {
        _renderer.size = GetSize(true);

        _collider.size = GetSize(false);
        _collider.offset = new Vector2(_collider.size.x / 2, 0);
        
    }

    private void AdjustPosition()
    {
        transform.position = _anchor.position;
    }

    private void AdjustRotation()
    {
        Vector2 dir = _ball.position - _anchor.position;
        float angle = (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }


    float CalculateLength()
    {
        return Vector2.Distance(_ball.localToWorldMatrix.GetPosition(), _anchor.localToWorldMatrix.GetPosition());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            BreakChain();
            ReleaseBall();
            _sentinel.StopAttack();
        }
    }

    public void BreakChain()
    {
        _renderer.enabled = false;
        _collider.enabled = false;
        _isActive = false;
        _sentinel.Animator.SetTrigger("onHit");
    }

    public void RepairChain()
    {
        _renderer.enabled = true;
        _collider.enabled = true;
        _isActive = true;
    }

    public void ReleaseBall()
    {
        _ball.TryGetComponent(out DemolitionBall ball);
        ball.Release();
        _sentinel.RemoveBall(ball);
        ball.enabled = false;

        _ball.TryGetComponent(out Rigidbody2D rb);
        rb.gravityScale = 1;
        rb.mass = 25;

        _ball.TryGetComponent(out Collider2D col);
        col.excludeLayers = LayerMask.GetMask("Player");

        _ball.transform.SetParent(null);
    }

    public void RecoverBall()
    {
        _ball.TryGetComponent(out Rigidbody2D rb);
        rb.gravityScale = 0;
        rb.mass = 1;

        _ball.TryGetComponent(out Collider2D col);
        col.includeLayers = LayerMask.GetMask("Player");

        _ball.transform.SetParent(_anchor);

        _ball.TryGetComponent(out DemolitionBall ball);
        ball.enabled = true;
        _sentinel.AddBall(ball, _ball.name.Contains("Right", StringComparison.OrdinalIgnoreCase));
        ball.StartCoroutine(ball.Return());
    }
}
