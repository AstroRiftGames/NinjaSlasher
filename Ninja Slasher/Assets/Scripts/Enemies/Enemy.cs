using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] protected EnemyData _data;
    [SerializeField] GameObject UpperCol;
    [SerializeField] GameObject LowerCol;
    [SerializeField] GameObject RearCol;
    [SerializeField] GameObject FrontCol;


    protected Transform _player;
    protected Rigidbody2D _rb;

    public virtual void OnEnable()
    {
        VulnerabilityCheck.OnVulnerabilityCheckColision += DetectCollision;
    }
    public virtual void OnDisable()
    {
        VulnerabilityCheck.OnVulnerabilityCheckColision -= DetectCollision;
    }

    public virtual void Awake()
    {
        TryGetComponent(out Rigidbody2D rb);
        _rb = rb;

        _player = FindAnyObjectByType<Controller>().transform;
    }

    public virtual void Start()
    {
        CheckVulnerability();
    }

    protected void DetectCollision(Direction dir)
    {
#if UNITY_EDITOR
        Debug.Log($"Hit rejected: {dir}");
#endif
    }

    public virtual void Update()
    {

    }

    protected void CheckVulnerability()
    {
        if(!_data.IsVulnerable.fromUp) UpperCol.SetActive(true);
        if(!_data.IsVulnerable.fromDown) LowerCol.SetActive(true);
        if(!_data.IsVulnerable.fromBehind) RearCol.SetActive(true);
        if(!_data.IsVulnerable.fromFront) FrontCol.SetActive(true);
    }

    public void Die()
    {
        Debug.Log($"{_data.Type} killed");
        Destroy(gameObject);
    }
}
