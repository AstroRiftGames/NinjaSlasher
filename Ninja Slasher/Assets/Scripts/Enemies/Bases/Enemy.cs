using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] protected EnemyData _data;
    protected EnemyAudioContext _audioContext;
    public EnemyAudioContext AudioContext => _audioContext;
    [SerializeField] protected GameObject UpperCol;
    [SerializeField] protected GameObject LowerCol;
    [SerializeField] protected GameObject RearCol;
    [SerializeField] protected GameObject FrontCol;
    [SerializeField] float _deathTime;

    [SerializeField] protected LayerMask _obstaclesLayer;
    [SerializeField] protected LayerMask _playerLayer;

    protected Transform _player;
    protected Rigidbody2D _rb;
    protected Collider2D _col;
    [SerializeField] protected Animator _animator;
    public Animator Animator => _animator;

    public virtual void OnEnable()
    {
        VulnerabilityCheck.OnVulnerabilityCheckColision += DetectCollision;

        if (CustomUpdateManager.Instance != null)
            CustomUpdateManager.Instance.SubscribeToUpdate(CustomUpdate);
    }

    public virtual void OnDisable()
    {
        VulnerabilityCheck.OnVulnerabilityCheckColision -= DetectCollision;

        if (CustomUpdateManager.Instance != null)
            CustomUpdateManager.Instance.UnsubscribeFromUpdate(CustomUpdate);
    }

    protected virtual void Awake()
    {
        TryGetComponent(out Rigidbody2D rb);
        _rb = rb;
        TryGetComponent(out Collider2D col);
        _col = col;
        if (_animator == null)
        {
            TryGetComponent(out Animator anim);
            _animator = anim;
        }
        _audioContext = GetComponent<EnemyAudioContext>();

        if (_audioContext != null && _data != null)
        {
            _audioContext.Initialize(_data.AudioSet);
        }

        _player = FindAnyObjectByType<NewController>().transform;
    }

    public virtual void Start()
    {
        CheckVulnerability();
    }

    public virtual void CustomUpdate() { }

    protected void DetectCollision(Direction dir)
    {
#if UNITY_EDITOR
        Debug.Log($"Hit rejected: {dir}");
#endif
    }

    protected void CheckVulnerability()
    {
        UpperCol.SetActive(!_data.IsVulnerable.fromUp);
        LowerCol.SetActive(!_data.IsVulnerable.fromDown);
        RearCol.SetActive(!_data.IsVulnerable.fromBehind);
        FrontCol.SetActive(!_data.IsVulnerable.fromFront);
    }

    public virtual void Die()
    {
        _animator.SetTrigger("OnHit");
        _col.includeLayers -= LayerMask.GetMask("Player");

        RegisterKill();

        Destroy(gameObject, _deathTime);
    }

    public void RegisterKill()
    {
        if (LevelSessionManager.Instance != null)
        {
            LevelSessionManager.Instance.RegisterEnemyKilled(this);
        }
        else
        {
            Debug.LogError("[Enemy] LevelSessionManager no encontrado - el enemigo no será trackeado");
        }

        var combo = ComboManager.Instance;
        if (combo != null)
            combo.RegisterKill(transform.position);
    }
}