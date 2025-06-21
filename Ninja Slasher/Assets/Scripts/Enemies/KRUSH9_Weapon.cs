using UnityEngine;

public class KRUSH9_Weapon : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.TryGetComponent(out Controller player);
            player.Die();
        }
    }
}
