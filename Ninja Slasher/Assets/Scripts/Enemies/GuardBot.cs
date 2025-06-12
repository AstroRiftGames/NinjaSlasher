using System.Collections;
using TMPro.EditorUtilities;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Rendering;

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
    public override void Update()
    {
        if(!CheckTarget(_target))
        {
            Move();
        }
        if (!_isPushing)
        {
            if (CheckTarget())
            {
                AlternatePush();
                _target = GetPlayerPos();
                Debug.Log("Push on");
            }
            else
            {
                if (_target == Vector2.zero || CheckTarget(_target))
                {
                    SetPatrolTarget();
                    Debug.Log($"Set Patrol {_target}");
                }
            }
        }
        else
        {
            if (CheckTarget(_target))
            {
                AlternatePush();
                _target = GetPlayerPos();
                Debug.Log("Push off");
            }
        }
    }

    private void Move()
    {
        transform.localScale = new Vector3(_target.x > transform.position.x ? 1 : -1, transform.localScale.y, transform.localScale.z);

        bool thereIsFloor = Physics2D.Raycast(transform.position + transform.right *.5f * _direction + Vector3.down, Vector3.down, .5f, _scenarioLayer);
        if(thereIsFloor)
        {
            _rb.linearVelocity = transform.right * _direction * _currentSpeed;
        }
        else
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }

    private bool CheckTarget(Vector2 target)
    {
        return Vector2.Distance(target, transform.position) <= .2f;
    }

    private bool CheckTarget()
    {
        return Physics2D.Raycast(_refPoint.position, transform.right * _direction, _data.Range, _playerLayer);
        
    }

    private void AlternatePush()
    {
        _isPushing = !_isPushing;
        _currentSpeed = _speed * (_isPushing ? _speedMultiplier: 1);
    }

    private void SetPatrolTarget()
    {
        _rb.linearVelocity = Vector2.zero;
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
