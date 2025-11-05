using Managers;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class GuardBot : Enemy
{
    [SerializeField] Transform _refPoint;
    [SerializeField] float _speed;
    [SerializeField][Range(1, 2)] float _speedMultiplier;
    [SerializeField] Transform[] _nodes;

    private float _currentSpeed;
    private bool _isPushing;
    public bool IsPushing => _isPushing;
    private Vector2 _target;

    private float _direction => transform.localScale.x > 0 ? 1 : -1;

    bool _isAlert;
    bool _hasPlayedDetectionSFX;
    float _lastDetectionTime;
    [SerializeField] float _resetDelay = 5f;


    public override void Awake()
    {
        base.Awake();
        _currentSpeed = _speed;
    }

    public override void CustomUpdate()
    {
        if (!CheckDistanceToTarget(_target)) 
        {
            TryMove();
        }

        if (!_isPushing)
        {
            if (CheckTarget())
            {
                StartCoroutine(Push());
            }
            else if (_target == Vector2.zero || CheckDistanceToTarget(_target))
            {
                SetPatrolTarget();
            }
        }
        else if (CheckDistanceToTarget(_target))
        {
            _animator.SetTrigger("OnTargetReached");
            StopAllCoroutines();
            _currentSpeed = _speed;
            _isPushing = false;
            SetPatrolTarget();
        }
    }

    private void TryMove()
    {
        transform.localScale = new Vector3(_target.x > transform.localToWorldMatrix.GetPosition().x ? 1 : -1, transform.localScale.y, transform.localScale.z);


        bool thereIsFloor = Physics2D.Raycast(transform.position + transform.right * -_direction, Vector3.down, .5f, _obstaclesLayer);
        if(thereIsFloor)
        {
            _rb.linearVelocityX = _direction * _currentSpeed;
        }
        else
        {
            if(_isPushing)
            {
                _animator.SetTrigger("OnFloorEnd");
            }
            _rb.linearVelocityX = 0;
        }
    }

    private bool CheckDistanceToTarget(Vector2 target)
    {
        bool reached = Mathf.Abs(target.x - _refPoint.localToWorldMatrix.GetPosition().x) <= .5f;
        return reached;
    }

    private bool CheckTarget()
    {
        return Physics2D.Raycast(_refPoint.position, -transform.right * _direction, _data.Range, _playerLayer);
    }

    private IEnumerator Push()
    {
        _target = GetPlayerPos();
        _isPushing = true;
        _animator.SetTrigger("OnDetection");
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.E_Guard_Detection, transform.position);
        _currentSpeed = 0;
        yield return new WaitForSeconds(1f);
        _animator.SetTrigger("OnPushStart");
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.E_Guard_Charge, transform.position);
        _currentSpeed = _speed * _speedMultiplier;
        yield return new WaitForSeconds(2f);
        _isPushing = false;
        _currentSpeed = _speed;
        _animator.SetTrigger("OnPushEnd");
        SetPatrolTarget();
    }

    private void SetPatrolTarget()
    {
        _rb.linearVelocityX = 0;
        if (_target == (Vector2)_nodes[0].localToWorldMatrix.GetPosition())
        {
            _target = _nodes[1].localToWorldMatrix.GetPosition();
        }
        else
        {
            _target = _nodes[0].localToWorldMatrix.GetPosition();
        }
    }

    private Vector2 GetPlayerPos()
    {
        return _player.transform.position;
    }

    public override void Die()
    {
        _currentSpeed = 0;
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.E_Guard_Death, transform.position);
        FrontCol.SetActive(false);
        RearCol.SetActive(false);
        UpperCol.SetActive(false);  
        LowerCol.SetActive(false);
        base.Die();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(_refPoint.position, _refPoint.position + transform.right * _data.Range * _direction);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position + transform.right * -_direction + transform.up*.5f, Vector2.down *.5f);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(_refPoint.position, _target);
        Gizmos.color = Color.green;
    }
}
