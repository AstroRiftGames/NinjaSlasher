using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryRenderer : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    private PowerUpHawkVision _trajectorySettings;

    [SerializeField] private LayerMask _collisionLayers;

    [SerializeField] private Material _lineMaterial;
    [SerializeField] private Color _defaultLineColor = new Color(0f, 1f, 1f, 0.7f);
    [SerializeField] private float _defaultLineWidth = 0.1f;
    [SerializeField] private float _defaultMaxDistance = 50f;

    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        ConfigureLineRenderer();
    }

    void ConfigureLineRenderer()
    {
        if (_lineRenderer == null) return;

        _lineRenderer.startWidth = _defaultLineWidth;
        _lineRenderer.endWidth = _defaultLineWidth;
        _lineRenderer.positionCount = 0;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.enabled = false;

        if (_lineMaterial != null)
        {
            _lineRenderer.material = _lineMaterial;
        }

        _lineRenderer.startColor = _defaultLineColor;
        _lineRenderer.endColor = _defaultLineColor;

        _lineRenderer.sortingLayerName = "Player";
        _lineRenderer.sortingOrder = 10;
    }

    public void SetTrajectorySettings(PowerUpHawkVision settings)
    {
        _trajectorySettings = settings;

        if (_lineRenderer != null && settings != null)
        {
            _lineRenderer.startWidth = settings.lineWidth;
            _lineRenderer.endWidth = settings.lineWidth;
            _lineRenderer.startColor = settings.lineColor;
            _lineRenderer.endColor = settings.lineColor;
        }
    }

    public void ShowTrajectory(Vector3 startPosition, Vector2 direction)
    {
        if (_lineRenderer == null) return;

        float maxDistance = _trajectorySettings != null ? _trajectorySettings.maxDistance : _defaultMaxDistance;

        CalculateTrajectory(startPosition, direction, maxDistance);
    }

    void CalculateTrajectory(Vector3 startPos, Vector2 direction, float maxDistance)
    {
        Vector2 normalizedDir = direction.normalized;

        RaycastHit2D hit = Physics2D.Raycast(startPos, normalizedDir, maxDistance, _collisionLayers);

        _lineRenderer.positionCount = 2;

        if (hit.collider != null)
        {
            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, hit.point);
        }
        else
        {
            Vector2 endPoint = (Vector2)startPos + normalizedDir * maxDistance;
            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPoint);
        }

        _lineRenderer.enabled = true;
    }

    public void HideTrajectory()
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
            _lineRenderer.positionCount = 0;
        }
    }

    void OnDisable()
    {
        HideTrajectory();
    }
}