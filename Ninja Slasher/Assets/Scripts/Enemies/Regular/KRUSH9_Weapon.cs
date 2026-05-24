using UnityEngine;

public class KRUSH9_Weapon : MonoBehaviour
{
    [SerializeField] KRUSH9 _bot;
    public Collider2D Col => _col;
    [SerializeField] Collider2D _col;

    [Header("VFX")]
    [SerializeField] private ParticleSystem _impactParticles;

    private void PlayImpactParticles(Vector2 contactPoint, Vector2 normal)
    {
        if (_impactParticles != null)
        {
            _impactParticles.transform.position = contactPoint;
            _impactParticles.transform.up = normal;
            _impactParticles.Play();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        AudioService.Instance.PlaySFXAtPosition(_bot.AudioContext.Audio.collision, transform.position);
        
        if (collision.contacts.Length > 0)
        {
            ContactPoint2D contact = collision.contacts[0];
            PlayImpactParticles(contact.point, contact.normal);
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent(out PlayerController player))
            {
                player.Die();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        AudioService.Instance.PlaySFXAtPosition(_bot.AudioContext.Audio.collision, transform.position);

        ColliderDistance2D distance = Physics2D.Distance(collision, _col);
        Vector2 normal = distance.normal;
        
        // Fallback if normal is exactly zero
        if (normal == Vector2.zero)
        {
            normal = ((Vector2)transform.position - distance.pointA).normalized;
            if (normal == Vector2.zero) normal = Vector2.up;
        }

        PlayImpactParticles(distance.pointA, normal);

        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent(out PlayerController player))
            {
                player.Die();
            }
        }
    }
}
