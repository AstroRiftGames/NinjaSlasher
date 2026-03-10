using System;
using UnityEngine;

public class GuardBotShield : MonoBehaviour
{
    public Action<GameObject> OnCollision;
    public Collider2D Col => _col;
    [SerializeField] Collider2D _col;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        OnCollision?.Invoke(gameObject);
    }
}
