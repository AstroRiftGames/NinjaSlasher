using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Arachnomadre : BossEnemy
{

    #region VARIABLES
    //EXTRAS
    [SerializeField] private ArachnomadreAudioContext _arachnomadreAudioContext;
    [SerializeField] ArachnomadreAudioSet _audioSet;
    [SerializeField] Transform _spriteContainer;
    private float _verticalOffset = .65f;
    private float _horizontalOffset = .6f;
    private float groundCheckOffset = .15f;
    private float groundCheckDistance = 0.15f;

    [Header("Movement")]
    [SerializeField] private float _speed;
    [SerializeField][Range(0f, 1f)] private float _dirChangeChance;
    private float wallCheckDistance = 0.15f;
    private bool _goingRight = true;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed;
    private float _pivotDistance = 0.05f;
    private float turnSign;
    private bool isTurning;
    private float targetAngle;
    private Vector2 pivotPoint;

    [Header("Navigation")]
    private Vector2 currentNormal = Vector2.up;

    [Header("COMBAT")]
    [Header("   Stats")]
    [SerializeField] private float _maxAttackCD;
    [SerializeField] private float _minAttackCD;
    private bool _isAttacking;
    private float _attackCD = 3f;
    private float _lastAttack = 0;
    [Space]
    [Header("   EggSpawn")]
    [SerializeField] private float _launchingBaseForce;
    [SerializeField] private float _timeBetweenEggs;
    [SerializeField][Range(0f, 1f)] private float _spawnAttackChance;
    [SerializeField] private BlaztEgg _blaztEgg;
    [SerializeField] private int _blaztEggsAmount;
    [SerializeField] private Transform _spawnRefPoint;
    private ObjectPool<BlaztEgg> _pool;
    public ObjectPool<BlaztEgg> Pool => _pool;
    private int _blaztsAmount;
    [Space]
    [Header("   Furtive")]
    [SerializeField] private float _hidingTime;
    [SerializeField] private float _submergingTime;
    [SerializeField] private float _emergingTime;
    [SerializeField] private float _biteRadius;
    [SerializeField] private Transform _biteRefPoint;
    [Space]
    [Header("Vulnerability")]
    [SerializeField] float _vulnerabilityTime;
    private bool _isVulnerable;

    #endregion

    #region SURFACE DETECTION
    private bool DetectGround(float offset, out RaycastHit2D hit)
    {
        Vector3 origin =
            transform.position +
            GetMovementDir() * offset +
            -transform.up * _verticalOffset;

        Vector2 direction = -transform.up;

        hit = Physics2D.Raycast(origin, direction, groundCheckDistance, _obstaclesLayer);

        return hit.collider != null;
    }

    private bool DetectWall(out RaycastHit2D hit)
    {
        Vector3 origin = transform.position + transform.up + GetMovementDir() * _horizontalOffset;
        Vector2 direction = GetMovementDir();

        hit = Physics2D.Raycast(origin, direction, wallCheckDistance, _obstaclesLayer);

        return hit.collider != null;
    }

    #endregion

    #region COLLISION DETECTION
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (_isVulnerable)
            {
                Die();
            }
            else
            {
                collision.gameObject.TryGetComponent(out NewController player);
                player.Die();
            }
        }
    }
    #endregion

    #region MOVEMENT & ALIGNMENT
    private bool HandleMovement(bool groundFront, RaycastHit2D frontHit, bool groundBack, RaycastHit2D backHit, bool wallAhead)
    {
        //ALIGNMENT
        AlignToSurface(groundFront ? frontHit.normal : backHit.normal);
        SetDirToTarget();

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
        transform.position += GetMovementDir() * _speed * Time.deltaTime;
    }

    private void SnapToSurface()
    {
        Vector3 origin =
            transform.position -
            transform.up * _verticalOffset/2;

        Vector2 direction = -transform.up;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, groundCheckDistance, _obstaclesLayer);

        if (!hit.collider)
            return;

        float delta = hit.distance;

        transform.position -= transform.up * delta;
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

        transform.position += GetMovementDir() * _speed * Time.deltaTime;

        if (!isTurning)
        {
            SnapToSurface();
        }
    }
    #endregion

    #region COMBAT
    private void PrepareAttack()
    {
        _isAttacking = true;
        SetCD();
        SetMovementDirection();
        Attack();
        _lastAttack = Time.time;
    }

    private void SetMovementDirection()
    {
        float r = Random.Range(0f, 1f);
        if (r < _dirChangeChance) _goingRight = !_goingRight;
    }

    private void Attack()
    {
        float r = Random.Range(0f, 1f);
        float chance = _spawnAttackChance / _blaztsAmount;
        if (r <= chance) StartCoroutine(SpawnAttack());
        else StartCoroutine(FurtiveAttack());
    }
    #region SPAWN ATTACK
    private IEnumerator SpawnAttack()
    {
        _animator.SetTrigger("OnEggSpawn");
        yield return new WaitForSeconds(1.25f);
        for (int n = 0; n < _blaztEggsAmount; n++)
        {
            BlaztEgg newEgg = _pool.Get();


            newEgg.transform.SetPositionAndRotation(
                _spawnRefPoint.position + transform.up,
                Quaternion.identity
            );

            newEgg.TryGetComponent(out Rigidbody2D eggRB);
            eggRB.AddForce(SetDirection(n), ForceMode2D.Impulse);



            newEgg.SetArachnomadre(this);


            newEgg.OnRequestDespawn -= HandleEggDespawn;
            newEgg.OnRequestDespawn += HandleEggDespawn;

            yield return new WaitForSeconds(_timeBetweenEggs);
        }
        yield return new WaitForSeconds(1.35f);
        _isAttacking = false;
    }

    private void HandleEggDespawn(BlaztEgg egg)
    {
        egg.OnRequestDespawn -= HandleEggDespawn;
        _pool.Release(egg);
    }

    private Vector2 SetDirection(int index)
    {
        Vector2 dir = transform.up;
        Vector2 up = transform.TransformDirection(transform.up);
        float force = _launchingBaseForce;
        if (up == Vector2.up || up == Vector2.down)
        {
            dir.x -= index switch
            {
                0 => Random.Range(0, 5f),
                1 => Random.Range(-2f, 2f),
                2 => Random.Range(-5, 0),
                _ => Random.Range(-2f, 2f),
            };
        }
        else
        {
            force *= index switch
            {
                0 => Random.Range(.25f, .75f),
                1 => Random.Range(1, 1.5f),
                2 => Random.Range(1.75f, 2.25f),
                _ => Random.Range(.25f, .75f),
            };
        }
        return dir.normalized * force;
    }
    #endregion
        #region FURTIVE ATTACK
    private IEnumerator FurtiveAttack()
    {
        _animator.SetTrigger("OnSubmerge");
        _col.enabled = false;
        yield return new WaitForSeconds(_submergingTime + _hidingTime / 2);

        //GET CLOSEST POINT TO PLAYER
        RaycastHit2D hit = GetClosestPoint(_player.position);
        
        //SET NEW ROTATION
        float angle = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg - 90;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        //SET NEW POSITION
        transform.position = hit.point + (Vector2)transform.up * (_verticalOffset + groundCheckDistance);
        SnapToSurface();

        yield return new WaitForSeconds(_hidingTime/2);

        _animator.SetTrigger("OnEmerge");

        yield return new WaitForSeconds(_emergingTime);

        _col.enabled = true;
        _isAttacking = false;
    }

    public void Bite()
    {
        Collider2D playerCol = Physics2D.OverlapCircle(_biteRefPoint.position, _biteRadius, _playerLayer);
        if (playerCol == null) return;
        playerCol.TryGetComponent(out NewController controller);
        controller.Die();
    }

    #endregion
    #endregion

    #region UTILS
    private RaycastHit2D GetClosestPoint(Vector2 origin)
    {
        float disToClosestSurface = float.MaxValue;
        RaycastHit2D hit = new RaycastHit2D();
        for (int n = 0; n < 4; n++)
        {
            Vector2 dirToCast = GetDirectionByIndex(n);
            RaycastHit2D currentHit = Physics2D.Raycast(origin, dirToCast, 15, _obstaclesLayer);
            float disToCurrent = Vector2.Distance(origin, currentHit.point);

            if (disToClosestSurface == 0 || disToClosestSurface > disToCurrent)
            {
                disToClosestSurface = disToCurrent;
                hit = currentHit;
            }
        }
        Debug.DrawLine(origin, hit.point, Color.red, 2f);
        return hit;
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
    private Vector3 GetMovementDir() => _goingRight ? transform.right : -transform.right;
    
    private void SetDirToTarget()
    {
        Vector3 localTargetPos = transform.InverseTransformPoint(_player.position);

        float threshold = 0.05f;
        if (Mathf.Abs(localTargetPos.x) > threshold)
        {
            bool lookingRight = localTargetPos.x > 0;

            Vector3 newScale = _spriteContainer.localScale;
            newScale.x = lookingRight ? -Mathf.Abs(newScale.x) : Mathf.Abs(newScale.x);
            _spriteContainer.localScale = newScale;
        }
    }
    private IEnumerator Activate()
    {
        _isWaiting = true;
        yield return new WaitForSeconds(_waitTime);
        _isWaiting = false;
    }

    protected override void InitializeAudioContext()
    {
        if (_arachnomadreAudioContext != null)
            _arachnomadreAudioContext.Initialize(_audioSet);
    }

    private bool CheckCD(float cd, float last) => Time.time >= cd + last;
    private void SetCD() => _attackCD = Random.Range(_minAttackCD, _maxAttackCD);
    public void DecreaseEggsAmount() => _blaztsAmount--;
    public void IncreaseEggsAmount() => _blaztsAmount++;
    #endregion

    #region VULNERABILITY MANAGEMENT
    private bool SetVulnerability(bool value) => _isVulnerable = value;

    public IEnumerator GetVulnerable()
    {
        SetVulnerability(true);
        _animator.SetTrigger("OnHit");
        yield return new WaitForSeconds(_vulnerabilityTime);
        SetVulnerability(false);
        _animator.SetTrigger("OnRecovery");
    }
    #endregion

    #region MAGIC METHODS

    protected override void Awake()
    {
        base.Awake();
        InitializeAudioContext();
        _pool = new ObjectPool<BlaztEgg>(_blaztEgg, _blaztEggsAmount, transform);
        StartCoroutine(Activate());
    }
    public override void CustomUpdate()
    {
        if (!_isVulnerable && !_isWaiting)
        {
            if (isTurning)
            {
                UpdateTurn();
                return;
            }

            bool groundFront = DetectGround(groundCheckOffset, out RaycastHit2D frontHit);
            bool groundBack = DetectGround(-groundCheckOffset, out RaycastHit2D backHit);
            bool wallAhead = DetectWall(out RaycastHit2D wallHit);

            if(!groundFront && !groundBack)
            {
                return;
            }

            if (CheckCD(_attackCD, _lastAttack) && !_isAttacking)
            {
                PrepareAttack();
                _animator.SetBool("IsMoving", false);
            }
            else if (!_isAttacking)
            {
                _animator.SetBool("IsMoving", true);
                HandleMovement(groundFront, frontHit, groundBack, backHit, wallAhead);
            }
        }
    }
    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_biteRefPoint.position, _biteRadius);
    }
}

