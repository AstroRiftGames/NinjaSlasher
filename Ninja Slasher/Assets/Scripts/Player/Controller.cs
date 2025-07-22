using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Controller : MonoBehaviour
{
    public View View => _playerView;
    [SerializeField] private View _playerView;
    public Model Model => _playerModel;
    [SerializeField] private Model _playerModel;

    private FSM<NinjaStates> _fsm;
    private ITreeNode _root;

    [Space]
    private bool _isDashing = false;
    private float _lastDash;
    private Collider2D _currentSurface;
    private bool _lastSurfaceWasElastic = false;
    private Vector2 _lastDashDirection;
    private Vector2 _wishedDirection;
    private Vector2 lastSwipeDelta;

    [Space]
    [SerializeField] private LineRenderer swipeIndicator;
    private Vector2 swipeStart;
    private Vector2 endTouchPosition;
    private Vector2 currentSwipe;
    private bool isSwiping = false;
    [SerializeField] private float minSwipeDistance;

    [Space]
    private bool isParrying = false;
    private float parryTimer;

    [Space]
    [SerializeField] private float checkDistance;
    [SerializeField] private LayerMask obstacleLayer;

    private bool _isDead = false;
    private bool _isInvincible;


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
        dash.AddTransition(NinjaStates.Idle, idle);

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

    private bool QDash() => _isDashing || CanDashFromInput();
    private bool QGrab() => _currentSurface != null && !_isDashing;
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
    }

    private void Update()
    {
        _root.Execute();
        _fsm.OnUpdate();

        CheckSwipe();
        CheckParryTap();

        HandleParryTimer();
    }

    private void FixedUpdate()
    {
        // HandleFalling();
    }

    #endregion

    #region INPUT DETECTION
    private void CheckSwipe()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            swipeStart = Input.mousePosition;
            isSwiping = true;
            if (swipeIndicator != null)
                swipeIndicator.enabled = true;
        }

        if (Input.GetMouseButton(0) && isSwiping)
        {
            currentSwipe = (Vector2)Input.mousePosition - swipeStart;
            if (currentSwipe.magnitude >= minSwipeDistance)
            {
                if (swipeIndicator != null && !swipeIndicator.enabled)
                    swipeIndicator.enabled = true;

                Vector2 dir = -currentSwipe.normalized;
                Vector3 start = transform.position;
                Vector3 end = start + (Vector3)(dir * 2f);

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
            if (swipeIndicator != null)
                swipeIndicator.enabled = false;
            endTouchPosition = Input.mousePosition;
            Vector2 swipeDelta = endTouchPosition - swipeStart;
            if (swipeDelta.magnitude >= minSwipeDistance)
                TryDashFromSwipe(swipeDelta);
        }
#else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    swipeStart = touch.position;
                    isSwiping = true;
                    if (swipeIndicator != null)
                        swipeIndicator.enabled = true;
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isSwiping)
                    {
                        currentSwipe = touch.position - swipeStart;

                        if (currentSwipe.magnitude >= minSwipeDistance)
                        {
                            if (swipeIndicator != null && !swipeIndicator.enabled)
                                swipeIndicator.enabled = true;

                            Vector2 dir = -currentSwipe.normalized;
                            Vector3 start = transform.position;
                            Vector3 end = start + (Vector3)(dir * 2f);

                            if (swipeIndicator != null)
                            {
                                swipeIndicator.SetPosition(0, start);
                                swipeIndicator.SetPosition(1, end);
                            }
                        }
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isSwiping)
                    {
                        isSwiping = false;
                        if (swipeIndicator != null)
                            swipeIndicator.enabled = false;
                        endTouchPosition = touch.position;
                        Vector2 swipeDelta = endTouchPosition - swipeStart;
                        if (swipeDelta.magnitude >= minSwipeDistance)
                            TryDashFromSwipe(swipeDelta);
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

        if (_currentSurface == null)
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

        RaycastHit2D hit = Physics2D.Raycast(transform.position, dashDir, checkDistance, obstacleLayer);

        if (hit.collider != null)
        {
            return;
        }

        lastSwipeDelta = swipeDelta;
        _wishedDirection = dashDir;
        _lastDash = Time.time;

        _dashInputDetected = true;
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
        if (_playerView == null || _playerView.RB == null)
        {
            return;
        }

        MoveTracker.RegisterMove();
        _lastDashDirection = _wishedDirection;

        _playerView.RB.linearVelocity = Vector2.zero;
        _playerView.RB.AddForce(_wishedDirection * _playerModel.DashForce, ForceMode2D.Impulse);

        _isDashing = true;
        _playerView.Animator.SetBool("IsGrounded", _isDashing);
    }
    #endregion

    #region PARRYING
    private void CheckParryTap()
    {
        if (isSwiping) return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            TryStartParryLogic();
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TryStartParryLogic();
        }
#endif
    }

    private void TryStartParryLogic()
    {
        if (isParrying) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2f, LayerMask.GetMask("Projectiles"));
        foreach (var hit in hits)
        {
            Projectile proj = hit.GetComponent<Projectile>();
            if (proj != null && proj.IsParryable && !proj.HasBeenReflected)
            {
                _parryInputDetected = true;
                return;
            }
        }
    }

    public bool CanParryFromInput()
    {
        if (_parryInputDetected)
        {
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
            if (proj != null && proj.IsParryable && !proj.HasBeenReflected)
            {
                proj.ReflectBackwards();
            }
        }
    }
    #endregion

    #region COLLISION DETECTION
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Scenario") ||
            collision.gameObject.CompareTag("Obstacle") ||
            collision.gameObject.GetComponent<PlatformBase>() != null)
        {
            if (_currentSurface != null && collision.collider == _currentSurface)
            {
                return;
            }

            _currentSurface = collision.collider;

            //if (_fsm.CurrentState.GetType() != typeof(NinjaDashState<NinjaStates>))
            //{
                SetIsDashing(false);
            //}


            _playerView.Animator.SetBool("IsGrounded", _isDashing);

            Vector2 contactPoint = Vector2.zero;
            Vector2 point = collision.GetContact(0).point;
            contactPoint.x = point.x > transform.position.x ? 1 : -1;
            contactPoint.y = point.y >= transform.position.y ? 1 : -1;

            int value = 0;
            if (contactPoint.y > 0) value = 2;
            else if (contactPoint.x > 0) value = 1;

            _playerView.Animator.SetInteger("GrabType", value);

            ElasticPlatform elasticPlatform = collision.gameObject.GetComponent<ElasticPlatform>();
            _lastSurfaceWasElastic = (elasticPlatform != null);

            if (!_lastSurfaceWasElastic)
            {
                if (!_isDashing)
                {
                    _playerView.RB.linearVelocity = Vector2.zero;
                }
            }
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider == _currentSurface)
        {
            _currentSurface = null;

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
                collision.GetComponent<Enemy>().Die();
            }
            else
            {
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

            GameManager.Instance.OnPlayerLose();
            _fsm.Transition(NinjaStates.KO);
        }
    }

    #region RESOURCES
    public bool IsParrying() => isParrying;
    public void SetIsParrying(bool value) => isParrying = value;
    public float GetParryWindow() => _playerModel.ParryWindow;

    public bool IsDashing() => _isDashing;
    public void SetIsDashing(bool value) => _isDashing = value;

    public void ForceExitSurface() => _currentSurface = null;

    public Vector2 GetDashDirection() => _wishedDirection;
    public Vector2 GetLastDashDirection() => _lastDashDirection;

    public void SetInvincibility(bool value) => _isInvincible = value;
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
