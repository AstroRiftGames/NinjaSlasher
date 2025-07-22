using System.Collections;
using UnityEngine;

public class GuardBot : Enemy
{
    [SerializeField] Transform _refPoint;
    [SerializeField] LayerMask _playerLayer;
    [SerializeField] LayerMask _scenarioLayer;
    [SerializeField] float _speed;
    [SerializeField][Range(1, 2)] float _speedMultiplier;
    [SerializeField] Transform[] _nodes;

    private float _currentSpeed;
    private bool _isPushing;
    private Vector2 _target;

    private float _direction => transform.localScale.x > 0 ? 1 : -1;

    public override void Awake()
    {
        base.Awake();
        _currentSpeed = _speed;
    }
    public virtual void Update()
    {
        if (!CheckDistanceToTarget(_target)) 
        {
            Move();
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
                Debug.Log($"Set Patrol {_target}");
            }
        }
    }

    private void Move()
    {
        transform.localScale = new Vector3(_target.x > transform.localToWorldMatrix.GetPosition().x ? 1 : -1, transform.localScale.y, transform.localScale.z);

        bool thereIsFloor = Physics2D.Raycast(transform.position + transform.right *.5f * _direction + Vector3.down, Vector3.down, .5f, _scenarioLayer);
        if(thereIsFloor)
        {
            _rb.linearVelocityX = _direction * _currentSpeed;
        }
    }

    private bool CheckDistanceToTarget(Vector2 target)
    {
        return Vector2.Distance(target, transform.localToWorldMatrix.GetPosition()) <= .5f;
    }

    private bool CheckTarget()
    {
        return Physics2D.Raycast(_refPoint.position, transform.right * _direction, _data.Range, _playerLayer);
        
    }

    private IEnumerator Push()
    {
        _isPushing = true;
        _target = GetPlayerPos();
        _currentSpeed *= _speedMultiplier;
        Debug.Log("Start Push");
        yield return new WaitForSeconds(2f);
        _currentSpeed = _speed;
        _isPushing = false;
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
        Vector2 pos = _player.transform.position;
        pos.y = transform.position.y;

        return pos;
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(_refPoint.position, _refPoint.position + transform.right * _data.Range * _direction);
        Gizmos.DrawLine(transform.position + transform.right *.5f * _direction + Vector3.down, transform.position + transform.right * .5f * _direction + Vector3.down*.5f);
    }
}
