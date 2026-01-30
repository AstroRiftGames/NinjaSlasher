using UnityEngine;

public class KRUSH9 : Enemy
{
    [SerializeField] float _attackSpeed;

    protected override void Awake()
    {
        base.Awake();
        _animator = GetComponent<Animator>();
    }

    public override void Start()
    {
        base.Start();
        _animator.SetFloat("Speed", _attackSpeed);
    }

    public override void CustomUpdate()
    {
        _animator.SetFloat("Speed", _attackSpeed);
    }
}
