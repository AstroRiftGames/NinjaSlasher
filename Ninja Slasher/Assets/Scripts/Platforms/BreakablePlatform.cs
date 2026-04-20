using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Tilemaps;

public class BreakablePlatform : PlatformBase
{
    [Header("SETTINGS")]
    [SerializeField] private int _maxUses;
    private PlayerController _playerController;
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
        if (_remainingUses == 1)
        {
            SetFinalUseState();
        }
    }

    private void SetFinalUseState()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Collider2D[] playerColliders = player != null ? player.GetComponentsInChildren<Collider2D>() : new Collider2D[0];

        var colliders = _tilemap.GetComponentsInChildren<Collider2D>(true);
        foreach (var col in colliders)
        {
            if (!col.isTrigger && playerColliders.Length > 0)
            {
                foreach (var pCol in playerColliders)
                {
                    Physics2D.IgnoreCollision(col, pCol, true);
                }
            }
        }
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


        _remainingUses--;
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.Interaction, transform.position);
        if (_remainingUses <= 0)
        {
            Break();
        }
        else if (_remainingUses == 1)
        {
            SetFinalUseState();
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
        Destroy(gameObject, 1f);
    }
}
