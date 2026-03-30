using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Tilemaps;

public class BreakablePlatform : PlatformBase
{
    [Header("SETTINGS")]
    [SerializeField] private int _maxUses;
    private NewController _playerController;
    private int _remainingUses;

    [SerializeField] GameObject _tilemap;
    [SerializeField] ParticleSystem _particleSystem;

    public override void Awake()
    {
        base.Awake();
    }


    protected override void InitializePlatform()
    {
        _remainingUses = _maxUses;
    }

    public override void OnPlayerExit(GameObject player, bool isForced = false) { }

    public override void OnPlayerEnter(GameObject player)
    {
        player.TryGetComponent(out NewController controller);
        _playerController = controller;
        if (!isActive) return;


        _remainingUses--;
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.Interaction, transform.position);
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
        _particleSystem.Play();
        StartCoroutine(DestroyAfterParticles());
    }

    private void DeactivateWhole()
    {
        _tilemap.SetActive(false);
    }

    private IEnumerator DestroyAfterParticles()
    {
        yield return null;
        _playerController.ForceDash(Vector2.down);
        Destroy(gameObject, 1f);
    }
}
