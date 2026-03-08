using System.Collections;
using UnityEngine;

public class BL4ZT : Enemy
{
    private Arachnomadre _arachnomadre;
    public void SetArachnomadre(Arachnomadre boss) => _arachnomadre = boss;

    #region VARIABLES
    //EXTRAS
    [SerializeField] Transform _spriteContainer;
    private float groundCheckDistance = 0.05f;
    private float groundCheckOffset = 0.15f;

    [Header("Movement")]
    [SerializeField] private float _baseSpeed;
    private float wallCheckDistance = 0.2f;
    private bool _goingRight = true;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed;
    private float _pivotDistance = 0.05f;
    private float turnSign;
    private bool isTurning;
    private float targetAngle;
    private Vector2 pivotPoint;

    [Header("Navigation")]
    [SerializeField][Range(1, 2)] float _speedMultiplier;
    [SerializeField] Transform[] _nodes;
    [SerializeField] float _rayCD;
    private float _currentSpeed;
    private float _lastRay;
    private Vector2 _destination;
    private Vector2 currentNormal = Vector2.up;
    private bool _isWaiting = false;
    private int _currentNodeIndex = 0;
    private bool _isRoaming = false;

    [Header("Explostion")]
    [SerializeField] float _timeToExplode;
    [SerializeField] float _explosionRadius;
    private float _activationTime;
    private bool _isActive;
    private bool _hasExploded = false;
    #endregion

    #region SURFACE DETECTION
    private bool DetectGround(float offset, out RaycastHit2D hit)
    {
        Vector3 origin =
            transform.position +
            GetMovementDir() * offset;

        Vector2 direction = -transform.up;

        hit = Physics2D.Raycast(origin, direction, groundCheckDistance, _obstaclesLayer);
        Debug.DrawRay(origin, direction * groundCheckDistance, Color.red);

        return hit.collider != null;
    }

    private bool DetectWall(out RaycastHit2D hit)
    {
        Vector3 origin = transform.position + transform.up;
        Vector2 direction = GetMovementDir();

        hit = Physics2D.Raycast(origin, direction, wallCheckDistance, _obstaclesLayer);
        Debug.DrawRay(origin, direction * wallCheckDistance, Color.red);

        return hit.collider != null;
    }

    #endregion

    #region MOVEMENT & ALIGNMENT
    private bool HandleMovement(bool groundFront, RaycastHit2D frontHit, bool groundBack, RaycastHit2D backHit, bool wallAhead)
    {
        //ALIGNMENT
        AlignToSurface(groundFront ? frontHit.normal : backHit.normal);

        //ROTATION
        if (CheckCorner(groundFront, groundBack, wallAhead))
        {
            return true;
        }

        //MOVEMENT

        MoveAlongSurface();
        SnapToSurface();
        return false;
    }

    private bool CheckCorner(bool groundFront, bool groundBack, bool wallAhead)
    {
        // CLOSE CORNER
        if (wallAhead && groundFront && groundBack)
        {
            Vector2 newNormal =
                new Vector2(-currentNormal.y, currentNormal.x);

            StartTurn(newNormal, true);
            return true;
        }

        // OPEN CORNER
        if (!wallAhead && groundBack && !groundFront)
        {
            Vector2 newNormal =
                new Vector2(currentNormal.y, -currentNormal.x);

            StartTurn(newNormal, false);
            return true;
        }

        return false;
    }

