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
        Debug.Log("Geyser Platform: Player Entered");
        _playerIsOn = true;
        TryGetComponent(out Rigidbody2D rb);
        _playerRB = rb;
    }

    public override void OnPlayerExit(GameObject player, bool isForced = false)
    {
        Debug.Log("Geyser Platform: Player Exited");
        _playerIsOn = false;
        _playerRB = null;
    }

    public override void OnPlatformUpdate()
    {
        Vector2 lastPos = transform.position;
        Debug.Log($"Geyser Platform Update: Player={_playerIsOn} - Falling={_isFalling}");

        if (_isFalling && _playerIsOn)
        {
            Vector2 platformDelta = (Vector2)transform.position - lastPos;

            if (_playerRB != null && platformDelta != Vector2.zero)
            {
                _playerRB.MovePosition(_playerRB.position + platformDelta);
            }
        }
    }

    public void SetValues(bool goesDown)
    {
        _rb.gravityScale = goesDown?1:0;
        _isFalling = goesDown;
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);
        Debug.Log("Geyser Platform Collision: " + collision.gameObject.name);
        if (collision.gameObject == _geyser)
        {
            SetValues(false);
        }
    }
}