using UnityEngine;

public class View : MonoBehaviour
{
    public Rigidbody2D RB => _rb;
    [SerializeField] Rigidbody2D _rb;

    public Collider2D Col => _col;
    [SerializeField] Collider2D _col;
    public Collider2D TriggerCol => _triggerCol;
    [SerializeField] Collider2D _triggerCol;

    public Animator Animator => _anim;
    [SerializeField] Animator _anim;

    public GameObject SpriteContainer => _spriteContainer;
    [SerializeField] GameObject _spriteContainer;

    public Portal LastUsedPortal { get; set; }

    private TrailRenderer _trailRenderer;
    public TrailRenderer TrailRendererComponent => _trailRenderer;

    private ParticleSystem _landingParticles;
    public ParticleSystem LandingParticles => _landingParticles;

    [SerializeField] private TrailRenderer _dashTrailRenderer;
    public TrailRenderer SlashTrail => _dashTrailRenderer;

    void Awake()
    {
        _trailRenderer = GetComponent<TrailRenderer>();

        if (_trailRenderer != null)
        {
            _trailRenderer.emitting = false;
        }

        _landingParticles = GetComponentInChildren<ParticleSystem>();

        if (_dashTrailRenderer != null)
        {
            _dashTrailRenderer.emitting = false;
        }
    }
}
