using UnityEngine;
using UnityEngine.UIElements;

public class Chain : MonoBehaviour
{
    [SerializeField] Transform _anchor;
    [SerializeField] Transform _ball;

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
        Debug.Log($"Col: {_collider != null}");
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
        }
    }

    private void BreakChain()
    {
        _isActive = false;
        _ball.TryGetComponent(out DemolitionBall ball);
        ball.enabled = false;
        _ball.TryGetComponent(out Rigidbody2D rb);
        rb.gravityScale = 1;
        _ball.transform.SetParent(null);
    }
}
