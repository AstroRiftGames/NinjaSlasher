using UnityEngine;

public class SpikeDamage : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var controller = collision.gameObject.GetComponent<Controller>();
            if (controller != null)
            {
                controller.Die();
            }
        }
    }
}