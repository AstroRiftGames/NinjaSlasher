using UnityEngine;

public class TrajectoryRenderer : MonoBehaviour
{
    [SerializeField] private GameObject _hitMarker;
    private Animator _animator;
 
    [SerializeField] private LayerMask _collisionLayers;
    [SerializeField] private float _defaultMaxDistance = 100f;
    [SerializeField] private float _playerRadius = 0.5f;

    [Header("Hawk Vision Guide")]
    [SerializeField] private Sprite _dotSprite;
    [SerializeField] private Material _dotMaterial;
    [SerializeField] private Color _dotColor = new Color(0.65f, 0.95f, 1f, 0.85f);
    [SerializeField] private float _dotSize = 0.08f;
    [SerializeField] private float _dotSpacing = 0.35f;
    [SerializeField] private int _maxDots = 24;
    [SerializeField] private string _dotSortingLayerName = "VFX";
    [SerializeField] private int _dotSortingOrder = 20;
    [SerializeField] private float _startOffset = 0.25f;
    [SerializeField] private float _endOffset = 0.15f;

    private SpriteRenderer[] _hawkVisionDots;
    private Transform _dotPoolRoot;
    private bool _missingDotSpriteWarningLogged;
    private int _activeDotCount;

    void Awake()
    {
        if (_hitMarker != null)
        {
            _hitMarker.SetActive(false);
            _animator = _hitMarker.GetComponent<Animator>();
        }

        DisableLegacyHawkVisionLine();
        SetupHawkVisionDots();
    }

    public void ShowTrajectory(Vector3 startPosition, Vector2 direction)
    {
        if (direction == Vector2.zero)
        {
            HideTrajectory();
            return;
        }

        RaycastHit2D hit = CalculateHit(startPosition, direction);

        if (hit.collider != null)
        {
            if (_hitMarker != null)
            {
                _hitMarker.SetActive(true);
                _hitMarker.transform.position = hit.point;

                if (_animator!= null)
                {
                    bool isSafe = !hit.collider.CompareTag("Spikes");
                    if(_animator.GetBool("IsAvailable") != isSafe) 
                    {
                        _animator.SetBool("IsAvailable", isSafe);
                    }
                }
            }

            if (IsHawkVisionActive())
                ShowHawkVisionDots(startPosition, hit.point);
            else
                HideHawkVisionDots();
        }
        else
        {
            if (_hitMarker != null)
            {
                _hitMarker.SetActive(false);
            }

            HideHawkVisionDots();
        }

        UpdateHitMarkerRotation(hit.normal);
    }

    private RaycastHit2D CalculateHit(Vector3 startPos, Vector2 direction)
    {
        float parentScale = transform.parent != null ? Mathf.Abs(transform.parent.localScale.x) : 1f;

        return Physics2D.CircleCast(
            startPos,
            _playerRadius * parentScale,
            direction.normalized,
            GetTrajectoryMaxDistance(),
            _collisionLayers
        );
    }

    private void UpdateHitMarkerRotation(Vector2 surfaceNormal)
    {
        if (_hitMarker == null) return;

        if (surfaceNormal != Vector2.zero)
        {
            _hitMarker.transform.up = surfaceNormal;
        }
    }

    public void HideTrajectory()
    {
        if (_hitMarker != null) 
        {
            _hitMarker.SetActive(false);
        }

        HideHawkVisionDots();
    }

    private bool IsHawkVisionActive()
    {
        PowerUpContext context = PowerUpManager.Instance != null ? PowerUpManager.Instance.context : null;
        return context != null && context.HawkVisionActive;
    }

    private float GetTrajectoryMaxDistance()
    {
        PowerUpContext context = PowerUpManager.Instance != null ? PowerUpManager.Instance.context : null;
        if (context != null && context.HawkVisionActive && context.HawkVisionTrajectoryMaxDistance > 0f)
            return context.HawkVisionTrajectoryMaxDistance;

        return _defaultMaxDistance;
    }

