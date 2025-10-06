using Managers;
using System.Collections;
using UnityEngine;
using CandyCoded.HapticFeedback;

public class Controller : MonoBehaviour
{
    public View View => _playerView;
    [SerializeField] private View _playerView;
    public Model Model => _playerModel;
    [SerializeField] private Model _playerModel;

    private FSM<NinjaStates> _fsm;
    private ITreeNode _root;

    [Space]
    [SerializeField] float _maxAngle = 70f;
    private bool _isDashing = false;
    private float _lastDash;
    private Collider2D _currentSurface;
    private bool _lastSurfaceWasElastic = false;
    private Vector2 _lastDashDirection;
    private Vector2 _wishedDirection;
    private Vector2 lastSwipeDelta;
    private bool _isMirrored;
    private bool _isFlipped;


    [Space]
    [SerializeField] private LineRenderer swipeIndicator;
    private Vector2 swipeStart;
    private bool _startedSwipe;
    private Vector2 endTouchPosition;
    private Vector2 currentSwipe;
    private bool isSwiping = false;
    [SerializeField] private float minSwipeDistance;

    [Space]
    private bool isParrying = false;
    private float parryTimer;

    [Space]
    [SerializeField] private float checkDistance;
    [SerializeField] private LayerMask _scenarioLayer;
    [SerializeField] private LayerMask _enemyLayer;

    private bool _isDead = false;
    private bool _isInvincible;
    private bool inputEnabled = true;


    #region FSM && DECISION TREE
    public enum NinjaStates
    {
        Idle,
        Dash,
        Grab,
        Parry,
        KO
    }

    private void InitializeFSM()
    {
        var idle = new NinjaIdleState<NinjaStates>(this);
        var dash = new NinjaDashState<NinjaStates>(this);
        var grab = new NinjaGrabState<NinjaStates>(this);
        var parry = new NinjaParryState<NinjaStates>(this);
        var ko = new NinjaKOState<NinjaStates>(this);

        idle.AddTransition(NinjaStates.Dash, dash);
        idle.AddTransition(NinjaStates.Parry, parry);
        idle.AddTransition(NinjaStates.KO, ko);
        idle.AddTransition(NinjaStates.Grab, grab);

        dash.AddTransition(NinjaStates.Grab, grab);
        dash.AddTransition(NinjaStates.KO, ko);

        grab.AddTransition(NinjaStates.Dash, dash);
        grab.AddTransition(NinjaStates.Parry, parry);
        grab.AddTransition(NinjaStates.KO, ko);
        grab.AddTransition(NinjaStates.Idle, idle);

        parry.AddTransition(NinjaStates.Idle, idle);
        parry.AddTransition(NinjaStates.Grab, grab);
        parry.AddTransition(NinjaStates.KO, ko);

        _fsm = new FSM<NinjaStates>(idle);
    }

    private void InitializeTree()
    {
        ITreeNode idle = new ActionNode(() => { _fsm.Transition(NinjaStates.Idle); });
        ITreeNode dash = new ActionNode(() => { _fsm.Transition(NinjaStates.Dash); });
        ITreeNode grab = new ActionNode(() => { _fsm.Transition(NinjaStates.Grab); });
        ITreeNode parry = new ActionNode(() => { _fsm.Transition(NinjaStates.Parry); });
        ITreeNode KO = new ActionNode(() => { _fsm.Transition(NinjaStates.KO); });

        ITreeNode rootQuestion = new QuestionNode(QKO, KO,
                                 new QuestionNode(QDash, dash,
                                 new QuestionNode(QParry, parry,
                                 new QuestionNode(QGrab, grab, idle))));

        _root = rootQuestion;
    }

    private bool QDash() => IsDashing() || CanDashFromInput();
    private bool QGrab() => _currentSurface != null && !IsDashing();
    private bool QParry() => CanParryFromInput();
    private bool QKO() => _isDead;

    #endregion

    #region MAGIC METHODS

    private void Awake()
    {
        InitializeFSM();
        InitializeTree();
    }

    private void Start()
    {
        if (swipeIndicator != null)
            swipeIndicator.enabled = false;
    }

    void OnEnable()
    {
        _isDead = false;
        CustomUpdateManager.Instance.SubscribeToUpdate(CustomUpdate);
    }

    private void OnDisable()
    {
        CustomUpdateManager.Instance.UnsubscribeFromUpdate(CustomUpdate);
    }

    private void CustomUpdate()
    {
        _root.Execute();
        _fsm.OnUpdate();

        CheckSwipe();

        HandleParryTimer();
        UpdateAnimatorParameters();
    }

    private void FixedUpdate()
    {
        // HandleFalling();
    }

    #endregion

