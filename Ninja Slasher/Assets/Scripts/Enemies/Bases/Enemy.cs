using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] protected EnemyData _data;

    protected EnemyAudioContext _audioContext;
    public EnemyAudioContext AudioContext => _audioContext;
    [SerializeField] float _deathTime;

    [SerializeField] protected LayerMask _obstaclesLayer;
    [SerializeField] protected LayerMask _playerLayer;

    protected Transform _player;
    protected Rigidbody2D _rb;
    protected Collider2D _col;
    [SerializeField] protected Collider2D _triggerCol;

    [SerializeField] protected Animator _animator;
    public Animator Animator => _animator;
    //[SerializeField] protected EnemyBrokenPart[] _parts;

    // Guard para garantizar que Die() y RegisterKill() se ejecutan una sola vez por enemigo
    protected bool _isDead = false;

    public virtual void OnEnable()
    {
        if (CustomUpdateManager.Instance != null)
            CustomUpdateManager.Instance.SubscribeToUpdate(CustomUpdate);
    }

    public virtual void OnDisable()
    {
        if (CustomUpdateManager.Instance != null)
            CustomUpdateManager.Instance.UnsubscribeFromUpdate(CustomUpdate);
    }

    protected virtual void Awake()
    {
        TryGetComponent(out Rigidbody2D rb);
        _rb = rb;
        TryGetComponent(out Collider2D col);
        _col = col;
        if (_animator == null)
        {
            TryGetComponent(out Animator anim);
            _animator = anim;
        }

        InitializeAudioContext();

        _player = FindAnyObjectByType<NewController>().transform;
    }

    public virtual void CustomUpdate() { }

    protected virtual void InitializeAudioContext()
    {
        _audioContext = GetComponent<EnemyAudioContext>();
        if (_audioContext != null && _data != null)
            _audioContext.Initialize(_data.AudioSet);
    }

    protected void DetectCollision(Direction dir)
    {
#if UNITY_EDITOR
        Debug.Log($"Hit rejected: {dir}");
#endif
    }

    public virtual void Die()
    {
        if (_isDead) return;
        _isDead = true;

        _animator.SetTrigger("OnHit");
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.hit, transform.position);
        _col.excludeLayers += LayerMask.GetMask("Player");
        _triggerCol.excludeLayers += LayerMask.GetMask("Player");
        StartCoroutine(BreakEnemy());
    }

    //public void StartBreaking()
    //{
    //    StartCoroutine(BreakEnemy());
    //}

    private IEnumerator BreakEnemy()
    {
        //_rb.bodyType = RigidbodyType2D.Dynamic;
        //foreach (var part in _parts)
        //{
        //    part.BreakAndThrow();
        //}
        yield return new WaitForSeconds(1f);
        RegisterKill();
        //yield return new WaitForSeconds(2f);
        //Destroy(gameObject);
    }

    protected void RegisterKill()
    {
        if (LevelSessionManager.Instance != null)
            LevelSessionManager.Instance.RegisterEnemyKilled(this);
        else
            Debug.LogError("[Enemy] LevelSessionManager no encontrado - el enemigo no será trackeado");

        // Evento canónico: ComboManager escucha esto, sin acoplamiento directo
        GameEvents.RaiseEnemyKilled(transform.position);
    }


    [ContextMenu("Test Death")]
    private void TestDie()
    {
        Die();
    }
    
}