    private void DisableLegacyHawkVisionLine()
    {
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    private void SetupHawkVisionDots()
    {
        int dotCount = Mathf.Max(0, _maxDots);
        _hawkVisionDots = new SpriteRenderer[dotCount];

        GameObject rootObject = new GameObject("HawkVisionGuideDots");
        _dotPoolRoot = rootObject.transform;

        for (int i = 0; i < dotCount; i++)
        {
            GameObject dotObject = new GameObject("HawkVisionGuideDot_" + i);
            dotObject.transform.SetParent(_dotPoolRoot, false);

            SpriteRenderer dotRenderer = dotObject.AddComponent<SpriteRenderer>();
            ConfigureDotRenderer(dotRenderer);
            dotObject.SetActive(false);

            _hawkVisionDots[i] = dotRenderer;
        }
    }

    private void ConfigureDotRenderer(SpriteRenderer dotRenderer)
    {
        dotRenderer.sprite = _dotSprite;
        dotRenderer.color = _dotColor;

        if (_dotMaterial != null)
            dotRenderer.sharedMaterial = _dotMaterial;

        if (!string.IsNullOrEmpty(_dotSortingLayerName))
            dotRenderer.sortingLayerName = _dotSortingLayerName;

        dotRenderer.sortingOrder = _dotSortingOrder;
    }

    private void ShowHawkVisionDots(Vector3 startPosition, Vector2 hitPoint)
    {
        if (_dotSprite == null)
        {
            if (!_missingDotSpriteWarningLogged)
            {
                Debug.LogWarning("Hawk Vision guide dot sprite is not assigned.", this);
                _missingDotSpriteWarningLogged = true;
            }

            HideHawkVisionDots();
            return;
        }

        if (_hawkVisionDots == null || _hawkVisionDots.Length == 0)
        {
            HideHawkVisionDots();
            return;
        }

        Vector3 start = startPosition;
        Vector3 end = hitPoint;
        start.z = 0f;
        end.z = 0f;

        Vector3 offset = end - start;
        float distance = offset.magnitude;
        if (distance <= 0f)
        {
            HideHawkVisionDots();
            return;
        }

        Vector3 direction = offset / distance;
        float startOffset = Mathf.Max(0f, _startOffset);
        float endOffset = Mathf.Max(0f, _endOffset);
        float usableDistance = distance - startOffset - endOffset;
        if (usableDistance <= 0f)
        {
            HideHawkVisionDots();
            return;
        }

        float spacing = Mathf.Max(0.01f, _dotSpacing);
        int desiredDotCount = Mathf.FloorToInt(usableDistance / spacing);
        int dotCount = Mathf.Min(Mathf.Max(0, _maxDots), desiredDotCount);
        dotCount = Mathf.Min(dotCount, _hawkVisionDots.Length);

        Vector3 dotScale = Vector3.one * Mathf.Max(0.01f, _dotSize);

        for (int i = 0; i < dotCount; i++)
        {
            SpriteRenderer dotRenderer = _hawkVisionDots[i];
            if (dotRenderer == null)
                continue;

            dotRenderer.transform.position = start + direction * (startOffset + i * spacing);
            dotRenderer.transform.localScale = dotScale;

            if (!dotRenderer.gameObject.activeSelf)
                dotRenderer.gameObject.SetActive(true);
        }

        for (int i = dotCount; i < _activeDotCount; i++)
        {
            if (i >= _hawkVisionDots.Length)
                break;

            SpriteRenderer dotRenderer = _hawkVisionDots[i];
            if (dotRenderer != null && dotRenderer.gameObject.activeSelf)
                dotRenderer.gameObject.SetActive(false);
        }

        _activeDotCount = dotCount;
    }

    private void HideHawkVisionDots()
    {
        if (_hawkVisionDots == null)
            return;

        for (int i = 0; i < _activeDotCount; i++)
        {
            if (_hawkVisionDots[i] != null)
                _hawkVisionDots[i].gameObject.SetActive(false);
        }

        _activeDotCount = 0;
    }

    private void OnDisable()
    {
        HideTrajectory();
    }

    private void OnDestroy()
    {
        if (_dotPoolRoot != null)
            Destroy(_dotPoolRoot.gameObject);
    }
}
