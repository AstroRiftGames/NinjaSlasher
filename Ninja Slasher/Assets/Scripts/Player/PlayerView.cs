using UnityEngine;

public class PlayerView : MonoBehaviour
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

    [System.Serializable]
    public struct SurfaceSpriteSet
    {
        public SurfaceMaterial materialType;
        public Sprite[] sprites;
    }

    [Header("Landing Particles")]
    [SerializeField] private SurfaceSpriteSet[] _surfaceParticleSets;

    public void SetLandingParticlesSurface(SurfaceMaterial material)
    {
        if (_landingParticles == null) return;
        
        var ts = _landingParticles.textureSheetAnimation;
        if (!ts.enabled) return;

        Sprite[] spritesToUse = null;
        
        if (_surfaceParticleSets != null)
        {
            foreach (var set in _surfaceParticleSets)
            {
                if (set.materialType == material)
                {
                    spritesToUse = set.sprites;
                    break;
                }
            }

            // fallback to general
            if (spritesToUse == null || spritesToUse.Length == 0)
            {
                foreach (var set in _surfaceParticleSets)
                {
                    if (set.materialType == SurfaceMaterial.General)
                    {
                        spritesToUse = set.sprites;
                        break;
                    }
                }
            }
        }

        if (spritesToUse != null && spritesToUse.Length > 0)
        {
            ts.mode = ParticleSystemAnimationMode.Sprites;
            while (ts.spriteCount > 0)
            {
                ts.RemoveSprite(0);
            }
            
            for (int i = 0; i < spritesToUse.Length; i++)
            {
                ts.AddSprite(spritesToUse[i]);
            }
        }
    }

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
