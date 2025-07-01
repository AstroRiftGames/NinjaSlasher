using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField] private View _playerView;
    [SerializeField] private Model _playerModel;

    // Movimiento
    private bool _isDashing = false;
    private float _lastDash;
    private Collider2D _currentSurface;
    private bool _lastSurfaceWasElastic = false;
    private Vector2 _lastDashDirection;

    private Vector2 _wishedDirection;
    private Vector2 lastSwipeDelta;

    [SerializeField] private LineRenderer swipeIndicator;

    // Swipe
    private Vector2 swipeStart;
    private Vector2 endTouchPosition;
    private Vector2 currentSwipe;
    private bool isSwiping = false;
    [SerializeField] private float minSwipeDistance;
    private int _moveCount = 0;

    // Parry
    private bool isParrying = false;
    private float parryTimer;

    [SerializeField] private float checkDistance;
    [SerializeField] private LayerMask obstacleLayer;

    private bool _isDead = false;

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
        if (!isParrying)
        {
            CheckSwipe();
            CheckParryTap();
        }

        HandleParryTimer();
    }

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
            else
                Debug.Log("Swipe muy corto");
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

    private void CheckParryTap()
    {
        if (isSwiping) return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            TryStartParry();
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TryStartParry();
        }
#endif
    }

    private void TryStartParry()
    {
        if (isParrying) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2f, LayerMask.GetMask("Projectiles"));
        foreach (var hit in hits)
        {
            Projectile proj = hit.GetComponent<Projectile>();
            if (proj != null && proj.IsParryable && !proj.HasBeenReflected)
            {
                StartParry(hits);
                return;
            }
        }
    }

    private void StartParry(Collider2D[] hits)
    {
        isParrying = true;
        float parryWindow = _playerModel.ParryWindow;

        var context = PowerUpManager.Instance?.context;
        if (context != null && context.ParryPerfectActive)
            parryWindow += context.ParryBonusWindow;

        parryTimer = parryWindow;
        Debug.Log("Parry activado (ventana: " + parryWindow + ")");

        foreach (var hit in hits)
        {
            Projectile proj = hit.GetComponent<Projectile>();
            if (proj != null && proj.IsParryable && !proj.HasBeenReflected)
            {
                proj.ReflectBackwards();
            }
        }
    }

    private void TryDashFromSwipe(Vector2 swipeDelta)
    {
        if (swipeDelta.magnitude < minSwipeDistance)
        {
            Debug.Log("Swipe muy corto");
            return;
        }

        if (_currentSurface == null)
        {
            Debug.Log("No puedes hacer dash en el aire");
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
        Debug.DrawRay(transform.position, dashDir * checkDistance, Color.magenta, 1f);

        if (hit.collider != null)
        {
            Debug.Log($"Dash cancelado: obst�culo en esa direcci�n: {hit.collider.name}");
            return;
        }

        lastSwipeDelta = swipeDelta;
        _wishedDirection = dashDir;

        _lastDash = Time.time;
        Dash();
    }

    private void Dash()
    {
        MoveTracker.RegisterMove();
        _moveCount++;
        _lastDashDirection = _wishedDirection;
        _playerView.RB.linearVelocity = Vector2.zero;
        _playerView.RB.AddForce(_wishedDirection * _playerModel.DashForce, ForceMode2D.Impulse);
        _playerView.CurrentVelocity = _playerView.RB.linearVelocity;

        _isDashing = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Scenario") ||
            collision.gameObject.CompareTag("Obstacle") ||
            collision.gameObject.GetComponent<PlatformBase>() != null)
        {
            if (_currentSurface != null && collision.collider == _currentSurface)
                return;

            _currentSurface = collision.collider;
            _isDashing = false;

            ElasticPlatform elasticPlatform = collision.gameObject.GetComponent<ElasticPlatform>();
            _lastSurfaceWasElastic = (elasticPlatform != null);

            if (!_lastSurfaceWasElastic)
            {
                _playerView.RB.linearVelocity = Vector2.zero;
            }
        }
    }

    private void FixedUpdate()
    {
        HandleFalling();
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

    private System.Collections.IEnumerator ResetElasticFlag()
    {
        yield return new WaitForSeconds(0.5f);
        _lastSurfaceWasElastic = false;
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
                Debug.Log("Game Over");
                Die();
            }
        }

        if (collision.gameObject.GetComponent<MovingPlatform>() != null)
        {
            Rigidbody2D rb = _playerView.RB;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void HandleParryTimer()
    {
        if (isParrying)
        {
            parryTimer -= Time.deltaTime;
            if (parryTimer <= 0)
                isParrying = false;
        }
    }

    public void Die()
    {
        if (!_isInvincible)
        {
            if (_isDead) return;
            _isDead = true;
            Debug.Log("Jugador muerto");
            GameManager.Instance.OnPlayerLose();
        }
        else
        {
            Debug.Log("IsInvincible");
        }
    }

    public bool IsParrying() => isParrying;
    public bool IsDashing() => _isDashing;
    public void ForceExitSurface() => _currentSurface = null;

    public Vector2 GetDashDirection() => _wishedDirection;

    public Vector2 GetLastDashDirection() => _lastDashDirection;

    private bool _isInvincible;
    public void SetInvincibility(bool value)
    {
        _isInvincible = value;
    }

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
