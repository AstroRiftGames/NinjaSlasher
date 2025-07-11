using UnityEngine;
using UnityEngine.UIElements;

public class Chain : MonoBehaviour
{
    [SerializeField] Transform _anchor;
    [SerializeField] Transform _objective;
    SpriteRenderer _renderer;

    private void Awake()
    {
        TryGetComponent(out SpriteRenderer r);
        _renderer = r;
    }
    private void Start()
    {
        transform.position = _anchor.position;
    }
    private void Update()
    {
        Vector2 dir = _objective.position - _anchor.position;

        float angle = (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        transform.rotation = Quaternion.Euler(0,0,angle);

        transform.position = _anchor.position;
        _renderer.size = new Vector2(CalculateLength() * CheckScale(), .553f);
    }

    private float CheckScale() => _renderer.flipX ? -1:1;

    float CalculateLength()
    {
        return Vector2.Distance(_objective.localToWorldMatrix.GetPosition(), _anchor.localToWorldMatrix.GetPosition());
    }
}
