using System;
using System.Collections;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class DemolitionBall : MonoBehaviour
{
    [SerializeField] Transform _pivotPoint;
    [SerializeField] DemolitionSentinel _sentinel;
    [SerializeField] Chain _chain;
    public Chain Chain => _chain;
    public Transform PivotPoint => _pivotPoint;
    [SerializeField] float _maxDistance;
    [SerializeField] float _force;
    public bool IsReturning => _isReturning;
    private bool _isReturning;
    public void SetReturn(bool value) => _isReturning = value;
    public bool IsOut => _isOut;
    private bool _isOut;
    public void SetIsOut(bool value) => _isOut = value;

    public Rigidbody2D RB => _rb;
    private Rigidbody2D _rb;
    [SerializeField] Animator _animator;


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
        CheckMaxDistanceReached();
    }

    private void CheckMaxDistanceReached()
    {
        Vector2 v = transform.position - _sentinel.transform.position;
        float dis = v.magnitude;
        Vector2 dir = v.normalized;

        if (dis > _maxDistance)
        {
            Stop();
            Vector2 newPos = (Vector2)_sentinel.transform.position + (dir * _maxDistance - dir);
            transform.SetPositionAndRotation(newPos, Quaternion.identity);
            if(!_isReturning) _sentinel.ReturnOneBall(this);
        }
    }

    public void Stop()
    {
        _rb.linearVelocity = Vector2.zero;
        _chain.SetIsMoving(false);
    }

    public IEnumerator Return(float lapse = 1)
    {
        SetReturn(true);
        _chain.SetIsMoving(true);
        Vector3 initPos = transform.localPosition;
        Vector3 targetPos = new Vector3(3f, 0, 0);
        float t = 0;
        while (t < 1 && enabled)
        {
            t += Time.deltaTime / lapse;
            transform.localPosition = Vector3.Lerp(initPos, targetPos, t);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(Vector3.zero), t);
            yield return null;
        }
        SetReturn(false);
        SetIsOut(false);
        _chain.SetIsMoving(false);
    }

    public void Release()
    {
        StopAllCoroutines();
        SetReturn(false);
        SetIsOut(false);
        _heavyAttack = false;
    }

    public void Throw()
    {
        SetIsOut(true);
        _chain.SetIsMoving(true);
        transform.localRotation.Set(0, 0, 0, 0);
        _rb.AddForce(transform.right * _force, ForceMode2D.Impulse);
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.B_Sentinel_Woosh, transform.position);
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.B_Sentinel_Sweep, transform.position);
    }

    public void HeavyThrow()
    {
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.B_Sentinel_Heavy, transform.position);
        Throw();
        _heavyAttack = true;
    }

    private void CreateDamageArea(Vector2 position)
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(position, _heavyAttackRadius);

        foreach(var col in cols)
        {
            if(col.gameObject.CompareTag("Player"))
            {
                col.TryGetComponent(out Controller player);
                player.Die();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        _animator.SetTrigger("OnImpact");
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.B_Sentinel_Impact, transform.position);
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.TryGetComponent(out Controller player);
            player.Die();
            Stop();
            if (!_isReturning) _sentinel.ReturnOneBall(this);
        }
        if(_heavyAttack)
        {
            _heavyAttack = false;
            CreateDamageArea(collision.transform.position);
        }
        Stop();
        if(!_isReturning) _sentinel.ReturnOneBall(this);
    }
}