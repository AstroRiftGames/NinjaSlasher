using UnityEngine;
using UnityEngine.UIElements;

public class Chain : MonoBehaviour
{
    [SerializeField] Transform _anchor;
    [SerializeField] Transform _ball;

    private bool _isActive = true;

    SpriteRenderer _renderer;
    Collider2D _collider;

    private void Awake()
    {
        TryGetComponent(out SpriteRenderer r);
        _renderer = r;
        TryGetComponent(out Collider2D col);
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

    private void AdjustSize()
    {
        Vector2 newSize = new Vector2 (CalculateLength() * CheckScale(), .553f);
        _renderer.size = newSize;
        _collider.bounds.SetMinMax(newSize, newSize);
        
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

    private float CheckScale() => _renderer.flipX ? -1:1;

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
