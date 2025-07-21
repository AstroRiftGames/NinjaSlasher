using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Chain : MonoBehaviour
{
    [SerializeField] Transform _anchor;
    [SerializeField] Transform _ball;
    [SerializeField] DemolitionSentinel _sentinel;

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

            if(Input.GetKeyDown(KeyCode.B))
            {
                BreakChain();
                ReleaseBall();
            }
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
        }
    }

    private void BreakChain()
    {
        _isActive = false;
        _renderer.enabled = false;
        Destroy(gameObject, 2f);
    }

    private void ReleaseBall()
    {
        _ball.TryGetComponent(out DemolitionBall ball);
        ball.enabled = false;
        _sentinel.RemoveBall(ball);

        _ball.TryGetComponent(out Rigidbody2D rb);
        rb.gravityScale = 1;
        rb.mass = 25;

        _ball.TryGetComponent(out Collider2D col);
        col.enabled = true;

        _ball.transform.SetParent(null);

        Destroy(_ball.gameObject, 2f);
    }
}
