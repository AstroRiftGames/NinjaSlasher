using System;
using Unity.VisualScripting;
using UnityEngine;

public class BL4ZT : RangeEnemy
{
    private Arachnomadre _arachnomadre;
    public void SetArachnomadre(Arachnomadre boss) => _arachnomadre = boss;
    [SerializeField] float _speed;
    [SerializeField] float _timeToExplode;
    [SerializeField] float _explosionRadius;
    [SerializeField][Range(1, 2)] float _speedMultiplier;
    [SerializeField] Transform[] _nodes;
    [SerializeField] float _rayCD;

    private float _currentSpeed;
    private float _activationTime;
    private bool _isActive;
    private Vector2 _destination;
    private Surface _currentSurface = Surface.Floor;
    private float _lastRay;

    private bool MovingRight()
    {
        Vector2 currentPos = transform.position;

        Debug.DrawLine(currentPos, _destination);

        bool movingRight = _currentSurface switch
        {
            Surface.Ceiling => _destination.x < currentPos.x,
            Surface.Right_Wall => _destination.y > currentPos.y,
            Surface.Floor => _destination.x > currentPos.x,
            Surface.Left_Wall => _destination.y < currentPos.y,
            _ => throw new IndexOutOfRangeException($"Surface: {_currentSurface}"),
        };
        Debug.DrawRay(currentPos, transform.right * (movingRight ? 1 : -1));
        return movingRight;
    }

    protected override void Awake()
    {
        base.Awake();
        _currentSpeed = _speed;
        SetPatrolTarget();
    }
    public override void CustomUpdate()
    {
        UpdateTarget();
        if(!_isActive)
        {
            if (!_hasLOS)
            {
                if (!CheckTarget(_destination)) Move();
                else
                {
                    _animator.SetBool("IsMoving", false);
                    SetPatrolTarget();
                }
            }
            else Activate();
        }
        else
        {
            if(!CheckExplosionTime())
            {
                if(CheckCooldown(_rayCD, _lastRay)) _destination = GetClosestPoint(_player.transform.position);
                if(!CheckTarget(_destination)) Move();
                else _animator.SetBool("IsMoving", false);
            }
            else Explode();
        }

    }

    private void Move()
    {
        bool thereIsWall = Physics2D.Raycast(transform.position + transform.right * .5f * (MovingRight() ? 1 : -1), transform.right * (MovingRight() ? 1 : -1), .25f, _obstaclesLayer);
        Debug.DrawRay(transform.position + transform.right * .5f * (MovingRight() ? 1 : -1), transform.right * .25f * (MovingRight() ? 1 : -1));
        if (thereIsWall)
        {
            Rotate();
        }

        bool thereIsFloor = Physics2D.Raycast(transform.position + transform.right * .4f * (MovingRight() ? 1:-1) + transform.up * -1f, transform.up * -1, .5f, _obstaclesLayer);
        Debug.DrawRay(transform.position + transform.right * .4f * (MovingRight() ? 1 : -1) + transform.up * -1f, transform.up * -1 *.5f);
        if (thereIsFloor)
        {
            transform.position += transform.right * (MovingRight() ? 1 : -1) * _currentSpeed * Time.deltaTime;
        }
        _animator.SetBool("IsMoving", true);

    }
    private void Rotate()
    {
        _currentSurface = _currentSurface switch
        {
            Surface.Floor => MovingRight() ? Surface.Right_Wall : Surface.Left_Wall,
            Surface.Right_Wall => MovingRight() ? Surface.Ceiling : Surface.Floor,
            Surface.Ceiling => MovingRight() ? Surface.Left_Wall : Surface.Right_Wall,
            Surface.Left_Wall => MovingRight() ? Surface.Floor : Surface.Ceiling,
            _ => throw new IndexOutOfRangeException($"Surface: {_currentSurface}. Direction: {MovingRight()}")
        };
        transform.Rotate(new Vector3(0, 0, 90 * (MovingRight() ? 1 : -1)));
        _animator.SetTrigger($"OnRotation{(MovingRight() ? "Right" : "Left")}");
    }
    private void Explode()
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, _explosionRadius);
        foreach(Collider2D col in cols)
        {
            if (col.CompareTag("Player"))
            {
                col.TryGetComponent(out NewController player);
                player.Die();
            }
            else if (_arachnomadre != null && col.CompareTag("Boss"))
            {
                _arachnomadre.StartCoroutine(_arachnomadre.GetVulnerable());
            }
        }
        Die();
    }

    public override void Die()
    {
        if(_arachnomadre != null)
        {
            _arachnomadre.DecreaseEggsAmount();
        }
        base.Die();
        Destroy(transform.parent.gameObject,.5f);
    }

    private bool CheckTarget(Vector3 target)
    {
        return Vector3.Distance(target, transform.localToWorldMatrix.GetPosition()) <= _explosionRadius/2;
    }

    private bool CheckExplosionTime()
    {
        return Time.time >= _activationTime + _timeToExplode;
    }

    private void Activate()
    {
        _isActive = true;
        _activationTime = Time.time;
        if(CheckCooldown(_rayCD, _lastRay)) _destination = GetClosestPoint(_player.transform.position);
        _currentSpeed *= _speedMultiplier;
    }

    private void SetPatrolTarget()
    {
        _rb.linearVelocityX = 0;
        if (_destination == (Vector2)_nodes[0].localToWorldMatrix.GetPosition())
        {
            _destination = _nodes[1].localToWorldMatrix.GetPosition();
        }
        else
        {
            _destination = _nodes[0].localToWorldMatrix.GetPosition();
        }
    }

    private Vector2 GetClosestPoint(Vector2 origin)
    {
        Debug.Log("Getting Point");
        _lastRay = Time.time;
        Vector2 closestPoint = origin;
        float disToClosestSurface = float.MaxValue;

        for (int n = 0; n < 4; n++)
        {
            Vector2 dirToCast = GetDirectionByIndex(n);
            RaycastHit2D hit = Physics2D.Raycast(origin, dirToCast, 15, _obstaclesLayer);
            if (hit != false) Debug.DrawLine(origin, hit.point, Color.red, .25f);
            float disToCurrent = Vector2.Distance(origin, hit.point);

            if (disToClosestSurface == 0 || disToClosestSurface > disToCurrent)
            {
                disToClosestSurface = disToCurrent;
                closestPoint = hit.point;
            }
        }
        return closestPoint;
    }

    private Vector2 GetDirectionByIndex(int index)
    {
        return index switch
        {
            0 => Vector2.right,
            1 => Vector2.left,
            2 => Vector2.up,
            3 => Vector2.down,
            _ => throw new System.IndexOutOfRangeException(),
        };
    }

    private bool CheckCooldown(float cd, float last) => Time.time >= cd + last;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 5);
    }
#endif

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up);
    }
}
