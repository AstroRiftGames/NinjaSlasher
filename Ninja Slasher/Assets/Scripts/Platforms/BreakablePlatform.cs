using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BreakablePlatform : PlatformBase
{
    [Header("SETTINGS")]
    [SerializeField] private int _maxUses;
    private PlayerController _playerController;
    private int _remainingUses;

    [SerializeField] List<GameObject> _tilemap = new List<GameObject>();
    [SerializeField] ParticleSystem _particleSystem;
    [SerializeField] bool _hasSpikes;
    [SerializeField] GameObject _spikes;
    [SerializeField] ParticleSystem _spikesParticles;

    public override void Awake()
    {
        base.Awake();
    }


    protected override void InitializePlatform()
    {
        _remainingUses = _maxUses;
    }

    public override void OnPlayerExit(GameObject player, bool isForced = false) { }

    private float _lastEnterTime;

    public override void OnPlayerEnter(GameObject player)
    {
        if (Time.time < _lastEnterTime + 0.1f) return;
        _lastEnterTime = Time.time;

        player.TryGetComponent(out PlayerController controller);
        _playerController = controller;
        if (!isActive) return;

        if(_remainingUses >= 1)
        {
            ChangeTilemap();
            _particleSystem.Play();
            AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.Interaction, transform.position);
        }
        if (_remainingUses <= 0)
        {
            Break();
        }
    }

    public override void OnPlatformUpdate() { }

    private void Break()
    {
        isActive = false;

        GameEvents.RaiseBreakablePlatformBroken();
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.DestroyPlatform, transform.position);
        DeactivateWhole();
        if (_hasSpikes)
        {
            _spikes.SetActive(false);
            _spikesParticles.Play();
        }
        StartCoroutine(DestroyAfterParticles());
    }

    private void ChangeTilemap()
    {
        _tilemap[_maxUses - _remainingUses].SetActive(false);
        _remainingUses--;
        if(_remainingUses> 0)
        {
            _tilemap[_maxUses - _remainingUses].SetActive(true);
        }
    }

    private void DeactivateWhole()
    {
        _tilemap[_maxUses-1].SetActive(false);
    }

    private IEnumerator DestroyAfterParticles()
    {
        yield return null;
        Destroy(gameObject, 1f);
    }
}
