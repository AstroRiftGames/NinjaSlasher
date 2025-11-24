using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [SerializeField] private float _shakeDuration = 0.3f;
    [SerializeField] private float _shakeMagnitude = 0.2f;
    [SerializeField] private AnimationCurve _shakeIntensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [SerializeField] private bool _useTraumaSystem = true;
    [SerializeField] private float _traumaDecay = 1.5f;
    [SerializeField] private float _maxShakeOffset = 0.3f;
    [SerializeField] private float _maxShakeRotation = 5f;

    private Camera _mainCamera;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private float _currentTrauma = 0f;
    private bool _isShaking = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        _mainCamera = Camera.main;

        if (_mainCamera != null)
        {
            _originalPosition = _mainCamera.transform.localPosition;
            _originalRotation = _mainCamera.transform.localRotation;
        }
    }

    private void Update()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            return;
        }

        if (_useTraumaSystem && _currentTrauma > 0)
        {
            _currentTrauma = Mathf.Max(0f, _currentTrauma - _traumaDecay * Time.deltaTime);
            ApplyTraumaShake();
        }
    }

    public void TriggerShake()
    {
        TriggerShake(_shakeDuration, _shakeMagnitude);
    }

    public void TriggerShake(float duration, float magnitude)
    {
        if (_mainCamera == null) return;

        if (_useTraumaSystem)
        {
            AddTrauma(magnitude);
        }
        else
        {
            if (!_isShaking)
            {
                StartCoroutine(ShakeCoroutine(duration, magnitude));
            }
        }
    }

    public void AddTrauma(float amount)
    {
        _currentTrauma = Mathf.Clamp01(_currentTrauma + amount);
    }

    private void ApplyTraumaShake()
    {
        if (_mainCamera == null) return;

        float traumaAmount = _currentTrauma * _currentTrauma;

        float offsetX = _maxShakeOffset * traumaAmount * (Mathf.PerlinNoise(Time.time * 25f, 0f) * 2f - 1f);
        float offsetY = _maxShakeOffset * traumaAmount * (Mathf.PerlinNoise(0f, Time.time * 25f) * 2f - 1f);

        float rotationZ = _maxShakeRotation * traumaAmount * (Mathf.PerlinNoise(Time.time * 20f, Time.time * 20f) * 2f - 1f);

        _mainCamera.transform.localPosition = _originalPosition + new Vector3(offsetX, offsetY, 0);
        _mainCamera.transform.localRotation = _originalRotation * Quaternion.Euler(0, 0, rotationZ);
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        if (_mainCamera == null) yield break;

        _isShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percentComplete = elapsed / duration;
            float currentMagnitude = magnitude * _shakeIntensityCurve.Evaluate(percentComplete);

            float offsetX = Random.Range(-1f, 1f) * currentMagnitude;
            float offsetY = Random.Range(-1f, 1f) * currentMagnitude;

            _mainCamera.transform.localPosition = _originalPosition + new Vector3(offsetX, offsetY, 0);

            yield return null;
        }

        _mainCamera.transform.localPosition = _originalPosition;
        _mainCamera.transform.localRotation = _originalRotation;
        _isShaking = false;
    }

    public void ResetCamera()
    {
        if (_mainCamera != null)
        {
            _mainCamera.transform.localPosition = _originalPosition;
            _mainCamera.transform.localRotation = _originalRotation;
        }
        _currentTrauma = 0f;
        _isShaking = false;
    }

    private void OnDisable()
    {
        ResetCamera();
    }
}