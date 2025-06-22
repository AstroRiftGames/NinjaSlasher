using UnityEngine;

public class CameraScript : MonoBehaviour
{
#if UNITY_EDITOR
    private Transform _player;
    private Vector3 _targetPosition;
    [SerializeField] Bounds _bounds;

    private void Awake()
    {
        _player = FindFirstObjectByType<Controller>().transform;
    }


    private void Update()
    {
        FollowPlayer();
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
#endif
}