    private void AlignToSurface(Vector2 normal)
    {
        currentNormal = normal;

        Vector2 tangent = new Vector2(normal.y, -normal.x);

        if (Vector2.Dot(tangent, transform.right) < 0)
            tangent = -tangent;

        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void MoveAlongSurface()
    {
        transform.position += GetMovementDir() * _currentSpeed * Time.deltaTime;
    }

    private void SnapToSurface()
    {
        Vector2 origin = transform.position;
        Vector2 direction = -transform.up;

        RaycastHit2D hit =
            Physics2D.Raycast(origin, direction, 2f, _obstaclesLayer);

        if (!hit.collider)
            return;

        float delta = hit.distance;

        transform.position -= (Vector3)transform.up * delta;
    }
    #endregion

    #region ROTATION
    private void StartTurn(Vector2 newNormal, bool isClosedCorner)
    {
        isTurning = true;
        currentNormal = newNormal;

        turnSign = _goingRight ? (isClosedCorner ? 1f : -1f) : (isClosedCorner ? -1f : 1f);

        Vector2 tangent = _goingRight
            ? new Vector2(newNormal.y, -newNormal.x)
            : new Vector2(-newNormal.y, newNormal.x);

        targetAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

        pivotPoint = (Vector2)transform.position - (Vector2)transform.up * _pivotDistance;
    }

    private void UpdateTurn()
    {
        float current = transform.eulerAngles.z;

        float diff = Mathf.DeltaAngle(current, targetAngle);

        float step = rotationSpeed * Time.deltaTime * turnSign;

        if (Mathf.Sign(diff) != Mathf.Sign(step) || Mathf.Abs(step) >= Mathf.Abs(diff))
        {
            step = diff;
            isTurning = false;
        }

        transform.RotateAround(pivotPoint, Vector3.forward, step);

        transform.position += GetMovementDir() * _currentSpeed * Time.deltaTime;

        if (!isTurning)
        {
            SnapToSurface();
        }
    }
    #endregion
    
    #region AI
    private bool HasLOS()
    {
        Vector2 dirToPlayer = (_player.transform.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, _data.Range, _playerLayer);
        return hit;

    }

    private bool CheckTarget(Vector3 target)
    {
        bool hasReachedTarget = Vector3.Distance(target, transform.position) <= _explosionRadius / 2;
        _animator.SetBool("IsMoving", !hasReachedTarget);
        return hasReachedTarget;
    }

    private void SetDirToTarget()
    {
        Vector3 localTargetPos = transform.InverseTransformPoint(_destination);
        
        float threshold = 0.05f;
        
        if (Mathf.Abs(localTargetPos.x) > threshold)
        {
            _goingRight = localTargetPos.x > 0;
        }
        Vector3 newScale = _spriteContainer.localScale;
        newScale.x = _goingRight ? -Mathf.Abs(newScale.x) : Mathf.Abs(newScale.x);
        _spriteContainer.localScale = newScale;
    }

    private IEnumerator SetPatrolTarget()
    {
        _isWaiting = true;
        _rb.linearVelocityX = 0;
        _animator.SetBool("IsMoving", false);

        yield return new WaitForSeconds(0.5f);
        IncreaseNodeIndex();
        Transform _nextNode = _nodes[_currentNodeIndex];
        
        _destination = GetClosestPoint(_nextNode.position);
        SetDirToTarget();

        yield return new WaitForSeconds(.5f);
        _animator.SetBool("IsMoving", true);
        _isWaiting = false;
    }

    private Vector2 GetClosestPoint(Vector2 origin)
    {
        _lastRay = Time.time;
        Vector2 closestPoint = origin;
        float disToClosestSurface = float.MaxValue;

        for (int n = 0; n < 4; n++)
        {
            Vector2 dirToCast = GetDirectionByIndex(n);
            RaycastHit2D hit = Physics2D.Raycast(origin, dirToCast, 15, base._obstaclesLayer);
            float disToCurrent = Vector2.Distance(origin, hit.point);

            if (disToClosestSurface == 0 || disToClosestSurface > disToCurrent)
            {
                disToClosestSurface = disToCurrent;
                closestPoint = hit.point;
            }
        }
        return closestPoint;
    }

    #endregion

    #region COMBAT
    private void Activate()
    {
        _isActive = true;
        _activationTime = Time.time;
        if (CheckCooldown(_rayCD, _lastRay))
        {
            _destination = GetClosestPoint(_player.transform.position);
        }
        SetRoaming(false);
        _currentSpeed *= _speedMultiplier;
        _animator.SetTrigger("OnActivated");
        AudioService.Instance.StopSFX(_audioContext.Audio.idle);
        AudioService.Instance.StopSFX(_audioContext.Audio.move);
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.detection, transform.position);
        _animator.SetBool("IsActive", true);
    }
    private void Explode()
    {
        _hasExploded = true;
        _col.enabled = false;
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, _explosionRadius);
        int bl4ztKills = 0;

