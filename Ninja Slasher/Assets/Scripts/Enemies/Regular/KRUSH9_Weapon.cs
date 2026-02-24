using UnityEngine;

public class KRUSH9_Weapon : MonoBehaviour
{
    [SerializeField] KRUSH9 _bot;
    public Collider2D Col => _col;
    [SerializeField] Collider2D _col;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        AudioService.Instance.PlaySFXAtPosition(_bot.AudioContext.Audio.collision, transform.position);
        if (collision.CompareTag("Player"))
        {
            collision.TryGetComponent(out NewController player);
            player.Die();
        }
    }
}
