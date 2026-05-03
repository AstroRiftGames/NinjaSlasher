using UnityEngine;

public class TrajectoryRenderer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _indicator;
    public SpriteRenderer Indicator => _indicator;

    [SerializeField] private GameObject _hitMarker;
    private ParticleSystem _hitParticles;
    private Animator _animator;
 
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
            _hitMarker.SetActive(false);
            _hitParticles = _hitMarker.GetComponent<ParticleSystem>();
            _animator = _hitMarker.GetComponent<Animator>();

            if (_hitParticles != null)
            {
                _hitParticles.Stop();
                _hitParticles.Clear();
            }
        }
    }

    public void ShowTrajectory(Vector3 startPosition, Vector2 direction)
    {
        if (_indicator == null || direction == Vector2.zero) return;

        float maxDist = _hawkVisionSettings != null ? _hawkVisionSettings.maxDistance : _defaultMaxDistance;

        RaycastHit2D hit = CalculateHit(startPosition, direction);

        float distance;

        if (hit.collider != null)
        {
            distance = hit.distance;

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

                if (_hitParticles != null && !_hitParticles.isPlaying)
                {
                    _hitParticles.Play();
                }
            }
        }
        else
        {
            distance = maxDist;

            if (_hitMarker != null)
            {
                _hitMarker.SetActive(false);
                if (_hitParticles != null && _hitParticles.isPlaying)
                {
                    _hitParticles.Stop();
                }
            }
        }

        UpdateArrowTransform(startPosition, direction, distance);
        UpdateHitMarkerRotation(hit.normal);

        _indicator.enabled = true;
    }

    private RaycastHit2D CalculateHit(Vector3 startPos, Vector2 direction)
    {
        return Physics2D.CircleCast(
            startPos,
            _playerRadius * transform.parent.localScale.x,
            direction.normalized,
            100f,
            _collisionLayers
        );
    }

    private void UpdateArrowTransform(Vector3 startPos, Vector2 direction, float distance)
    {
        if (_indicator == null) return;
        _indicator.transform.position = startPos;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _indicator.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        
        _indicator.size = new Vector2(_fixedWidth, distance);
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
        if (_indicator != null) _indicator.enabled = false;
        if (_hitMarker != null) 
        {
            _hitMarker.SetActive(false);
            if (_hitParticles != null && _hitParticles.isPlaying)
            {
                _hitParticles.Stop();
            }
        }
    }
}