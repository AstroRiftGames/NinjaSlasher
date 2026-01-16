using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class OM3GA : RangeEnemy
{
    [Header("Laser")]
    [SerializeField] float _laserDuration;
    [SerializeField] float _timeToShoot;
    [SerializeField] float _laserLength;

    [Header("Patrolling")]
    [SerializeField] Transform[] _nodes;
    [SerializeField] float _speed;

    private float _direction => transform.localScale.x > 0 ? 1 : -1;
    private bool _isAttacking;
    private bool _isShooting;
    private Vector2 _patrolTarget;
    private LineRenderer _ray;


    public override void Awake()
    {
        base.Awake();
        SetPatrolTarget();
        TryGetComponent(out LineRenderer ray);
        _ray = ray;
    }

    public override void Start()
    {
        base.Start();
        _ray.enabled = false;
    }
    public override void CustomUpdate()
    {
        UpdateTarget();
        _isAttacking = _hasLOS;
        if(_isAttacking || _isShooting)
        {
            _rb.linearVelocity = Vector2.zero;
            TryAttack();
        }
        else
        {
            if (!CheckTarget(_patrolTarget))
            {
                Move();
            }
            else if(CheckTarget(_patrolTarget))
            {
                SetPatrolTarget();
            }
        }
    }

    private void Move()
    {
        transform.localScale = new Vector3(_patrolTarget.x > transform.localToWorldMatrix.GetPosition().x ? 1 : -1, transform.localScale.y, transform.localScale.z);

        bool thereIsFloor = Physics2D.Raycast(transform.position + transform.right * .5f * _direction + Vector3.down, Vector3.down, .5f, _obstaclesLayer);
        if (thereIsFloor)
        {
            _rb.linearVelocityX = _direction * _speed;
        }
    }

    private bool CheckTarget(Vector2 target)
    {
        return Vector2.Distance(target, transform.localToWorldMatrix.GetPosition()) <= .5f;
    }

    public override void Attack()
    {
        SetLastAttack();
        StartCoroutine(ShootLaser());
    }

    private IEnumerator ShootLaser()
    {
        if (_player != null)
        {
            _isShooting = true;
            yield return new WaitForSeconds(_timeToShoot/2);
            Vector2 dir = (_player.position - _refPoint.position).normalized;

            yield return new WaitForSeconds(_timeToShoot/2);

            _refPoint.right = dir;

            if (_ray != null)
            {
                _ray.enabled = true;
                _ray.SetPosition(0, _refPoint.position);

                RaycastHit2D hit = Physics2D.Raycast(_refPoint.position, dir, _laserLength, _playerLayer);
                Vector3 endPoint = _refPoint.position + (Vector3)dir * _laserLength;

                if (hit.collider != null)
                {
                    endPoint = hit.point;
                    if (hit.collider.CompareTag("Player"))
                    {
                        NewController playerController = hit.collider.GetComponent<NewController>();
                        if (playerController != null)
                        {
                            playerController.Die();
                        }
                    }
                }

                _ray.SetPosition(1, endPoint);
            }
        }

        yield return new WaitForSeconds(_laserDuration);

        _ray.enabled = false;
        _isShooting = false;
    }

    private void SetPatrolTarget()
    {
        _rb.linearVelocityX = 0;
        if (_patrolTarget == (Vector2)_nodes[0].localToWorldMatrix.GetPosition())
        {
            _patrolTarget = _nodes[1].localToWorldMatrix.GetPosition();
        }
        else
        {
            _patrolTarget = _nodes[0].localToWorldMatrix.GetPosition();
        }
    }
}
