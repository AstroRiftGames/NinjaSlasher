using Unity.VisualScripting;
using UnityEngine;

public class KRUSH9 : Enemy
{
    [SerializeField] float _attackSpeed;
    private Animator _animator;

    public override void Awake()
    {
        base.Awake();
        _animator = GetComponent<Animator>();
    }

    public override void Start()
    {
        base.Start();
        _animator.SetFloat("Speed", _attackSpeed);
    }

#if UNITY_EDITOR
    private void Update()
    {
        _animator.SetFloat("Speed", _attackSpeed);
    }
#endif
}
