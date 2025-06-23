using UnityEngine;
using UnityEngine.UI;

public class GodMenu : MonoBehaviour
{
    [SerializeField] Animator _menuCanvasANIM;
    [SerializeField] Button _openCloseBTN;
    [SerializeField] Button _restartBTN;
    [SerializeField] Toggle _invincibleTGL;
    private bool _isOpen = false;
    private Controller _player;
    public static Transform Player;

    private Vector2 _playerInitialPos;
    private Vector2 _enemiesInitialPos;
    [SerializeField] GameObject _currentEnemies;
    [SerializeField] GameObject _enemiesPrefab;
    [SerializeField] private GameObject _playerPrefab;


    private void Awake()
    {
        _player = FindFirstObjectByType<Controller>().GetComponent<Controller>();
        Player = _player.transform;

        _playerInitialPos = _player.transform.position;
        _enemiesInitialPos = _currentEnemies.transform.position;
    }

    private void OnEnable()
    {
        _openCloseBTN.onClick.AddListener(OpenClose);
        _invincibleTGL.onValueChanged.AddListener(_player.SetInvincibility);
        _restartBTN.onClick.AddListener(RestartPositions);
    }

    private void OnDisable()
    {
        _openCloseBTN.onClick.RemoveListener(OpenClose);
        _invincibleTGL.onValueChanged.RemoveListener(_player.SetInvincibility);
        _restartBTN.onClick.RemoveListener(RestartPositions);
    }

    private void OpenClose()
    {
        _isOpen = !_isOpen;
        _menuCanvasANIM.SetTrigger(_isOpen ? "Open" : "Close");
    }

    public void RestartPositions()
    {
        Destroy(_currentEnemies);
        if(_player != null) Destroy(_player.gameObject);
        _player = Instantiate(_playerPrefab, _playerInitialPos, Quaternion.identity).GetComponent<Controller>();
        Player = _player.transform;
        _currentEnemies = Instantiate(_enemiesPrefab, _enemiesInitialPos, Quaternion.identity);
    }
}
