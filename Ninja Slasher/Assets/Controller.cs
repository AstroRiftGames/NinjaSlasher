using System;
using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField] View _playerView;
    [SerializeField] Model _playerModel;

    private bool _isOnSurface = true;
    private float _lastDash;
    private Collider2D _currentSurface;

    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private Vector2 _wishedDirection;

    private Vector2 lastSwipeDelta;
    private Vector2 swipeStart;
    private Vector2 currentSwipe;
    private bool isSwiping = false;

    [SerializeField] private float checkDistance;
    [SerializeField] private LayerMask obstacleLayer;

    [SerializeField] private float minSwipeDistance;

    private void Update()
    {
        if (_isOnSurface)
            GetSwipeInput();
    }

    void GetSwipeInput()
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

    void TryDashFromSwipe(Vector2 swipeDelta)
    {
        if (swipeDelta.magnitude >= minSwipeDistance && Time.time >= _lastDash + _playerModel.DashCD)
        {
            lastSwipeDelta = swipeDelta;
            _wishedDirection = -swipeDelta.normalized;
            _wishedDirection = -swipeDelta.normalized;
            _lastDash = Time.time;
            if (IsBlockedInDirection(_wishedDirection))
            {
                Debug.Log("Dash bloqueado en esa direccion");
                return;
            }
            Dash();
        }
    }

    void Dash()
    {
        _playerView.RB.velocity = Vector2.zero;
        _playerView.RB.AddForce(_wishedDirection * _playerModel.DashForce, ForceMode2D.Impulse);
        _isOnSurface = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Scenario"))
        {
            if (_currentSurface != null && collision.collider == _currentSurface)
                return;

            _currentSurface = collision.collider;
            _isOnSurface = true;
            _playerView.RB.velocity = Vector2.zero;
        }
    }

    bool IsBlockedInDirection(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, checkDistance, obstacleLayer);
        return hit.collider != null;
    }

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
#endif
    }
}
