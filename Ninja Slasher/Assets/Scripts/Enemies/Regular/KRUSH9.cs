using UnityEngine;

public class KRUSH9 : Enemy
{
    [SerializeField] float _attackSpeed;
    [SerializeField] float _attackCD;
    float _lastAttack;
    KRUSH9_Weapon _weapon;

    protected override void Awake()
    {
        base.Awake();
        _animator = GetComponent<Animator>();
        _weapon = GetComponentInChildren<KRUSH9_Weapon>();
    }

    public override void Start()
    {
        base.Start();
        _animator.SetFloat("AttackSpeed", _attackSpeed);
    }

    public override void CustomUpdate()
    {
        _animator.SetFloat("AttackSpeed", _attackSpeed);
        if(TargetClose() && CanAttack())
        {
            Attack();
        }
    }

    private void Attack()
    {
        _animator.SetTrigger("OnAttack");
        _lastAttack = Time.time;
    }

    public void PlayDeathSFX()
    {
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.death, transform.position);
    }

    private bool TargetClose()
    {
        return Vector2.Distance(transform.position, _player.position) < _data.Range;
    }

    private bool CanAttack()
    {
        return Time.time > _lastAttack + _attackCD;
    }

    public override void Die()
    {
        _weapon.Col.enabled = false;
        base.Die();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _data.Range);
    }
}
