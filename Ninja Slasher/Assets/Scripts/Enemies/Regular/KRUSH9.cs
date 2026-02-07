using Unity.VisualScripting;
using UnityEngine;
using AstroRift.Core.Update;

public class KRUSH9 : Enemy
{
    [SerializeField] float _attackSpeed;
    [SerializeField] float _attackCD;
    float _lastAttack;

    public override void Awake()
    {
        base.Awake();
        _animator = GetComponent<Animator>();
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

    private bool TargetClose()
    {
        return Vector2.Distance(transform.position, _player.position) < _data.Range;
    }

    private bool CanAttack()
    {
        return Time.time > _lastAttack + _attackCD;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _data.Range);
    }
}
