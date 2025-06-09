using System;
using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField] private View _playerView;
    [SerializeField] private Model _playerModel;

    // Movimiento
    private bool _isOnSurface = true;
    private float _lastDash;
    private Collider2D _currentSurface;
    private Vector2 _lastDashDirection;

    private Vector2 _wishedDirection;
    private Vector2 lastSwipeDelta;

    // Swipe
    private Vector2 swipeStart;
    private Vector2 endTouchPosition;
    private Vector2 currentSwipe;
    private bool isSwiping = false;
    [SerializeField] private float minSwipeDistance;

    // Parry
    private bool isParrying = false;
    private float parryTimer;

    // Dash bloqueado
    [SerializeField] private float checkDistance;
    [SerializeField] private LayerMask obstacleLayer;

    private void Update()
    {
        if (_isOnSurface && !isParrying)
            GetSwipeInput();

        HandleParryTimer();
    }

    private void GetSwipeInput()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            swipeStart = Input.mousePosition;
            isSwiping = true;
        }

        if (Input.GetMouseButton(0))
        {
            currentSwipe = (Vector2)Input.mousePosition - swipeStart;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isSwiping = false;
            endTouchPosition = Input.mousePosition;
            TryDashFromSwipe(endTouchPosition - swipeStart);
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
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    currentSwipe = touch.position - swipeStart;
                    break;
                case TouchPhase.Ended:
                    isSwiping = false;
                    endTouchPosition = touch.position;
                    TryDashFromSwipe(endTouchPosition - swipeStart);
                    break;
            }
        }
#endif
    }

    private void TryDashFromSwipe(Vector2 swipeDelta)
    {
        if (_isOnSurface && swipeDelta.magnitude >= minSwipeDistance && Time.time >= _lastDash + _playerModel.DashCD)
        {
            lastSwipeDelta = swipeDelta;
            _wishedDirection = -swipeDelta.normalized;

            if (IsBlockedInDirection(_wishedDirection))
            {
                Debug.Log("Dash bloqueado en esa direccion");
                return;
            }

            _lastDash = Time.time;
            Dash();
        }
        else if (_isOnSurface && !isParrying)
        {
            StartParry();
        }
    }

    private void Dash()
    {
        _lastDashDirection = _wishedDirection;
        _playerView.RB.linearVelocity = Vector2.zero;
        _playerView.RB.AddForce(_wishedDirection * _playerModel.DashForce, ForceMode2D.Impulse);
        _playerView.CurrentVelocity = _playerView.RB.linearVelocity;
        _isOnSurface = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        switch (collision.gameObject.tag)
        {
            case "Scenario" or "Obstacle":
                if (_currentSurface != null && collision.collider == _currentSurface)
                    return;

                _currentSurface = collision.collider;
                _isOnSurface = true;

                if (collision.gameObject.GetComponent<PlatformBase>() == null)
                {
                    _playerView.RB.linearVelocity = Vector2.zero;
                }
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Enemy"))
        {
            if (!_isOnSurface)
            {
                collision.GetComponent<Enemy>().Die();
            }
            else
            {
                Debug.Log("Game Over");
                Destroy(gameObject);
            }
        }
    }

    private bool IsBlockedInDirection(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, checkDistance, obstacleLayer);
        return hit.collider != null;
    }

    void StartParry()
    {
        if (isParrying) return;

        isParrying = true;
        parryTimer = _playerModel.ParryWindow;

        Debug.Log("Parry activado");

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2f, LayerMask.GetMask("Projectiles"));

        foreach (var hit in hits)
        {
            Projectile proj = hit.GetComponent<Projectile>();
            if (proj != null && proj.IsParryable && !proj.HasBeenReflected)
            {
                proj.ReflectBackwards();
            }
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
        Debug.Log("Jugador muerto");
        Destroy(gameObject);
    }

    public bool IsParrying() => isParrying;
    public bool IsDashing() => !_isOnSurface;

    public Vector2 GetDashDirection() => _wishedDirection;

    public Vector2 GetLastDashDirection() => _lastDashDirection;

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
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
#endif
    }
}
