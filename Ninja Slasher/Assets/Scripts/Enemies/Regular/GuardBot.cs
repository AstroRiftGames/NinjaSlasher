using System.Collections;
using UnityEngine;

public class GuardBot : Enemy
{
    [SerializeField] Transform _refPoint;
    [SerializeField] GuardBotShield _shield;
    [SerializeField] float _speed;
    [SerializeField][Range(1, 2)] float _speedMultiplier;
    [SerializeField] Transform[] _nodes;

    [SerializeField] protected GameObject FrontCol;

    private float _currentSpeed;
    private bool _isPushing;
    public bool IsPushing => _isPushing;
    private Vector2 _target;

    private float _timeToReachTarget;
    private float _patrolTimer;

    private bool _isDying = false;

    private float _direction => transform.localScale.x > 0 ? 1 : -1;
    [SerializeField] float _resetDelay = 5f;
    [SerializeField] private LayerMask _layerMask;

    public override void OnEnable()
    {
        base.OnEnable();
        _shield.OnCollision += ManageCollision;
    }
    public override void OnDisable()
    {
        base.OnDisable();
        _shield.OnCollision -= ManageCollision;

    }
    protected override void Awake()
    {
        base.Awake();
        _currentSpeed = _speed;
    }

    public override void CustomUpdate()
    {
        if (GameManager.Instance.PlayerHasDied) return;

        if (!_isPushing)
        {
            _patrolTimer += Time.deltaTime;
        }

        bool reachedTarget = CheckDistanceToTarget(_target);
        if (!reachedTarget) 
        {
            TryMove();
        }

        if (!_isPushing)
        {
            if (CheckTarget())
            {
                StartCoroutine(Push());
            }
            else if (_target == Vector2.zero || reachedTarget)
            {
                SetPatrolTarget();
            }
        }
        else if (reachedTarget)
        {
            _animator.SetTrigger("OnTargetReached");
            StopAllCoroutines();
            _currentSpeed = _speed;
            _isPushing = false;
            _animator.SetBool("IsIdle", true);
            SetPatrolTarget();
        }
    }

    private void TryMove()
    {
        transform.localScale = new Vector3(_target.x > transform.localToWorldMatrix.GetPosition().x ? 1 : -1, transform.localScale.y, transform.localScale.z);

        bool thereIsFloor = Physics2D.Raycast(_refPoint.position + transform.right * _direction + Vector3.down, Vector3.down, .75f, _obstaclesLayer);
        Debug.DrawRay(_refPoint.position + transform.right * _direction + Vector3.down, Vector3.down * .75f, Color.blue);

        bool thereIsObstacleTop = Physics2D.Raycast(_refPoint.position + transform.up * .8f, transform.right * _direction, 1.5f, _obstaclesLayer);
        bool thereIsObstacleMid = Physics2D.Raycast(_refPoint.position, transform.right * _direction, 1.5f, _obstaclesLayer);
        bool thereIsObstacleBottom = Physics2D.Raycast(_refPoint.position - transform.up * .8f, transform.right * _direction, 1.5f, _obstaclesLayer);

        if (!_isDying && thereIsFloor && !(thereIsObstacleTop || thereIsObstacleMid || thereIsObstacleBottom))
        {
            _rb.linearVelocityX = _direction * _currentSpeed;
            if (!_isPushing)
            {
                _animator.SetBool("IsIdle", false);
            }
        }
        else
        {
            if(_isPushing)
            {
                _animator.SetTrigger("OnFloorEnd");
                _animator.SetBool("IsIdle", true);
            }
            else 
            {
                _animator.SetBool("IsIdle", true);
                if (_patrolTimer >= _timeToReachTarget && _timeToReachTarget > 0)
                {
                    SetPatrolTarget();
                }
            }
            _rb.linearVelocityX = 0;
        }
    }

    private void ManageCollision(GameObject other)
    {
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.collision, transform.position);
        if(other.tag == "Player")
        {
            other.TryGetComponent(out NewController controller);
            controller.Die();
        }
    }

    private bool CheckDistanceToTarget(Vector2 target)
    {
        float distance = Mathf.Abs(target.x - _refPoint.localToWorldMatrix.GetPosition().x);
        bool reached =  distance <= .5f;
        return reached;
    }

    private bool CheckTarget()
    {
        bool topHit = false;
        RaycastHit2D topRay = Physics2D.Raycast(_refPoint.position + transform.up, transform.right * _direction, _data.Range, _layerMask); 
        if (topRay)
        {
            topHit = topRay.collider.CompareTag("Player");
        }

        bool midHit = false;
        RaycastHit2D midRay = Physics2D.Raycast(_refPoint.position, transform.right * _direction, _data.Range, _layerMask);
        if (midRay)
        {
            midHit = midRay.collider.CompareTag("Player");
        }

        bool bottomHit = false;
        RaycastHit2D bottomRay = Physics2D.Raycast(_refPoint.position + -transform.up, transform.right * _direction, _data.Range, _layerMask);
        if (bottomRay)
        {
            bottomHit = bottomRay.collider.CompareTag("Player");
        }
        return topHit || midHit || bottomHit;
    }

    private IEnumerator Push()
    {
        _target = GetPlayerPos();
        _isPushing = true;
        _animator.SetTrigger("OnDetection");
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.detection, transform.position);
        _currentSpeed = 0;
        yield return new WaitForSeconds(1f);
        _animator.SetTrigger("OnPushStart");
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.charge, transform.position);
        _currentSpeed = _speed * _speedMultiplier;
        yield return new WaitForSeconds(2f);
        _isPushing = false;
        _currentSpeed = _speed;
        _animator.SetTrigger("OnPushEnd");
        _animator.SetBool("IsIdle", true);
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

        float distance = Mathf.Abs(_target.x - _refPoint.localToWorldMatrix.GetPosition().x);
        _timeToReachTarget = (distance / _speed) + 0.5f;
        _patrolTimer = 0f;
    }

    private Vector2 GetPlayerPos()
    {
        return _player.transform.position;
    }

    public override void Die()
    {
        _isDying = true;
        _currentSpeed = 0;
        StopAllCoroutines();
        AudioService.Instance.StopSFX(_audioContext.Audio.detection);
        AudioService.Instance.StopSFX(_audioContext.Audio.charge);
        FrontCol.SetActive(false);
        base.Die();
    }
}