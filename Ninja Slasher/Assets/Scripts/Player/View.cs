using UnityEngine;

public class View : MonoBehaviour
{
    public Rigidbody2D RB => _rb;
    [SerializeField] Rigidbody2D _rb;

    public Collider2D Col => _col;
    [SerializeField] Collider2D _col;

    public Animator Animator => _anim;
    [SerializeField] Animator _anim;

    public GameObject SpriteContainer => _spriteContainer;
    [SerializeField] GameObject _spriteContainer;

    public Portal LastUsedPortal { get; set; }

    private TrailRenderer _trailRenderer;
    public TrailRenderer TrailRendererComponent => _trailRenderer;

    private ParticleSystem _landingParticles;
    public ParticleSystem LandingParticles => _landingParticles;

    [SerializeField] private TrailRenderer _slashTrailRenderer;
    public TrailRenderer SlashTrail => _slashTrailRenderer;

    void Awake()
    {
        _trailRenderer = GetComponent<TrailRenderer>();

        if (_trailRenderer != null)
        {
            _trailRenderer.emitting = false;
        }

        _landingParticles = GetComponentInChildren<ParticleSystem>();

        if (_slashTrailRenderer != null)
        {
            _slashTrailRenderer.emitting = false;
        }
    }
}
