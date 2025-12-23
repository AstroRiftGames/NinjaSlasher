using UnityEngine;

public class TrajectoryRenderer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _arrowRenderer;
    [SerializeField] private LayerMask _collisionLayers;
    [SerializeField] private float _defaultMaxDistance = 50f;

    [SerializeField] private PowerUpHawkVision _hawkVisionSettings;

    private float _fixedWidth;

    void Awake()
    {
        if (_arrowRenderer != null)
        {
            _fixedWidth = _arrowRenderer.size.x;
            _arrowRenderer.enabled = false;
        }
    }

    public void ShowTrajectory(Vector3 startPosition, Vector2 direction)
    {
        if (_arrowRenderer == null || direction == Vector2.zero) return;

        float maxDist = _hawkVisionSettings != null ? _hawkVisionSettings.maxDistance : _defaultMaxDistance;

        float distance = CalculateDistance(startPosition, direction, maxDist);
        UpdateArrowTransform(startPosition, direction, distance);

        _arrowRenderer.enabled = true;
    }

    private float CalculateDistance(Vector3 startPos, Vector2 direction, float maxDist)
    {
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction.normalized, maxDist, _collisionLayers);

        if (hit.collider != null)
        {
            return hit.distance;
        }

        return maxDist;
    }

    private void UpdateArrowTransform(Vector3 startPos, Vector2 direction, float distance)
    {
        _arrowRenderer.transform.position = startPos;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        _arrowRenderer.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        _arrowRenderer.size = new Vector2(_fixedWidth, distance);
    }

    public void HideTrajectory()
    {
        if (_arrowRenderer != null) _arrowRenderer.enabled = false;
    }
}