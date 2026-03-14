using System;
using UnityEngine;
using DG.Tweening;

public class EnemyBrokenPart : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private float _spawnTime;
    [SerializeField] private float _timeToDestroy = 3.5f;
    [SerializeField] private float _fadingTime = 1f;
    private void Start()
    {
        _spawnTime = Time.time;
    }

    private void Update()
    {
        CheckTime();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if(collision.gameObject.layer is 7 or 6)
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }

    private void CheckTime()
    {
        if (Time.time >= _spawnTime + _timeToDestroy)
        {
            _spriteRenderer.DOFade(0, _fadingTime);
        }
    }

}
