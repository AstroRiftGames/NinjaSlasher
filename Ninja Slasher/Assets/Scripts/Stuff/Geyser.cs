using System.Collections;
using UnityEngine;

public class Geyser : MonoBehaviour
{
    [SerializeField] private float _cooldown;
    [SerializeField] private float _activeTime;
    [SerializeField] private float _force;
    [SerializeField] private GeyserPlatform _platform;
    [SerializeField] private ParticleSystem _particles;
    private float _maxHeight;
    
    private float _lastActivation;
    private bool _isActive;
    private NewController _player;
    [SerializeField] private AudioSet _audioSet;

    protected ElementAudioContext _audioContext;

    private void OnEnable()
    {
        CustomUpdateManager.Instance.SubscribeToFixedUpdate(CustomUpdate);
        _player = FindFirstObjectByType<NewController>();
        _lastActivation = Time.time;
        _maxHeight = _platform.transform.localPosition.y;
        _platform.transform.localPosition = Vector2.zero;
    }

    private void OnDisable()
    {
        CustomUpdateManager.Instance.UnsubscribeFromFixedUpdate(CustomUpdate);
    }

    public void Awake()
    {
        InitializeAudioContext();
    }

    public void CustomUpdate()
    {
        if (!_isActive)
        {
            if (CanActivate())
            {
                Activate();
            }
        }
        else if(TimeCheck())
        {
            Deactivate();
        }
        else
        {
            AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.Loop, transform.position);
        }
    }

    private void Activate()
    {
        _lastActivation = Time.time;
        _isActive = true;
        StartCoroutine(MovePlatform());
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.Start, transform.position);
        _particles.Play();
    }

    private void Deactivate()
    {
        _isActive = false;
        AudioService.Instance.StopSFX(_audioContext.Audio.Loop);
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.End, transform.position);
        _particles.Stop();
        _platform.SetValues(true);
    }

    private IEnumerator MovePlatform()
    {
        float currentHeight = _platform.transform.localPosition.y;
        Vector3 newPos = _platform.transform.localPosition;
        while (currentHeight < _maxHeight)
        {
            currentHeight += Time.deltaTime * _force;
            newPos.y = currentHeight;
            _platform.transform.localPosition = newPos;
            yield return null;
        }
    }

    private bool CanActivate()
    {
        return Time.time >= _lastActivation + _activeTime +_cooldown;
    }

    private bool TimeCheck()
    {
        return Time.time >= _lastActivation + _activeTime;
    }

    protected virtual void InitializeAudioContext()
    {
        _audioContext = GetComponent<ElementAudioContext>();
        if (_audioContext != null && _audioSet != null)
            _audioContext.Initialize(_audioSet);
    }
}
