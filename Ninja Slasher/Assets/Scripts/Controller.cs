using System;
using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField] View _playerView;
    [SerializeField] Model _playerModel;

    private bool _isOnSurface = true;
    private float _lastDash;

    Vector2 _wishedDirection;

    private Collider2D _currentSurface;

    private void Awake()
    {
        //TactileController.Instance.OnTouchEnd += TryDash;
    }

    private void Update()
    {
        GetInput();
    }

    void GetInput()
    {
        _wishedDirection.x = Mathf.RoundToInt(Input.GetAxis("Horizontal"));
        _wishedDirection.y = Mathf.RoundToInt(Input.GetAxis("Vertical"));

        if (Input.GetKeyDown(KeyCode.Space)) TryDash();

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F)) _isOnSurface = true;
#endif
    }

    void TryDash(/*object sender, EventArgs e*/)
    {
        if(_isOnSurface && Time.time >= _lastDash + _playerModel.DashCD)
        {
            //_wishedDirection = TactileController.Instance.Slide.normalized;
            _lastDash = Time.time;
            Dash();
        }
    }

    void Dash()
    {
        Debug.Log("Dashed " +  _wishedDirection.ToString());
        _playerView.RB.AddForce(_wishedDirection * _playerModel.DashForce, ForceMode2D.Impulse);
        _isOnSurface = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Scenario"))
        {
            if (_currentSurface != null && collision.collider == _currentSurface)
            {
                return;
            }
            else
            {
                _currentSurface = collision.collider;
                _isOnSurface = true;
                _playerView.RB.linearVelocity = Vector2.zero;
            }
        }
        else if (collision.gameObject.CompareTag("Enemy") )
        {
            if (!_isOnSurface)
            {
                collision.gameObject.GetComponent<Enemy>().Die();
            }
            else
            {
                Debug.Log("Game Over");
                Destroy(gameObject);
            }
        }
    }
}
