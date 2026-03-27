using UnityEngine;

public class TrajectoryRenderer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _indicator;
    public SpriteRenderer Indicator => _indicator;

    [SerializeField] private SpriteRenderer _hitMarker;

    [SerializeField] private LayerMask _collisionLayers;
    [SerializeField] private float _defaultMaxDistance = 5f;

    [SerializeField] private float _playerRadius = 0.5f;

    [SerializeField] private PowerUpHawkVision _hawkVisionSettings;

    private float _fixedWidth;

    void Awake()
    {
        if (_indicator != null)
        {
            _fixedWidth = _indicator.size.x;
            _indicator.enabled = false;
        }

        if (_hitMarker != null)
        {
            _hitMarker.enabled = false;
        }
    }

    public void ShowTrajectory(Vector3 startPosition, Vector2 direction)
    {
        if (_indicator == null || direction == Vector2.zero) return;

        float maxDist = _hawkVisionSettings != null ? _hawkVisionSettings.maxDistance : _defaultMaxDistance;

        RaycastHit2D hit = CalculateHit(startPosition, direction);

        float distance;
        Vector3 hitPoint;

        if (hit.collider != null)
        {
            distance = hit.distance;
            hitPoint = hit.point;

            if (_hitMarker != null)
            {
                _hitMarker.enabled = true;
                _hitMarker.transform.position = hitPoint;
            }
        }
        else
        {
            distance = maxDist;
            hitPoint = startPosition + (Vector3)(direction.normalized * maxDist);

            if (_hitMarker != null)
            {
                _hitMarker.enabled = false;
            }
        }

        //UpdateArrowTransform(startPosition, direction, distance);

        //_indicator.enabled = true;
    }

    private RaycastHit2D CalculateHit(Vector3 startPos, Vector2 direction)
    {
        return Physics2D.CircleCast(
            startPos,
            _playerRadius * transform.parent.localScale.x,
            direction.normalized,
            20f,
            _collisionLayers
        );
    }

    private void UpdateArrowTransform(Vector3 startPos, Vector2 direction, float distance)
    {
        _indicator.transform.position = startPos;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _indicator.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        _indicator.size = new Vector2(_fixedWidth, distance);
    }

    public void HideTrajectory()
    {
        //if (_indicator != null) _indicator.enabled = false;
        if (_hitMarker != null) _hitMarker.enabled = false;
    }
}