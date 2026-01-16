using Unity.VisualScripting;
using UnityEngine;

public class RezisableObject : MonoBehaviour
{
    protected SpriteRenderer _renderer;
    protected BoxCollider2D _collider;
    protected Animator _animator;

    private Vector2 _anchorPos;
    private Vector2 _targetPos;

    [SerializeField] private Transform _startT;
    [SerializeField] private Transform _endT;
    [SerializeField] private bool _horizontalExpansion = true;


    public virtual void Awake()
    {
        GetComponents();
        SetTarget(_endT.position);
        SetAnchor(_startT.position);
    }

    private void Start()
    {
        AdjustSize();
        AdjustPosition();
    }

    public virtual void GetComponents()
    {
        TryGetComponent(out SpriteRenderer r);
        _renderer = r;
        TryGetComponent(out BoxCollider2D col);
        _collider = col;
        TryGetComponent(out Animator anim);
        _animator = anim;
    }

    protected void AdjustSize(float multiplier = 1f)
    {
        _renderer.size = GetSize(true, multiplier);

        _collider.size = GetSize(false, multiplier);
        _collider.offset = new Vector2(
            _horizontalExpansion ? _collider.size.x / 2 : 0,
            _horizontalExpansion ? 0 : -_collider.size.y / 2);

    }

    protected void AdjustPosition()
    {
        transform.position = _anchorPos;
    }

    private Vector2 GetSize(bool _isRenderer, float multiplier)
    {
        float scale = _renderer.flipY ? -1 : 1;
        Vector2 size = _isRenderer ? _renderer.size : _collider.size;
        return new Vector2(
            _horizontalExpansion ? CalculateLength(multiplier) : size.x,
            _horizontalExpansion ?  size.y : CalculateLength(multiplier) * (_isRenderer ? scale : 1));
    }
    float CalculateLength(float multiplier = 1f)
    {
        return Vector2.Distance(_targetPos, _anchorPos) * multiplier;
    }

    protected void SetTarget(Vector2 position)
    {
        _targetPos = position;
    }

    protected void SetAnchor(Vector2 position)
    {
        _anchorPos = position;
    }
}
