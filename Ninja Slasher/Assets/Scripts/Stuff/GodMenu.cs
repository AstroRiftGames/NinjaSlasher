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

    //Boss Spawn Management
    [SerializeField] TMP_Dropdown _bossDPD;
    [SerializeField] Button _spawnBossBTN;
    [SerializeField] Button _removeBossesBTN;

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
    [SerializeField] Transform _enemiesInitialPos;
    [SerializeField] GameObject[] _enemiesPrefabs;

    List<TMP_Dropdown.OptionData> _enemyOptions = new List<TMP_Dropdown.OptionData>();
    GameObject _selectedEnemy;
    List<GameObject> _currentEnemies = new List<GameObject>();

    [Header("Bosses")]
    [SerializeField] Transform _bossesInitialPos;
    [SerializeField] GameObject[] _bossesPrefabs;

    List<TMP_Dropdown.OptionData> _bossOptions = new List<TMP_Dropdown.OptionData>();
    GameObject _selectedBoss;
    List<GameObject> _currentBosses= new List<GameObject>();


    [Header("Time Scale Management")]
    [SerializeField] private float _scaleJump;


    private void Awake()
    {
        _player = FindFirstObjectByType<Controller>().GetComponent<Controller>();
        Player = _player.transform;

        _playerInitialPos = _player.transform.position;
        SetEnemyOptions();
        SetBossOptions();
    }

    private void OnEnable()
    {
        _openCloseBTN.onClick.AddListener(OpenClose);
        //_invincibleTGL.onValueChanged.AddListener(_player.SetInvincibility); //TODO REVISAR
        _restartBTN.onClick.AddListener(RestartPositions);
        _enemiesDPD.onValueChanged.AddListener(SelectEnemy);
        _spawnEnemyBTN.onClick.AddListener(SpawnEnemy);
        _removeEnemiesBTN.onClick.AddListener(DestroyEnemies);
        _spawnBossBTN.onClick.AddListener(SpawnBoss);
        _bossDPD.onValueChanged.AddListener(SelectBoss);
        _removeBossesBTN.onClick.AddListener(DestroyBosses);
        _increaseScaleBTN.onClick.AddListener(IncreaseScale);
        _decreaseScaleBTN.onClick.AddListener(DecreaseScale);
    }

    private void OnDisable()
    {
        _openCloseBTN.onClick.RemoveListener(OpenClose);
        //_invincibleTGL.onValueChanged.RemoveListener(_player.SetInvincibility); //TODO REVISAR
        _restartBTN.onClick.RemoveListener(RestartPositions);
        _enemiesDPD.onValueChanged.RemoveListener(SelectEnemy);
        _enemiesDPD.onValueChanged.RemoveListener(SelectEnemy);
        _spawnEnemyBTN.onClick.RemoveListener(SpawnEnemy);
        _spawnBossBTN.onClick.RemoveListener(SpawnBoss);
        _bossDPD.onValueChanged.RemoveListener(SelectBoss);
        _removeBossesBTN.onClick.RemoveListener(DestroyBosses);
        _removeEnemiesBTN.onClick.RemoveListener(DestroyEnemies);
        _increaseScaleBTN.onClick.RemoveListener(IncreaseScale);
        _decreaseScaleBTN.onClick.RemoveListener(DecreaseScale);
    }

    private void SetEnemyOptions()
    {
        foreach (var enemy in _enemiesPrefabs)
        {
            _enemyOptions.Add(new TMP_Dropdown.OptionData(enemy.name));
        }
        _enemiesDPD.AddOptions(_enemyOptions);
    }

    private void SetBossOptions()
    {
        foreach (var boss in _bossesPrefabs)
        {
            _bossOptions.Add(new TMP_Dropdown.OptionData(boss.name));
        }
        _bossDPD.AddOptions(_bossOptions);
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
        DestroyBosses();
        RestartPlayer();
    }

    private void RestartPlayer()
    {
        if (_player != null) Destroy(_player.gameObject);

        _player = Instantiate(_playerPrefab, _playerInitialPos, Quaternion.identity).GetComponent<Controller>();
        Player = _player.transform;
    }

    #region ENEMY MANAGEMENT
    private void SelectEnemy(int value)
    {
        _selectedEnemy = _enemiesPrefabs[value];
    }

    private void SpawnEnemy()
    {
        _currentEnemies.Add(Instantiate(_selectedEnemy, _enemiesInitialPos.position, Quaternion.identity));
    }

    private void DestroyEnemies()
    {
        foreach (GameObject enemy in _currentEnemies)
        {
            Destroy(enemy);
        }
        _currentEnemies.Clear();
    }
#endregion

    #region BOSS MANAGEMENT
    private void SelectBoss(int value)
    {
        _selectedBoss= _bossesPrefabs[value];
    }

    private void SpawnBoss()
    {
        _currentBosses.Add(Instantiate(_selectedBoss, _bossesInitialPos.position, Quaternion.identity));
    }

    private void DestroyBosses()
    {
        foreach (GameObject boss in _currentBosses)
        {
            if(boss.name == "Arachnomadre BL-KR")
            {
                BL4ZT[] blaztEnemies = FindObjectsOfType<BL4ZT>();
                foreach(var enemy in blaztEnemies)
                {
                    Destroy(enemy.gameObject);
                }
            }
            Destroy(boss);
        }
        _currentBosses.Clear();
    }
#endregion

    #region TimeScale Management

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
    #endregion
}
