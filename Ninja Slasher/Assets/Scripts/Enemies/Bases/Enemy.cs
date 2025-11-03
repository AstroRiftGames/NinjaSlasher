using Managers;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] protected EnemyData _data;
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
    private EnemyTracker tracker;

    public virtual void OnEnable()
    {
        VulnerabilityCheck.OnVulnerabilityCheckColision -= DetectCollision;
        CustomUpdateManager.Instance.SubscribeToUpdate(CustomUpdate);
    }

    public virtual void OnDisable()
    {
        VulnerabilityCheck.OnVulnerabilityCheckColision += DetectCollision;
        CustomUpdateManager.Instance.UnsubscribeFromUpdate(CustomUpdate);
    }


    public virtual void Awake()
    {
        TryGetComponent(out Rigidbody2D rb);
        _rb = rb;
        TryGetComponent(out Collider2D col);
        _col = col;
        if(_animator == null)
        {
            TryGetComponent(out Animator anim);
            _animator = anim;
        }

        _player = FindAnyObjectByType<Controller>().transform;
        tracker = FindAnyObjectByType<EnemyTracker>();
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
        Debug.Log($"{_data.Type} killed");

        _animator.SetTrigger("OnHit");

        tracker.OnEnemyKilled(this);

        var combo = ComboManager.Instance;
        if (combo != null)
            combo.RegisterKill();

        Destroy(gameObject, _deathTime);
    }
}