        foreach (Collider2D col in cols)
        {
            if (col.CompareTag("Player"))
            {
                col.TryGetComponent(out NewController player);
                player.Die();
            }
            else if (col.CompareTag("Enemy") && col.TryGetComponent(out Enemy enemy) && enemy != this)
            {
                enemy.Die();
                bl4ztKills++;
            }
            else if (col.TryGetComponent(out BreakableProp prop))
            {
                prop.Break();
            }
            else if (_arachnomadre != null && col.CompareTag("Boss"))
            {
                _arachnomadre.StartCoroutine(_arachnomadre.GetVulnerable());
            }
        }

        if (bl4ztKills > 0)
        {
            GameEvents.RaiseBL4ZTExplosionKills(bl4ztKills);
        }

        Die();
    }

    private bool CheckExplosionTime()
    {
        return Time.time >= _activationTime + _timeToExplode;
    }

    public override void Die()
    {
        if (_arachnomadre != null)
        {
            _arachnomadre.DecreaseEggsAmount();
        }
        _animator.SetTrigger("OnHit");
        AudioService.Instance.StopSFX(_audioContext.Audio.charge);
        AudioService.Instance.StopSFX(_audioContext.Audio.idle);
        base.Die();
        Destroy(transform.parent.gameObject, .6f);
    }
    #endregion

    #region UTILS
    public void SetRoaming(bool newValue)
    {
        _isRoaming = newValue;
        if(newValue)
        {
            SetRandomDirection();
        }
    }
    private Vector3 GetMovementDir() => _goingRight ? transform.right : -transform.right;

    private void SetRandomDirection()
    {
        _goingRight = Random.value > 0.5f;
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
    private void IncreaseNodeIndex()
    {
        _currentNodeIndex++;
        if (_currentNodeIndex >= _nodes.Length)
        {
            _currentNodeIndex = 0;
        }
    }
    #endregion

    #region MAGIC METHODS

    protected override void Awake()
    {
        base.Awake();
        _currentSpeed = _baseSpeed;
        if(!_isRoaming)
        {
            StartCoroutine(SetPatrolTarget());
        }
        else
        {
            SetRandomDirection();
        }
    }
    public override void CustomUpdate()
    {
        if (isTurning)
        {
            UpdateTurn();
            return;
        }

        bool groundFront = DetectGround(groundCheckOffset, out RaycastHit2D frontHit);
        bool groundBack = DetectGround(-groundCheckOffset, out RaycastHit2D backHit);
        bool wallAhead = DetectWall(out RaycastHit2D wallHit);

        if (!groundFront && !groundBack)
        {
            return;
        }

        if(!_isActive)
        {
            if (HasLOS())
            {
                Activate();
            }
            else
            {
                if (!CheckTarget(_destination) || _isRoaming)
                {
                    AudioService.Instance.StopSFX(_audioContext.Audio.idle);
                    AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.move, transform.position);
                    if (HandleMovement(groundFront, frontHit, groundBack, backHit, wallAhead))
                    {
                        return;
                    }
                }
                else if(!_isWaiting)
                {
                    AudioService.Instance.StopSFX(_audioContext.Audio.move);
                    AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.idle, transform.position);
                    if (!_isRoaming)
                    {
                        StartCoroutine(SetPatrolTarget());
                    }   
                }
            }
        }
        else
        {
            if (!CheckExplosionTime())
            {
                if (CheckCooldown(_rayCD, _lastRay))
                {
                    _destination = GetClosestPoint(_player.transform.position);
                    SetDirToTarget();
                }

                if (!CheckTarget(_destination))
                {
                    _animator.SetBool("IsMoving", true);
                    AudioService.Instance.StopSFX(_audioContext.Audio.idle);
                    AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.charge, transform.position);
                    if (HandleMovement(groundFront, frontHit, groundBack, backHit, wallAhead))
                    {
                        return;
                    }
                }
                else
                {
                    AudioService.Instance.StopSFX(_audioContext.Audio.charge);
                    AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.idle, transform.position);
                    _rb.linearVelocityX = 0;
                    _animator.SetBool("IsMoving", false);
                }
            }
            else if(!_hasExploded)
            {
                Explode();
            }
        }
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, _destination);
        Gizmos.DrawSphere(transform.position + new Vector3(0, -_pivotDistance), 0);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _data.Range);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}

