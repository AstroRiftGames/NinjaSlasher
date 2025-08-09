using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GodMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] Animator _menuCanvasANIM;
    [SerializeField] Button _openCloseBTN;
    [SerializeField] Button _restartBTN;
    [SerializeField] Toggle _invincibleTGL;

    //Enemy Spawn Management
    [SerializeField] TMP_Dropdown _enemiesDPD;
    [SerializeField] Button _spawnEnemyBTN;
    [SerializeField] Button _removeEnemiesBTN;
    
    //Time Scale Management
    [SerializeField] Button _increaseScaleBTN;
    [SerializeField] Button _decreaseScaleBTN;
    [SerializeField] TextMeshProUGUI _currentScale;

    private bool _isOpen = false;
    [Header("Player")]
    [SerializeField] private GameObject _playerPrefab;
    
    private Controller _player;
    public static Transform Player;
    private Vector2 _playerInitialPos;

    [Header("Enemies")]
    [SerializeField] Transform _enemiesInitial;
    [SerializeField] GameObject[] _enemiesPrefab;

    [Space]
    List<TMP_Dropdown.OptionData> _enemyOptions = new List<TMP_Dropdown.OptionData>();
    GameObject _selectedEnemy;
    List<GameObject> _currentEnemies = new List<GameObject>();

    [Header("Time Scale Management")]
    [SerializeField] private float _scaleJump;


    private void Awake()
    {
        _player = FindFirstObjectByType<Controller>().GetComponent<Controller>();
        Player = _player.transform;

        _playerInitialPos = _player.transform.position;

        foreach(var enemy in _enemiesPrefab)
        {
            _enemyOptions.Add(new TMP_Dropdown.OptionData(enemy.name));
        }
        _enemiesDPD.AddOptions(_enemyOptions);
    }

    private void OnEnable()
    {
        _openCloseBTN.onClick.AddListener(OpenClose);
        _invincibleTGL.onValueChanged.AddListener(_player.SetInvincibility);
        _restartBTN.onClick.AddListener(RestartPositions);
        _enemiesDPD.onValueChanged.AddListener(SelectEnemy);
        _spawnEnemyBTN.onClick.AddListener(SpawnEnemy);
        _removeEnemiesBTN.onClick.AddListener(DestroyEnemies);
        _increaseScaleBTN.onClick.AddListener(IncreaseScale);
        _decreaseScaleBTN.onClick.AddListener(DecreaseScale);
    }

    private void OnDisable()
    {
        _openCloseBTN.onClick.RemoveListener(OpenClose);
        _invincibleTGL.onValueChanged.RemoveListener(_player.SetInvincibility);
        _restartBTN.onClick.RemoveListener(RestartPositions);
        _enemiesDPD.onValueChanged.RemoveListener(SelectEnemy);
        _enemiesDPD.onValueChanged.RemoveListener(SelectEnemy);
        _spawnEnemyBTN.onClick.RemoveListener(SpawnEnemy);
        _removeEnemiesBTN.onClick.RemoveListener(DestroyEnemies);
        _increaseScaleBTN.onClick.RemoveListener(IncreaseScale);
        _decreaseScaleBTN.onClick.RemoveListener(DecreaseScale);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) OpenClose();
    }

    private void OpenClose()
    {
        Debug.Log("Open/Close");
        _isOpen = !_isOpen;
        _menuCanvasANIM.SetTrigger(_isOpen ? "Open" : "Close");
    }

    public void RestartPositions()
    {
        DestroyEnemies();
        RestartPlayer();
    }

    private void SelectEnemy(int value)
    {
        _selectedEnemy = _enemiesPrefab[value];
    }

    private void SpawnEnemy()
    {
        _currentEnemies.Add(Instantiate(_selectedEnemy, _enemiesInitial.position, Quaternion.identity));
    }

    private void DestroyEnemies()
    {
        foreach (GameObject enemy in _currentEnemies)
        {
            Destroy(enemy);
        }
        _currentEnemies.Clear();
    }

    private void RestartPlayer()
    {
        if (_player != null) Destroy(_player.gameObject);

        _player = Instantiate(_playerPrefab, _playerInitialPos, Quaternion.identity).GetComponent<Controller>();
        Player = _player.transform;
    }

    private void IncreaseScale()
    {
        Time.timeScale += _scaleJump;
        _currentScale.text = $"x{Time.timeScale}";
    }
    private void DecreaseScale()
    {
        Time.timeScale -= _scaleJump;
        _currentScale.text = $"x{Time.timeScale}";
    }
}
