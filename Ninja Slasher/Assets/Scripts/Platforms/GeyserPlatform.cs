using UnityEngine;

public class GeyserPlatform : PlatformBase
{
    private bool _isFalling = false;
    private bool _playerIsOn = false;

    [Header("FALL SETTINGS")]
    [SerializeField] private float _gravity = 20f;
    [SerializeField] private float _maxFallSpeed = 10f;

    private float _currentFallSpeed;
    private float _startY;

    private Rigidbody2D _playerRB;
    private Rigidbody2D _rb;

    [SerializeField] private Geyser _geyser;
    [SerializeField] private GameObject _crater;

    public SpriteRenderer Renderer => _renderer;
    [SerializeField] private SpriteRenderer _renderer;

    private Vector3 _previousPosition;

    protected override void InitializePlatform()
    {
        TryGetComponent(out Rigidbody2D rb);
        _rb = rb;

        _startY = transform.position.y;
        _previousPosition = transform.position;

        base.InitializePlatform();
    }

    public override void OnPlayerEnter(GameObject player)
    {
        _playerIsOn = true;

        player.TryGetComponent(out Rigidbody2D rb);
        _playerRB = rb;
    }

    public override void OnPlayerExit(GameObject player, bool isForced = false)
    {
        _playerIsOn = false;

        if (_playerRB != null && !isForced)
        {
            _playerRB.gravityScale = 1;
        }

        _playerRB = null;
    }

    public override void OnPlatformUpdate()
    {
        HandlePlayerTransport();

        if (!_isFalling)
        {
            _previousPosition = transform.position;
            return;
        }

        HandleFalling();
    }

    private void HandleFalling()
    {
        _currentFallSpeed += _gravity * Time.fixedDeltaTime;
        _currentFallSpeed = Mathf.Min(_currentFallSpeed, _maxFallSpeed);

        Vector2 pos = _rb.position;
        pos.y -= _currentFallSpeed * Time.fixedDeltaTime;

        if (pos.y <= _startY)
        {
            pos.y = _startY;

            _rb.MovePosition(pos);

            _currentFallSpeed = 0f;
            _isFalling = false;

            _geyser.Animator.SetTrigger("OnLanding");

            _previousPosition = transform.position;

            return;
        }

        _rb.MovePosition(pos);

        _previousPosition = transform.position;
    }

    private void HandlePlayerTransport()
    {
        if (!_playerIsOn || _playerRB == null)
            return;

        Vector3 delta = transform.position - _previousPosition;

        _playerRB.position += (Vector2)delta;
    }

    public void SetValues(bool goesDown)
    {
        _isFalling = goesDown;

        if (goesDown)
        {
            _currentFallSpeed = 0f;
        }

        if (_playerRB != null)
        {
            _playerRB.gravityScale = goesDown ? 1 : 0;
            _playerRB.linearVelocity = Vector2.zero;
        }
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);

        if (collision.gameObject == _crater)
        {
            _currentFallSpeed = 0f;

            _geyser.Animator.SetTrigger("OnLanding");

            SetValues(false);
        }
    }

    public void MovePlatformAndPlayer(Vector3 newLocalPos)
    {
        Vector3 newWorldPos =
            transform.parent != null
            ? transform.parent.TransformPoint(newLocalPos)
            : newLocalPos;

        _rb.MovePosition(newWorldPos);

        _previousPosition = transform.position;
    }
}