    #region INPUT DETECTION
    private void CheckSwipe()
    {
        if (!CanProcessInput()) return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            TryStartParryLogic();
            swipeStart = Input.mousePosition;
            _startedSwipe = true;
        }

        if (Input.GetMouseButton(0))
        {
            currentSwipe = (Vector2)Input.mousePosition - swipeStart;

            Vector3 start = transform.position;

            if (_startedSwipe && currentSwipe.magnitude >= minSwipeDistance)
            {
                if (swipeIndicator != null && !swipeIndicator.enabled)
                {
                    swipeIndicator.enabled = true;
                }
                isSwiping = true;

                Vector2 clampedDir = -currentSwipe.normalized; //GetClampedSwipeDirection(currentSwipe.normalized);
                Vector3 end = start + (Vector3)(clampedDir * 2f);
                if (swipeIndicator != null)
                {
                    swipeIndicator.SetPosition(0, start);
                    swipeIndicator.SetPosition(1, end);
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            isSwiping = false;
            _startedSwipe = false;
            if (swipeIndicator != null)
            {
                swipeIndicator.enabled = false;
            }
            endTouchPosition = Input.mousePosition;
            Vector2 swipeDelta = endTouchPosition - swipeStart; // GetClampedSwipeDirection(endTouchPosition - swipeStart);
            if (swipeDelta.magnitude >= minSwipeDistance)
            {
                TryDashFromSwipe(swipeDelta);
            }
        }
#else
        if (Input.touchCount > 0)
        {
            TryStartParryLogic();
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    swipeStart = touch.position;
                    _startedSwipe = true;
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    currentSwipe = touch.position - swipeStart;

                    Vector3 start = transform.position;

                    if (_startedSwipe &&  currentSwipe.magnitude >= minSwipeDistance)
                    {
                        if (swipeIndicator != null && !swipeIndicator.enabled)
                        {
                            swipeIndicator.enabled = true;
                        }
                        isSwiping = true;

                        Vector2 clampedDir = -currentSwipe.normalized; //GetClampedSwipeDirection(currentSwipe.normalized);

                        Vector3 end = start + (Vector3)(clampedDir * 2f);

                        if (swipeIndicator != null)
                        {
                            swipeIndicator.SetPosition(0, start);
                            swipeIndicator.SetPosition(1, end);
                        }
                    }
                break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isSwiping)
                    {
                        isSwiping = false;
                        if (swipeIndicator != null)
                        {
                            swipeIndicator.enabled = false;
                        }
                        endTouchPosition = touch.position;
                        Vector2 swipeDelta = endTouchPosition - swipeStart;
                        if (swipeDelta.magnitude >= minSwipeDistance)
                        {
                            TryDashFromSwipe(swipeDelta);
                        }
                    }
                    break;
            }
        }
        else
        {
            if (isSwiping)
            {
                isSwiping = false;
                if (swipeIndicator != null)
                    swipeIndicator.enabled = false;
            }
        }
#endif
    }

    //private Vector2 GetClampedSwipeDirection(Vector2 swipeDelta)
    //{
    //    Vector2 dashDir = -swipeDelta.normalized;

    //    Vector2 surfaceNormal = Vector2.up;
    //    float dis = 0;

    //    for (int n = 0; n < 4; n++)
    //    {
    //        RaycastHit2D currentHit = Physics2D.Raycast(transform.position, GetDirectionByIndex(n), checkDistance, _scenarioLayer);
    //        if (currentHit && (dis == 0 || currentHit.distance < dis))
    //        {
    //            dis = currentHit.distance;
    //            surfaceNormal = currentHit.normal;
    //            Debug.DrawLine(transform.position, currentHit.point, Color.magenta);
    //        }
    //    }
    //    Debug.DrawRay(transform.position, surfaceNormal, Color.yellow);

    //    float angle = Vector2.Angle(dashDir, surfaceNormal);

    //    if (angle > _maxAngle)
    //    {
    //        Vector3 rotationAxis = Vector3.Cross(surfaceNormal, dashDir);
    //        dashDir = Quaternion.AngleAxis(_maxAngle, rotationAxis) * surfaceNormal;
    //    }

    //    return dashDir;
    //}

    private bool _dashInputDetected = false;
    private bool _parryInputDetected = false;
    #endregion

    #region DASHING
    private void TryDashFromSwipe(Vector2 swipeDelta)
    {
        if (swipeDelta.magnitude < minSwipeDistance)
        {
            return;
        }

        if(_isDashing)
        {
            return;
        }

        Vector2 dashDir = -swipeDelta.normalized;

        float dashCD = _playerModel.DashCD;

        var context = PowerUpManager.Instance?.context;
        if (context != null && context.DashTurboActive)
            dashCD *= context.DashCooldownMultiplier;

        if (Time.time < _lastDash + dashCD)
        {
            return;
        }

        RaycastHit2D hit = Physics2D.Raycast(transform.position, dashDir, checkDistance, _scenarioLayer);

        if (hit.collider != null)
        {
            return;
        }

        lastSwipeDelta = swipeDelta;
        _wishedDirection = dashDir;
        _lastDash = Time.time;

        _dashInputDetected = true;

        NotifyTutorialDashPerformed();
    }

    private void NotifyTutorialDashPerformed()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnDashPerformed();
        }
    }

    public bool CanDashFromInput()
    {
        if (_dashInputDetected)
        {
            _dashInputDetected = false;
            return true;
        }
        return false;
    }
    public void Dash()
    {
        _playerView.Animator.SetBool("IsWallGrabbed", false);
        _playerView.Animator.SetBool("IsCeilingGrabbed", false);
        if (_playerView == null || _playerView.RB == null)
        {
            return;
        }
        SetIsDashing(true);
        AudioManager.Instance.PlaySFXAtPosition(SFXClip.P_Movement, transform.position);

        MoveTracker.RegisterMove();
        _lastDashDirection = _wishedDirection;

        _playerView.RB.linearVelocity = Vector2.zero;
        _playerView.RB.AddForce(_wishedDirection * _playerModel.DashForce, ForceMode2D.Impulse);

        SetIsMirrored(false);
        RotateSprites(_lastDashDirection);
    }

    
    #endregion

    #region PARRYING
    private void TryStartParryLogic()
    {
        if (!CanProcessInput()) return;
        if (isParrying) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2f, LayerMask.GetMask("Projectiles"));

        AudioManager.Instance.PlaySFXAtPosition(SFXClip.P_ParrySwing, transform.position);
        foreach (var hit in hits)
        {
            Projectile proj = hit.GetComponent<Projectile>();
            if (proj != null && proj.IsParryable)
            {
                _parryInputDetected = true;
                AudioManager.Instance.PlaySFXAtPosition(SFXClip.P_ProjectileParried, transform.position);
            }
        }
    }

    public bool CanParryFromInput()
    {
        if (_parryInputDetected)
        {
            Debug.Log("Parry Input");
            _parryInputDetected = false;
            return true;
        }
        return false;
    }
    public void StartParry(Collider2D[] hits)
    {
        isParrying = true;
        float parryWindow = _playerModel.ParryWindow;

        var context = PowerUpManager.Instance?.context;
        if (context != null && context.ParryPerfectActive)
            parryWindow += context.ParryBonusWindow;

        parryTimer = parryWindow;

        foreach (var hit in hits)
        {
            Projectile proj = hit.GetComponent<Projectile>();
            if (proj != null && proj.IsParryable)
            {
                Transform target = proj.Shooter != null ? proj.Shooter : FindClosestEnemy();
                Vector2 newDir = (target.position - transform.position).normalized;
                proj.ReflectBackwards(transform, newDir);
            }
        }

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnParryPerformed();
        }
    }
    #endregion

    #region COLLISION DETECTION
    private void OnCollisionEnter2D(Collision2D collision)
    {
        string colTag = collision.gameObject.tag;
        if (colTag == "Scenario" ||
            colTag == "Obstacle" ||
            colTag == "Floor" ||
            colTag == "Ceiling" ||
            collision.gameObject.GetComponent<PlatformBase>() != null)
        {
            if (_currentSurface != null && collision.collider == _currentSurface)
            {
                return;
            }

            _currentSurface = collision.collider;

            SetIsDashing(false);
            SetGrabbingAnimation();
            RotateSprites(colTag == "Ceiling" ? Vector2.left : Vector2.right);

            ElasticPlatform elasticPlatform = collision.gameObject.GetComponent<ElasticPlatform>();
            _lastSurfaceWasElastic = elasticPlatform != null;

            if (!_lastSurfaceWasElastic)
            {
                if (!_isDashing)
                {
                    _playerView.RB.linearVelocity = Vector2.zero;
                }
            }
        }
        else if (colTag == "Projectile")
        {
            collision.gameObject.TryGetComponent(out Projectile projectile);
            projectile.ManageCollision(_playerView.Col);
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider == _currentSurface)
        {
            ForceExitSurface();

            if (_lastSurfaceWasElastic)
            {
                StartCoroutine(ResetElasticFlag());
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            if (_isDashing)
            {
                AudioManager.Instance.PlaySFXAtPosition(SFXClip.P_Attack, transform.position);
                HapticFeedback.MediumFeedback();
                collision.GetComponent<Enemy>().Die();
            }
            else
            {
                HapticFeedback.HeavyFeedback();
                Die();
            }
        }

        if (collision.gameObject.GetComponent<MovingPlatform>() != null)
        {
            Rigidbody2D rb = _playerView.RB;
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }
    }
    #endregion

    private void UpdateAnimatorParameters()
    {
        _playerView.Animator.SetBool("IsGrounded", !IsDashing());
    }

    private void HandleFalling()
    {
        if (_currentSurface == null && !_isDashing)
        {
            var rb = _playerView.RB;
            if (rb != null)
            {
                bool wasOnElasticPlatform = _lastSurfaceWasElastic;

                if (!wasOnElasticPlatform && rb.linearVelocity.y > -15f)
                {
                    float fallSpeed = 8f;
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, -fallSpeed);
                }
            }
        }
    }

    public bool HasCurrentSurface()
    {
        return _currentSurface != null;
    }

    private IEnumerator ResetElasticFlag()
    {
        yield return new WaitForSeconds(0.5f);
        _lastSurfaceWasElastic = false;
    }

    private void HandleParryTimer()
    {
        if (isParrying)
        {
            parryTimer -= Time.deltaTime;
            if (parryTimer <= 0)
            {
                isParrying = false;
            }
        }
    }

    public void Die()
    {
        if (!_isInvincible)
        {
            if (_isDead) return;
            _isDead = true;
            
            var levelController = FindObjectOfType<LevelController>();
            if (levelController != null)
            {
                levelController.MarkLevelAsFailed();
            }

            LevelManager.Instance.OnPlayerLose();
            _fsm.Transition(NinjaStates.KO);
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    private bool CanProcessInput()
    {
        return inputEnabled;
    }

    #region RESOURCES
    public bool IsParrying() => isParrying;
    public void SetIsParrying(bool value) => isParrying = value;
    public float GetParryWindow() => _playerModel.ParryWindow;

    public bool IsDashing() => _isDashing;
    public void SetIsDashing(bool value) => _isDashing = value;
    public bool IsMirrored() => _isMirrored;
    public void SetIsMirrored(bool newValue) => _isMirrored = newValue;
    public bool IsFlipped() => _isFlipped;
    public void SetIsFlipped(bool newValue) => _isFlipped = newValue;

    public void ForceExitSurface() => _currentSurface = null;

    public Vector2 GetDashDirection() => _wishedDirection;
    public Vector2 GetLastDashDirection() => _lastDashDirection;

    public void SetInvincibility(bool value) => _isInvincible = value;

    Transform FindClosestEnemy()
    {
        float minDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (Collider2D col in Physics2D.OverlapCircleAll(transform.position, 10f, _enemyLayer))
        {
            float dist = Vector2.Distance(transform.position, col.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = col.transform;
            }
        }

        return closest;
    }

    //private Vector2 GetDirectionByIndex(int i)
    //{
    //    return i switch
    //    {
    //        0 => Vector2.right,
    //        1 => Vector2.down,
    //        2 => Vector2.left,
    //        3 => Vector2.up,
    //        _ => throw new System.IndexOutOfRangeException($"{i} was out of range"),
    //    };
    //}

    public void RotateSprites(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _playerView.SpriteContainer.transform.rotation = Quaternion.Euler(0, 0, angle);

        SetIsFlipped((angle > -180 && angle <= -90) || angle <= 180 && angle > 90);

        Vector3 newScale = _playerView.SpriteContainer.transform.localScale;
        newScale.x = IsMirrored() ? -1 : 1;
        newScale.y = IsFlipped() ? -1 : 1;
        _playerView.SpriteContainer.transform.localScale = newScale;
    }
    private void SetGrabbingAnimation()
    {
        RaycastHit2D colPoint = Physics2D.Raycast(transform.position, _wishedDirection, float.MaxValue, _scenarioLayer);
        Vector2 normal = colPoint.normal;

        Debug.DrawRay(transform.position, _wishedDirection * float.MaxValue, Color.red, 1f);
        Debug.DrawRay(colPoint.point, normal * 1f, Color.red, 1f);

        if (normal != null)
        {
            if (normal == Vector2.right || normal == Vector2.left)
            {
                _playerView.Animator.SetBool("IsWallGrabbed", true);
                SetIsMirrored(normal == Vector2.left ? true : false);
            }
            else if (normal == Vector2.down)
            {
                _playerView.Animator.SetBool("IsCeilingGrabbed", true);
            }
        }
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (isSwiping && currentSwipe.magnitude >= minSwipeDistance)
        {
            Gizmos.color = Color.yellow;
            Vector3 start = transform.position;
            Vector3 end = start + (Vector3)(-currentSwipe.normalized * 2f);
            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(end, 0.1f);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}
