using System;
using TMPro.EditorUtilities;
using UnityEngine;

public class DemolitionBall : MonoBehaviour
{
    [SerializeField] Transform _anchor;
    public Transform Anchor => _anchor;
    [SerializeField] float _maxDistance;
    [SerializeField] float _minDistance;
    [SerializeField] float _force;

    bool _isRetrieving;


    public Rigidbody2D RB => _rb;
    private Rigidbody2D _rb;

    public EventHandler<DemolitionBall> OnMaxDistanceReached;


    [Header("HeavyAttack")]
    [SerializeField] float _heavyAttackRadius;
    [SerializeField] bool _heavyAttack;

    private void Start()
    {
        TryGetComponent(out Rigidbody2D rb);
        _rb = rb;
    }

    private void Update()
    { 
        if (IsMaxDistanceReached())
        {
            Retrieve();
        }

        if(_isRetrieving && IsBackOn())
        {
            _rb.linearVelocity = Vector2.zero;
            _isRetrieving = false;
        }

        Debug.Log($"{name} is retrieving: {_isRetrieving}");
    }

    private bool IsMaxDistanceReached()
    {
        return Vector2.Distance(transform.localToWorldMatrix.GetPosition(), _anchor.localToWorldMatrix.GetPosition()) >= _maxDistance;
    }

    private bool IsBackOn()
    {
        return Vector2.Distance(transform.localToWorldMatrix.GetPosition(), _anchor.localToWorldMatrix.GetPosition()) <= _minDistance;
    }

    public void Throw(Vector2 direction)
    {
        _rb.AddForce(direction * _force, ForceMode2D.Impulse);
    }

    public void HeavyThrow(Vector2 direction)
    {
        Throw(direction);
        _heavyAttack = true;
    }

    private void CreateDamageArea(Vector2 position)
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(position, _heavyAttackRadius);

        foreach(var col in cols)
        {
            if (col.gameObject.CompareTag("Player"))
            {
                col.TryGetComponent(out Controller player);
                player.Die();
            }
        }
    }


    private void Retrieve()
    {
        Vector2 dir = (_anchor.localToWorldMatrix.GetPosition() - transform.localToWorldMatrix.GetPosition()).normalized;
        _isRetrieving = true;
        _rb.linearVelocity = Vector2.zero;
        Throw(dir);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_heavyAttack) CreateDamageArea(collision.transform.position);
        Retrieve();
    }


    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(_anchor.position, _maxDistance);
    }
}