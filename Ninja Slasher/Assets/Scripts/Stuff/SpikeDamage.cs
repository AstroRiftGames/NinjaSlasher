using System.Linq;
using UnityEngine;

public class SpikeDamage : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var controller = collision.gameObject.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.Die();
            }
        }
        else if(collision.gameObject.CompareTag("Enemy"))
        {
            var enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Die();
            }
        }
    }
}