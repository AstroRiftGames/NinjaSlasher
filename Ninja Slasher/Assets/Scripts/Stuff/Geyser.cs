using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.UI;

public class Geyser : MonoBehaviour
{
    [SerializeField] private float _cooldown;
    [SerializeField] private float _activeTime;
    [SerializeField] private float _force;
    [SerializeField] private GeyserPlatform _platform;
    [SerializeField] private ParticleSystem _particles;
    [SerializeField] private LayerMask _playerLayer;
    private float _maxHeight;

    private float _lastActivation;
    private bool _isActive;
    private PlayerController _player;
    [SerializeField] private AudioSet _audioSet;

    protected ElementAudioContext _audioContext;

    [SerializeField] private Animator _animator;
    public Animator Animator => _animator;

    private void OnEnable()
    {
        CustomUpdateManager.Instance.SubscribeToFixedUpdate(CustomUpdate);
        _player = FindFirstObjectByType<PlayerController>();
        _lastActivation = Time.time - _cooldown/2 - _activeTime;
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
            CheckPlayer();
        }
    }

    public void Activate()
    {
        _lastActivation = Time.time;
        _isActive = true;

        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.Start, transform.position);
        _animator.SetTrigger("OnActivation");
        _particles.Play();
        StartCoroutine(MovePlatform());
    }

    private void Deactivate()
    {
        _isActive = false;
        AudioService.Instance.StopSFX(_audioContext.Audio.Loop);
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.End, transform.position);
        _animator.SetTrigger("OnDeactivation");
        _particles.Stop();
        _platform.SetValues(true);
    }

    private IEnumerator MovePlatform()
    {
        float currentHeight = _platform.transform.localPosition.y;
        Vector3 newPos = _platform.transform.localPosition;
        while (currentHeight < _maxHeight)
        {
            currentHeight += Time.fixedDeltaTime * _force;
            if (currentHeight > _maxHeight) currentHeight = _maxHeight;
            newPos.y = currentHeight;
            
            _platform.MovePlatformAndPlayer(newPos);
            
            yield return new WaitForFixedUpdate();
        }
        _animator.SetTrigger("OnTopReached");
    }

    private void CheckPlayer()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up, Vector2.Distance(transform.position, _platform.transform.position) - _platform.Renderer.size.y * _platform.transform.localScale.y, _playerLayer);
        if(hit)
        {
            _player.Die();
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
