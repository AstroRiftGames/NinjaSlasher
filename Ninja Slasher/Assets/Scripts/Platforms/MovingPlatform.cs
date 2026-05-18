using UnityEngine;
using System;

public class MovingPlatform : PlatformBase
{
    [Header("MOVEMENT")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float cooldown;

    private Transform _target;
    private bool _isMoving = true;
    private float _cooldownTimer = 0f;

    private IPlatform[] _behaviors;

    private Vector2 _previousPlatformPosition;
    private Rigidbody2D _playerRB;
    private GameObject _playerOnPlatform;

    protected override void InitializePlatform()
    {
        _behaviors = GetComponents<IPlatform>();
        _behaviors = Array.FindAll(_behaviors, b => b != (IPlatform)this);

        _previousPlatformPosition = transform.position;
        _target = pointB;

        foreach (var behaviour in _behaviors)
        {
        }
    }

    public override void OnPlayerEnter(GameObject player)
    {
        _playerOnPlatform = player;
        PlayerView view = player.GetComponent<PlayerView>();
        if (view != null)
            _playerRB = view.RB;

        player.transform.SetParent(this.transform);

        foreach (var behavior in _behaviors)
            behavior.OnPlayerEnter(player);
    }

    public override void OnPlayerExit(GameObject player, bool isForced = false)
    {
        if (_playerOnPlatform == player)
        {
            _playerOnPlatform = null;
            _playerRB = null;
            player.transform.SetParent(null);
        }

        foreach (var behavior in _behaviors)
            behavior.OnPlayerExit(player);
    }

    public override void OnPlatformUpdate()
    {
        Vector2 lastPos = transform.position;

        if (_isMoving)
        {
            transform.position = Vector2.MoveTowards(transform.position, _target.position, moveSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, _target.position) < 0.01f)
            {
                _isMoving = false;
                _cooldownTimer = cooldown;
            }
        }
        else
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0f)
            {
                _isMoving = true;
                _target = _target == pointA ? pointB : pointA;
            }
        }

        Vector2 platformDelta = (Vector2)transform.position - lastPos;

        if (_playerRB != null && platformDelta != Vector2.zero)
        {
            _playerRB.MovePosition(_playerRB.position + platformDelta);
        }

        foreach (var behavior in _behaviors)
            behavior.OnPlatformUpdate();
    }
}
