using UnityEngine;

public class GeyserPlatform : PlatformBase
{
    private bool _isFalling = false;
    private bool _playerIsOn = false;
    private Rigidbody2D _playerRB;
    private Rigidbody2D _rb;
    [SerializeField] private GameObject _geyser;

    protected override void InitializePlatform()
    {
        TryGetComponent(out Rigidbody2D rb);
        _rb = rb;
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
        _playerRB.gravityScale = 0;
        _playerRB = null;
    }

    public override void OnPlatformUpdate()
    {
    }

    public void SetValues(bool goesDown)
    {
        _rb.gravityScale = goesDown?1:0;
        _isFalling = goesDown;
        if(_playerRB != null)
        {
            _playerRB.gravityScale = goesDown?1:0;
            _playerRB.linearVelocity = Vector3.zero;
        }
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);
        if (collision.gameObject == _geyser)
        {
            SetValues(false);
        }
    }
}