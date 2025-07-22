using Unity.VisualScripting;
using UnityEngine;

public class FollowingCamera : MonoBehaviour
{
    private Transform _player;
    private Vector3 _targetPosition;
    [SerializeField] Bounds _bounds;

    private void Awake()
    {
        _player = GodMenu.Player;
    }

    private void Update()
    {
        if(_player != null)
        {
            FollowPlayer();
        }
        else
        {
            _player = GodMenu.Player;
        }
    }

    private void FollowPlayer()
    {
        _targetPosition = _player.position;
        _targetPosition.z = -15;
        if (_bounds.Contains(_targetPosition))
        {
            transform.position = _targetPosition;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_bounds.center, _bounds.size);
    }
}
