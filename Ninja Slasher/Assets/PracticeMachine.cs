using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PracticeMachine : MonoBehaviour
{
    [SerializeField] Transform _shootingPoint;
    [SerializeField] float _cooldown;
    [SerializeField] float _maxCadency;
    [SerializeField] LayerMask _obstaclesLayer;
    [SerializeField] GameObject[] _ammoTypes;
    Transform _target;

    private bool _isActive;
    private float _lastShot;
    private Vector2 _dirToTarget;
    private int _currentAmmoTypeIndex;
    private GameObject _currentAmmoType;
    private float _cadency;


    [SerializeField] GameObject UI;
    [SerializeField] TextMeshProUGUI _ammoTypeText;
    [SerializeField] TextMeshProUGUI _cadencyText;

    private void Start()
    {
        _target = FindAnyObjectByType<Controller>().transform;
    }

    private void Update()
    {
        GetInput();
        if (_isActive)
        {
            UpdateUI();
            _dirToTarget = _target.position - _shootingPoint.position;

            Debug.Log(CheckLOS());

            TryShoot();
        }
    }

    private void GetInput()
    {
        if(Input.GetKeyDown(KeyCode.J))
        {
            if (!_isActive) Activate();
            else Deactivate();
        }
        if (Input.GetKeyDown(KeyCode.K)) ChangeAmmoType();
        if (Input.GetKeyDown(KeyCode.L)) ChengeCadency();
    }

    private void UpdateUI()
    {
        _cadencyText.text = $"Cadency: {_cadency}/s";
        _ammoTypeText.text = $"Ammo type: {_currentAmmoType.name}";
    }

    private bool CheckLOS()
    {
        float distance = _dirToTarget.magnitude;

        bool hit = Physics2D.Raycast(_shootingPoint.position, _dirToTarget.normalized, distance, _obstaclesLayer).collider != null;
#if UNITY_EDITOR
        Debug.DrawRay(_shootingPoint.position, _dirToTarget.normalized * distance, Color.red, _cooldown / 2);
#endif
        return !hit;
    }

    private void Activate()
    {
        _isActive = true;
        _currentAmmoType = _ammoTypes[0];
        _cadency = 1;
        _cooldown = 1;
        _lastShot = Time.time;
        UI.SetActive(true);
    }

    private void Deactivate()
    {
        _isActive = false;
        UI.SetActive(false);
    }

    private void ChengeCadency()
    {
        _cadency += .5f;
        if (_cadency > _maxCadency) _cadency = .5f;
        _cooldown = 1 / _cadency;
    }

    private void TryShoot()
    {
        Debug.Log($"CD: {Time.time >= _lastShot + _cooldown}");
        if (CheckLOS() && Time.time >= _lastShot + _cooldown)
        {
            Shoot();
            _lastShot += Time.time;
            Debug.Log("Shoot");
        }
    }

    private void Shoot()
    {
        Projectile newProjectile = Instantiate(_currentAmmoType, transform.position, Quaternion.identity).GetComponent<Projectile>();
        newProjectile.Initialize(_dirToTarget.normalized, transform);
    }

    public void ChangeAmmoType()
    {
        _currentAmmoTypeIndex++;
        if(_currentAmmoTypeIndex >= _ammoTypes.Length)
        {
            _currentAmmoTypeIndex = 0;
        }
        _currentAmmoType = _ammoTypes[ _currentAmmoTypeIndex];
    }
}
